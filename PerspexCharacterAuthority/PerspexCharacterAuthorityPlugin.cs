using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace PerspexCharacterAuthority;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
public sealed class PerspexCharacterAuthorityPlugin : BaseUnityPlugin
{
    public const string PluginGuid = "TwentyOneZ.PerspexCharacterAuthority";
    public const string PluginName = "PerspexCharacterAuthority";
    public const string PluginVersion = "1.0.1";
    public const int ProtocolVersion = 1;
    private const string RpcName = "PCA_Message";
    private const string VLExtension = "ValheimLegends";
    private const string FreshProgressionExtension = "PCA.FreshProgression";
    private const string FreshAppliedExtension = "PCA.FreshApplied";
    private const int FirstJoinTimeoutSeconds = 2;

    private enum MessageKind { Hello = 1, Identify = 2, Snapshot = 3, Submit = 4, Denied = 5, SaveAck = 6 }
    private enum ClientAccess { Vanilla, Pending, Allowed, Denied }
    private sealed class Session { public string AccountId; public long CharacterId; public string CharacterName; }

    internal static PerspexCharacterAuthorityPlugin Instance;
    public static bool IsAuthorityActiveForCurrentSession => Instance != null && Instance.Enabled && Instance.clientAccess != ClientAccess.Vanilla;
    private readonly Dictionary<ZRpc, Session> sessions = new Dictionary<ZRpc, Session>();
    private readonly Dictionary<string, ZRpc> activeCharacters = new Dictionary<string, ZRpc>(StringComparer.Ordinal);
    private PcaStore store;
    private ConfigEntry<bool> pcaEnabled;
    private ConfigEntry<bool> singleCharacter;
    private ConfigEntry<string> firstJoinMode;
    private ConfigEntry<int> autosaveSeconds;
    private ConfigEntry<bool> saveOnLogout;
    private ConfigEntry<bool> saveOnDisconnect;
    private ConfigEntry<bool> saveOnShutdown;
    private ConfigEntry<bool> backupsEnabled;
    private ConfigEntry<int> maximumBackups;
    private ConfigEntry<int> maxSnapshotSizeMb;
    private ConfigEntry<bool> debugLogging;
    private ClientAccess clientAccess = ClientAccess.Vanilla;
    private ZRpc clientRpc;
    private CharacterSnapshot clientSnapshot;
    private bool resetFreshProgressionOnLoad;
    private bool freshProgressionApplied;
    private bool serverDetected;
    private long lastServerSnapshotTicks;

    private int MaxBytes => Math.Max(1, maxSnapshotSizeMb.Value) * 1024 * 1024;
    internal bool Enabled => pcaEnabled != null && pcaEnabled.Value;

    private void Awake()
    {
        Instance = this;
        pcaEnabled = Config.Bind("General", "Enabled", true, "Enable server-authoritative character snapshots.");
        debugLogging = Config.Bind("General", "DebugLogging", false, "Log protocol details.");
        singleCharacter = Config.Bind("Access", "SingleCharacterPerAccount", true, "Bind each account to one persistent CharacterId.");
        firstJoinMode = Config.Bind("FirstJoin", "Mode", "PreserveCharacter", "PreserveCharacter or FreshProgression.");
        autosaveSeconds = Config.Bind("Saving", "AutosaveIntervalSeconds", 300, "Client snapshot submit interval.");
        saveOnLogout = Config.Bind("Saving", "SaveOnLogout", true, "Submit the local snapshot during teardown.");
        saveOnDisconnect = Config.Bind("Saving", "SaveOnDisconnect", true, "Submit the local snapshot when the network object is destroyed.");
        saveOnShutdown = Config.Bind("Saving", "SaveOnServerShutdown", true, "Submit the local snapshot during application shutdown.");
        backupsEnabled = Config.Bind("Backups", "Enabled", true, "Keep rotating snapshots before replacement.");
        maximumBackups = Config.Bind("Backups", "MaximumBackupsPerCharacter", 10, "Maximum backups retained per character.");
        maxSnapshotSizeMb = Config.Bind("Limits", "MaxSnapshotSizeMB", 16, "Maximum accepted snapshot payload.");
        store = new PcaStore(Path.Combine(Paths.ConfigPath, "PerspexCharacterAuthority"), MaxBytes, backupsEnabled.Value, maximumBackups.Value);
        new Harmony(PluginGuid).PatchAll(typeof(PerspexCharacterAuthorityPlugin).Assembly);
        new Terminal.ConsoleCommand("pca", "PCA server character administration", RunCommand, onlyServer: true, onlyAdmin: true);
        InvokeRepeating(nameof(Autosave), Math.Max(10, autosaveSeconds.Value), Math.Max(10, autosaveSeconds.Value));
    }

    private void OnApplicationQuit()
    {
        if (saveOnShutdown.Value) SubmitLocalSnapshot("shutdown");
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Autosave() => SubmitLocalSnapshot("autosave");

    internal void OnPeerInfo(ZNet network, ZRpc rpc)
    {
        if (!Enabled || rpc == null) return;
        if (network.IsServer())
        {
            var authenticated = false;
            foreach (var peer in network.GetPeers())
                if (peer.m_rpc == rpc && peer.IsReady()) { authenticated = true; break; }
            if (!authenticated) return;
            var socketType = rpc.GetSocket()?.GetType().Name;
            if (!string.Equals(socketType, "ZSteamSocket", StringComparison.Ordinal) &&
                !string.Equals(socketType, "ZPlayFabSocket", StringComparison.Ordinal))
            {
                Warn("PCA could not authenticate the network backend; rejecting the peer.");
                rpc.Invoke("Error", 8);
                return;
            }
        }
        rpc.Register<ZPackage>(RpcName, HandleMessage);
        if (network.IsServer()) Send(rpc, MessageKind.Hello, Array.Empty<byte>());
        else WaitForServerHandshake(rpc);
    }

    private void WaitForServerHandshake(ZRpc rpc)
    {
        if (ZNet.instance == null || ZNet.instance.IsServer()) return;
        clientRpc = rpc;
        clientAccess = ClientAccess.Pending;
        serverDetected = false;
        CancelInvoke(nameof(AllowVanillaFallback));
        Invoke(nameof(AllowVanillaFallback), FirstJoinTimeoutSeconds);
        BeginClientHandshake();
    }

    private void BeginClientHandshake()
    {
        try
        {
            var snapshot = BuildLocalSnapshot();
            // Account ownership is never supplied by the client; this marker is replaced by the server.
            snapshot.AccountId = "client-untrusted";
            Send(clientRpc, MessageKind.Identify, PcaSnapshotCodec.Serialize(snapshot, MaxBytes));
        }
        catch (Exception ex)
        {
            Warn("Could not identify local character: " + ex.Message);
            clientAccess = ClientAccess.Denied;
        }
    }

    private void AllowVanillaFallback()
    {
        if (clientAccess != ClientAccess.Pending || serverDetected) return;
        clientAccess = ClientAccess.Vanilla;
        Log("PCA server not detected; using vanilla character persistence.");
        Game.instance?.RequestRespawn(0f);
    }

    private void HandleMessage(ZRpc rpc, ZPackage package)
    {
        if (package == null || package.Size() > MaxBytes + 65536) { Deny(rpc, "PCA payload is too large."); return; }
        try
        {
            var protocol = package.ReadInt();
            var kind = (MessageKind)package.ReadInt();
            var payload = package.ReadByteArray();
            if (protocol != ProtocolVersion) { Deny(rpc, "PCA protocol version mismatch."); return; }
            if (payload == null || payload.Length > MaxBytes + 65536) { Deny(rpc, "PCA payload is too large."); return; }
            if (ZNet.instance != null && ZNet.instance.IsServer()) HandleServerMessage(rpc, kind, payload);
            else HandleClientMessage(kind, payload);
        }
        catch (Exception ex)
        {
            Warn("Rejected malformed PCA message: " + ex.Message);
            if (ZNet.instance != null && ZNet.instance.IsServer()) Deny(rpc, "Invalid PCA message.");
        }
    }

    private void HandleServerMessage(ZRpc rpc, MessageKind kind, byte[] payload)
    {
        if (kind == MessageKind.Identify) { IdentifyServerCharacter(rpc, payload); return; }
        if (kind == MessageKind.Submit) { SaveSubmittedCharacter(rpc, payload); return; }
        Deny(rpc, "Unexpected PCA message.");
    }

    private void IdentifyServerCharacter(ZRpc rpc, byte[] payload)
    {
        if (!PcaSnapshotCodec.TryDeserialize(payload, MaxBytes, out var incoming, out var error)) { Deny(rpc, "Invalid character snapshot: " + error); return; }
        var socket = rpc.GetSocket();
        var accountId = string.Equals(socket?.GetType().Name, "ZPlayFabSocket", StringComparison.Ordinal)
            ? socket.GetEndPointString()
            : socket?.GetHostName();
        if (string.IsNullOrWhiteSpace(accountId)) { Deny(rpc, "Authenticated account identity is unavailable."); return; }
        if (!store.TryAuthorize(accountId, incoming.CharacterId, incoming.CharacterName, singleCharacter.Value, out var denial)) { Deny(rpc, denial); return; }

        CharacterSnapshot authoritative;
        var firstJoin = false;
        if (store.TryRead(accountId, incoming.CharacterId, out authoritative, out var recovered))
        {
            if (authoritative.CharacterId != incoming.CharacterId || !string.Equals(authoritative.AccountId, accountId, StringComparison.Ordinal))
            {
                Deny(rpc, "Stored character identity validation failed.");
                return;
            }
            if (recovered)
            {
                Warn("Snapshot checksum mismatch; restoring the newest valid backup for account=" + accountId + ".");
                if (!store.TrySave(authoritative, out error)) { Deny(rpc, "Could not restore the valid backup: " + error); return; }
            }
            Log("Server snapshot found for account=" + accountId + " character=" + incoming.CharacterId + ".");
        }
        else
        {
            if (store.HasSnapshotData(accountId, incoming.CharacterId))
            {
                Deny(rpc, "No valid server snapshot or backup is available; refusing local overwrite.");
                return;
            }
            firstJoin = true;
            authoritative = incoming;
            authoritative.AccountId = accountId;
            authoritative.CreatedUtcTicks = DateTime.UtcNow.Ticks;
            authoritative.UpdatedUtcTicks = authoritative.CreatedUtcTicks;
            if (!store.TrySave(authoritative, out error)) { Deny(rpc, "Could not create server snapshot: " + error); return; }
            Log("Created server snapshot for account=" + accountId + " character=" + incoming.CharacterId + ".");
        }
        if (firstJoin && string.Equals(firstJoinMode.Value, "FreshProgression", StringComparison.OrdinalIgnoreCase))
        {
            // Keep this marker server-side until the client submits the reset blob; a crash cannot resurrect progression.
            authoritative.Extensions[VLExtension] = VLBridge.EmptyState();
            authoritative.Extensions[FreshProgressionExtension] = new ExtensionPayload { SchemaVersion = 1, Data = Array.Empty<byte>() };
            if (!store.TrySave(authoritative, out error)) { Deny(rpc, "Could not mark fresh progression: " + error); return; }
        }
        var leaseKey = accountId + "\n" + authoritative.CharacterId;
        if (activeCharacters.TryGetValue(leaseKey, out var activeRpc) && activeRpc != rpc && activeRpc.IsConnected())
        {
            Deny(rpc, "This character already has an active session.");
            return;
        }
        activeCharacters[leaseKey] = rpc;
        sessions[rpc] = new Session { AccountId = accountId, CharacterId = authoritative.CharacterId, CharacterName = authoritative.CharacterName };
        Send(rpc, MessageKind.Snapshot, PcaSnapshotCodec.Serialize(authoritative, MaxBytes));
    }

    private void SaveSubmittedCharacter(ZRpc rpc, byte[] payload)
    {
        if (!sessions.TryGetValue(rpc, out var session)) { Deny(rpc, "Character has not been authorized."); return; }
        if (!PcaSnapshotCodec.TryDeserialize(payload, MaxBytes, out var submitted, out var error)) { Deny(rpc, "Invalid submitted snapshot: " + error); return; }
        if (submitted.CharacterId != session.CharacterId) { Deny(rpc, "Submitted character does not match the authenticated session."); return; }
        submitted.AccountId = session.AccountId;
        submitted.CharacterName = string.IsNullOrWhiteSpace(submitted.CharacterName) ? session.CharacterName : submitted.CharacterName;
        if (store.TryRead(session.AccountId, session.CharacterId, out var current, out _))
        {
            submitted.CreatedUtcTicks = current.CreatedUtcTicks;
            var freshApplied = submitted.Extensions.Remove(FreshAppliedExtension);
            if (!freshApplied && current.Extensions.ContainsKey(FreshProgressionExtension) && current.Extensions.TryGetValue(VLExtension, out var freshVl))
                submitted.Extensions[VLExtension] = freshVl;
            foreach (var extension in current.Extensions)
                if ((extension.Key != FreshProgressionExtension || !freshApplied) && !submitted.Extensions.ContainsKey(extension.Key))
                    submitted.Extensions.Add(extension.Key, extension.Value);
        }
        submitted.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
        if (!store.TrySave(submitted, out error)) { Warn("Snapshot save failed: " + error); return; }
        session.CharacterName = submitted.CharacterName;
        Send(rpc, MessageKind.SaveAck, Array.Empty<byte>());
        Log("Snapshot committed atomically for account=" + session.AccountId + " character=" + session.CharacterId + ".");
    }

    private void HandleClientMessage(MessageKind kind, byte[] payload)
    {
        if (kind == MessageKind.Hello)
        {
            if (clientAccess != ClientAccess.Pending || serverDetected) return;
            serverDetected = true;
            CancelInvoke(nameof(AllowVanillaFallback));
            BeginClientHandshake();
            return;
        }
        if (kind == MessageKind.Denied)
        {
            clientAccess = ClientAccess.Denied;
            CancelInvoke(nameof(AllowVanillaFallback));
            var message = payload == null ? "Character access denied." : System.Text.Encoding.UTF8.GetString(payload);
            Warn(message);
            MessageHud.instance?.ShowMessage(MessageHud.MessageType.Center, message);
            return;
        }
        if (kind == MessageKind.SaveAck) { freshProgressionApplied = false; return; }
        if (clientAccess != ClientAccess.Pending) return;
        if (kind != MessageKind.Snapshot)
        {
            clientAccess = ClientAccess.Denied;
            Warn("Unexpected PCA server message.");
            return;
        }
        if (!PcaSnapshotCodec.TryDeserialize(payload, MaxBytes, out var snapshot, out var error))
        {
            clientAccess = ClientAccess.Denied;
            Warn("Invalid server snapshot: " + error);
            return;
        }
        serverDetected = true;
        var profile = Game.instance?.GetPlayerProfile();
        if (profile == null || snapshot.CharacterId != profile.GetPlayerID())
        {
            clientAccess = ClientAccess.Denied;
            Warn("Server snapshot identity does not match the selected character.");
            return;
        }
        AccessTools.Field(typeof(PlayerProfile), "m_playerData").SetValue(profile, snapshot.VanillaPlayerData);
        clientSnapshot = snapshot;
        resetFreshProgressionOnLoad = snapshot.Extensions.ContainsKey(FreshProgressionExtension);
        lastServerSnapshotTicks = snapshot.UpdatedUtcTicks;
        clientAccess = ClientAccess.Allowed;
        CancelInvoke(nameof(AllowVanillaFallback));
        Log("Applying authoritative character snapshot before spawn.");
        Game.instance.RequestRespawn(0f);
    }

    internal bool BlockRespawn() => Enabled && (clientAccess == ClientAccess.Pending || clientAccess == ClientAccess.Denied);

    internal bool AuthorizeNativePlayerId(ZRpc rpc, long playerId)
    {
        if (!sessions.TryGetValue(rpc, out var session) || session.CharacterId != playerId)
        {
            Deny(rpc, "Character was not authorized before spawn.");
            rpc.Invoke("Error", 8);
            return false;
        }
        return true;
    }

    internal void ResetClientSession(ZNet network)
    {
        sessions.Clear();
        activeCharacters.Clear();
        if (network != null && network.IsServer()) return;
        CancelInvoke(nameof(AllowVanillaFallback));
        clientAccess = ClientAccess.Vanilla;
        clientRpc = null;
        clientSnapshot = null;
        resetFreshProgressionOnLoad = false;
        freshProgressionApplied = false;
        serverDetected = false;
    }

    internal void ApplyPendingVLExtension(Player player)
    {
        if (clientAccess != ClientAccess.Allowed || clientSnapshot == null) return;
        if (clientSnapshot.Extensions.TryGetValue(VLExtension, out var extension)) VLBridge.Import(player, extension);
        else VLBridge.Reset(player);
    }

    internal void ApplyFreshProgression(PlayerProfile profile, Player player)
    {
        if (!resetFreshProgressionOnLoad || player == null || profile == null) return;
        resetFreshProgressionOnLoad = false;
        freshProgressionApplied = true;
        player.UnequipAllItems();
        player.GetInventory().RemoveAll();
        player.ResetCharacter();
        player.ClearFood();
        player.SetGuardianPower(string.Empty);
        player.m_guardianPowerCooldown = 0f;
        player.GiveDefaultItems();
        VLBridge.Reset(player);
        profile.SavePlayerData(player);
        Log("Applied FreshProgression before OnSpawned.");
    }

    internal void SubmitLocalSnapshot(string reason)
    {
        if (!Enabled || clientAccess != ClientAccess.Allowed || clientRpc == null || !clientRpc.IsConnected()) return;
        try { Send(clientRpc, MessageKind.Submit, PcaSnapshotCodec.Serialize(BuildLocalSnapshot(), MaxBytes)); }
        catch (Exception ex) { Warn("Could not submit " + reason + " snapshot: " + ex.Message); }
    }

    public static void SubmitCurrentSnapshot() => Instance?.SubmitLocalSnapshot("character state change");

    private CharacterSnapshot BuildLocalSnapshot()
    {
        var profile = Game.instance?.GetPlayerProfile() ?? throw new InvalidOperationException("Player profile is unavailable.");
        if (Player.m_localPlayer != null) profile.SavePlayerData(Player.m_localPlayer);
        var data = AccessTools.Field(typeof(PlayerProfile), "m_playerData").GetValue(profile) as byte[];
        if (data == null) throw new InvalidDataException("Native player data is unavailable.");
        var snapshot = new CharacterSnapshot
        {
            CharacterId = profile.GetPlayerID(),
            CharacterName = profile.GetName(),
            CreatedUtcTicks = DateTime.UtcNow.Ticks,
            UpdatedUtcTicks = DateTime.UtcNow.Ticks,
            VanillaPlayerData = data
        };
        var vl = VLBridge.Export(Player.m_localPlayer);
        if (vl != null) snapshot.Extensions.Add(VLExtension, vl);
        if (freshProgressionApplied) snapshot.Extensions.Add(FreshAppliedExtension, new ExtensionPayload { SchemaVersion = 1, Data = Array.Empty<byte>() });
        return snapshot;
    }

    private static void Send(ZRpc rpc, MessageKind kind, byte[] payload)
    {
        var package = new ZPackage();
        package.Write(ProtocolVersion);
        package.Write((int)kind);
        package.Write(payload ?? Array.Empty<byte>());
        rpc.Invoke(RpcName, package);
    }

    private void Deny(ZRpc rpc, string message)
    {
        Warn("Character rejected: " + message);
        Send(rpc, MessageKind.Denied, System.Text.Encoding.UTF8.GetBytes(message ?? "Character access denied."));
    }
    private void Log(string message) { if (debugLogging.Value) Logger.LogInfo("[PCA] " + message); }
    private void Warn(string message) => Logger.LogWarning("[PCA] " + message);
    internal void WarnExternal(string message) => Warn(message);

    private void RunCommand(Terminal.ConsoleEventArgs args)
    {
        if (args.Length < 2) { Print(args, "pca: list|info|backup|backups|restore|reset|delete|export|binding|characters|unbind|bind|status"); return; }
        var action = args[1].ToLowerInvariant();
        if (action == "status") { Print(args, "PCA=" + clientAccess + " protocol=" + ProtocolVersion + " snapshot=" + new DateTime(lastServerSnapshotTicks == 0 ? DateTime.UtcNow.Ticks : lastServerSnapshotTicks, DateTimeKind.Utc)); return; }
        if (!RequireServer(args)) return;
        if (action == "list") { foreach (var binding in store.GetAccountBindings()) Print(args, binding.AccountId + " -> " + binding.CharacterId + " " + binding.LastKnownCharacterName); return; }
        if (args.Length < 3) { Print(args, "Account ID is required."); return; }
        var account = args[2];
        if (action == "binding") { Print(args, store.TryGetBinding(account, out var binding) ? binding.CharacterId + " " + binding.LastKnownCharacterName : "No binding."); return; }
        if (action == "characters") { foreach (var item in store.GetCharacters(account)) Print(args, item.CharacterId + " " + item.CharacterName + " " + new DateTime(item.UpdatedUtcTicks, DateTimeKind.Utc)); return; }
        if (action == "info" && args.Length == 3)
        {
            Print(args, store.TryGetBinding(account, out var binding) ? "Binding: " + binding.CharacterId + " " + binding.LastKnownCharacterName : "No binding.");
            foreach (var item in store.GetCharacters(account)) Print(args, item.CharacterId + " " + item.CharacterName + " " + new DateTime(item.UpdatedUtcTicks, DateTimeKind.Utc));
            return;
        }
        if (action == "unbind") { Print(args, store.TryUnbind(account) ? "Binding removed; snapshots retained." : "No binding."); return; }
        if (action == "bind" && args.Length >= 4 && long.TryParse(args[3], out var bindId)) { Print(args, store.TryBind(account, bindId, args.Length >= 5 ? args[4] : string.Empty, out var err) ? "Binding saved." : err); return; }
        if (args.Length < 4 || !long.TryParse(args[3], out var characterId)) { Print(args, "CharacterId is required."); return; }
        if (action == "backups") { foreach (var backup in store.GetBackups(account, characterId)) Print(args, backup); return; }
        if (action == "backup") { Print(args, store.TryBackup(account, characterId, out var err) ? "Backup created." : err); return; }
        if (action == "restore" && args.Length >= 5) { var ok = store.TryRestore(account, characterId, args[4], out var err); if (ok) RevokeCharacter(account, characterId); Print(args, ok ? "Restored with pre-restore backup." : err); return; }
        if (action == "reset") { var ok = ResetSnapshot(account, characterId, out var err); if (ok) RevokeCharacter(account, characterId); Print(args, ok ? "Fresh progression scheduled after backup." : err); return; }
        if (action == "delete") { var ok = store.TryDelete(account, characterId, out var err); if (ok) RevokeCharacter(account, characterId); Print(args, ok ? "Snapshot deleted after backup." : err); return; }
        if (action == "export") { var path = store.Export(account, characterId); Print(args, path ?? "Snapshot not found."); return; }
        if (action == "info") { if (store.TryRead(account, characterId, out var item, out var recovered)) Print(args, item.CharacterName + " " + item.UpdatedUtcTicks + (recovered ? " recovered-backup" : "")); else Print(args, "Snapshot not found."); return; }
        Print(args, "Invalid PCA command.");
    }

    private bool ResetSnapshot(string account, long characterId, out string error)
    {
        if (!store.TryRead(account, characterId, out var snapshot, out var recovered)) { error = "Snapshot not found."; return false; }
        if (recovered && !store.TrySave(snapshot, out error)) return false;
        if (!store.TryBackup(account, characterId, out error)) return false;
        snapshot.Extensions[VLExtension] = VLBridge.EmptyState();
        snapshot.Extensions[FreshProgressionExtension] = new ExtensionPayload { SchemaVersion = 1, Data = Array.Empty<byte>() };
        snapshot.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
        return store.TrySave(snapshot, out error);
    }

    private void RevokeCharacter(string account, long characterId)
    {
        activeCharacters.Remove(account + "\n" + characterId);
        foreach (var pair in new List<KeyValuePair<ZRpc, Session>>(sessions))
        {
            if (!string.Equals(pair.Value.AccountId, account, StringComparison.Ordinal) || pair.Value.CharacterId != characterId) continue;
            sessions.Remove(pair.Key);
            Deny(pair.Key, "Character authority was changed by an administrator; reconnect required.");
            pair.Key.Invoke("Error", 8);
        }
    }

    private static bool RequireServer(Terminal.ConsoleEventArgs args)
    {
        if (ZNet.instance != null && ZNet.instance.IsServer()) return true;
        Print(args, "PCA administration requires the server console.");
        return false;
    }
    private static void Print(Terminal.ConsoleEventArgs args, string value) => args.Context?.AddString(value);

    [HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
    private static class ZNetPeerInfoPatch { private static void Postfix(ZNet __instance, ZRpc rpc) => Instance?.OnPeerInfo(__instance, rpc); }

    [HarmonyPatch(typeof(ZNet), "RPC_PlayerID")]
    private static class ZNetPlayerIdPatch
    {
        private static bool Prefix(ZNet __instance, ZRpc rpc, long playerID) =>
            !__instance.IsServer() || Instance == null || Instance.AuthorizeNativePlayerId(rpc, playerID);
    }

    [HarmonyPatch(typeof(Game), "_RequestRespawn")]
    private static class GameRequestRespawnPatch { private static bool Prefix() => Instance == null || !Instance.BlockRespawn(); }

    [HarmonyPatch(typeof(Player), "OnSpawned")]
    [HarmonyPriority(Priority.First)]
    private static class PlayerSpawnPatch { private static void Prefix(Player __instance) => Instance?.ApplyPendingVLExtension(__instance); }

    [HarmonyPatch(typeof(PlayerProfile), "Save")]
    private static class PlayerProfileSavePatch { private static void Postfix() => Instance?.SubmitLocalSnapshot("player save"); }

    [HarmonyPatch(typeof(PlayerProfile), "LoadPlayerData")]
    [HarmonyPriority(Priority.Last)]
    private static class PlayerProfileLoadPatch { private static void Postfix(PlayerProfile __instance, Player player) => Instance?.ApplyFreshProgression(__instance, player); }

    [HarmonyPatch(typeof(Game), "Logout")]
    private static class GameLogoutPatch
    {
        private static void Prefix() { if (Instance != null && Instance.saveOnLogout.Value) Instance.SubmitLocalSnapshot("logout"); }
    }

    [HarmonyPatch(typeof(ZNet), "OnDestroy")]
    private static class ZNetDestroyPatch
    {
        private static void Prefix(ZNet __instance)
        {
            if (Instance == null) return;
            if (Instance.saveOnDisconnect.Value) Instance.SubmitLocalSnapshot("disconnect");
            Instance.ResetClientSession(__instance);
        }
    }
}

internal static class VLBridge
{
    private const int Schema = 1;
    private static Type PersistenceType => Type.GetType("ValheimLegends.VLCharacterPersistence, ValheimLegends");

    internal static ExtensionPayload EmptyState()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Schema);
        writer.Write(0);
        return new ExtensionPayload { SchemaVersion = Schema, Data = stream.ToArray() };
    }

    internal static ExtensionPayload Export(Player player)
    {
        var type = PersistenceType;
        var method = type?.GetMethod("ExportCharacterState", BindingFlags.Public | BindingFlags.Static);
        var state = method?.Invoke(null, new object[] { player });
        if (state == null) return null;
        var classField = state.GetType().GetField("Class");
        if (classField == null) return null;
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        writer.Write(Schema);
        writer.Write(Convert.ToInt32(classField.GetValue(state)));
        return new ExtensionPayload { SchemaVersion = Schema, Data = stream.ToArray() };
    }

    internal static void Reset(Player player)
    {
        try { PersistenceType?.GetMethod("ResetCharacterState", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new object[] { player }); }
        catch (Exception ex) { PerspexCharacterAuthorityPlugin.Instance?.WarnExternal("VL reset failed: " + ex.Message); }
    }

    internal static void Import(Player player, ExtensionPayload payload)
    {
        if (payload == null || payload.SchemaVersion != Schema || payload.Data == null) return;
        try
        {
            using var stream = new MemoryStream(payload.Data, false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadInt32() != Schema || stream.Length != 8) return;
            var classValue = reader.ReadInt32();
            var type = PersistenceType;
            var stateType = Type.GetType("ValheimLegends.VLCharacterState, ValheimLegends");
            var state = stateType == null ? null : Activator.CreateInstance(stateType);
            var classField = stateType?.GetField("Class");
            if (state == null || classField == null) return;
            classField.SetValue(state, Enum.ToObject(classField.FieldType, classValue));
            type.GetMethod("ImportCharacterState", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, new[] { (object)player, state });
        }
        catch (Exception ex) { PerspexCharacterAuthorityPlugin.Instance?.WarnExternal("VL extension import failed: " + ex.Message); }
    }
}
