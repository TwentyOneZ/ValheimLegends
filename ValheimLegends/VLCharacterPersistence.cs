using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ValheimLegends;

// Public, dependency-free bridge for character authority mods. Custom VL skills live in Player.Skills.
public sealed class VLCharacterState
{
    public const int CurrentSchemaVersion = 1;
    public int SchemaVersion = CurrentSchemaVersion;
    public ValheimLegends.PlayerClass Class = ValheimLegends.PlayerClass.None;
}

public static class VLCharacterPersistence
{
    private static readonly string[] RuntimeFieldNames =
    {
        "zonechargeCharging", "zonechargeCount", "zonechargeChargeAmount", "zonechargeChargeAmountMax", "zonechargeSkillGain",
        "rootCount", "rootTotal", "rootCountTrigger", "warpCount", "warpDistance", "warpGrowthTrigger",
        "fkickCount", "fkickCountMax", "kicklist", "meteorCharging", "meteorCount", "meteorTimer", "meteorSkillGain",
        "blizzardCharging", "blizzardChargeTimer", "blizzardSpawnTimer", "blizzardTickCount", "meditationTimer", "isMeditating",
        "meditatonFocus", "healCharging", "healCount", "healChargeAmount", "healChargeAmountMax"
    };

    public static bool IsPerspexAuthorityActive
    {
        get
        {
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, "PerspexCharacterAuthority", StringComparison.Ordinal))
                ?.GetType("PerspexCharacterAuthority.PerspexCharacterAuthorityPlugin");
            var value = type?.GetProperty("IsAuthorityActiveForCurrentSession", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
            return value is bool active && active;
        }
    }

    public static void NotifyCharacterChanged()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => string.Equals(a.GetName().Name, "PerspexCharacterAuthority", StringComparison.Ordinal))
            ?.GetType("PerspexCharacterAuthority.PerspexCharacterAuthorityPlugin");
        type?.GetMethod("SubmitCurrentSnapshot", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
    }

    public static VLCharacterState ExportCharacterState(Player player)
    {
        var state = new VLCharacterState();
        var name = player != null ? player.GetPlayerName() : Game.instance?.GetPlayerProfile()?.GetName();
        if (!string.IsNullOrEmpty(name) && ValheimLegends.vl_player != null &&
            string.Equals(ValheimLegends.vl_player.vl_name, name, StringComparison.Ordinal))
            state.Class = ValheimLegends.vl_player.vl_class;
        else if (!string.IsNullOrEmpty(name) && ValheimLegends.vl_playerList != null)
        {
            var entry = ValheimLegends.vl_playerList.FirstOrDefault(value =>
                string.Equals(value.vl_name, name, StringComparison.Ordinal));
            if (entry != null) state.Class = entry.vl_class;
        }
        return state;
    }

    public static void ImportCharacterState(Player player, VLCharacterState state)
    {
        ResetCharacterState(player);
        if (state == null || state.SchemaVersion != VLCharacterState.CurrentSchemaVersion || player == null) return;

        var entry = new ValheimLegends.VL_Player { vl_name = player.GetPlayerName(), vl_class = state.Class };
        ValheimLegends.vl_playerList = new List<ValheimLegends.VL_Player> { entry };
        ValheimLegends.vl_player = new ValheimLegends.VL_Player { vl_name = entry.vl_name, vl_class = entry.vl_class };
        ValheimLegends.RemoveIncompatibleClassBuffs(player, entry.vl_class);
        if (player == Player.m_localPlayer) ValheimLegends.NameCooldowns();
    }

    public static void ResetCharacterState(Player player)
    {
        if (player != null) ValheimLegends.RemoveAllClassBuffs(player);
        ValheimLegends.vl_player = null;
        ValheimLegends.vl_playerList = new List<ValheimLegends.VL_Player>();

        Class_Mage.ResetState(player);
        Class_Mage.QueuedAttack = Class_Mage.MageAttackType.None;
        Class_Enchanter.QueuedAttack = Class_Enchanter.EnchanterAttackType.None;
        Class_Metavoker.QueuedAttack = 0;
        Class_Monk.QueuedAttack = 0;
        Class_Druid.canDoubleJump = true;
        Class_Rogue.throwDagger = false;
        Class_Rogue.canDoubleJump = true;
        Class_Rogue.canGainTrick = false;
        Class_Shaman.isWaterWalking = false;
        Class_Shaman.gotWindfuryCooldown = false;
        Class_Valkyrie.inFlight = false;
        Class_Valkyrie.isBlocking = false;
        Class_Metavoker.isReactivearmored = false;
        Class_Duelist.challengedDeath.Clear();
        Class_Duelist.challengedMastery.Clear();
        ArcaneIntellectUtil.RedirectingEitrCost = false;

        if (ValheimLegends.abilitiesStatus != null)
        {
            foreach (var status in ValheimLegends.abilitiesStatus)
                if (status != null) UnityEngine.Object.Destroy(status.gameObject);
            ValheimLegends.abilitiesStatus.Clear();
        }

        ResetFields(typeof(Class_Enchanter));
        ResetFields(typeof(Class_Druid));
        ResetFields(typeof(Class_Metavoker));
        ResetFields(typeof(Class_Monk));
        ResetFields(typeof(Class_Necromancer));
        ResetFields(typeof(Class_Priest));
        ClearCollectionField(typeof(ValheimLegends), "_enchFlameCharges");
        ClearCollectionField(typeof(ValheimLegends), "_enchIceCharges");
        ClearCollectionField(typeof(ValheimLegends), "_enchThunderCharges");
        ClearCollectionField(typeof(Class_Mage), "CooldownRegistry");
        ClearCollectionField(typeof(Class_Mage), "FrozenDamageRegistry");
        ClearCollectionField(typeof(SE_ManaShield), "_lastFxTimeByInstance");
        typeof(VL_Utility).GetField("vl_timer", BindingFlags.Static | BindingFlags.NonPublic)?.SetValue(null, 0f);
    }

    private static void ResetFields(Type type)
    {
        foreach (var name in RuntimeFieldNames)
        {
            var field = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            if (field == null || field.IsInitOnly) continue;
            if (typeof(IList).IsAssignableFrom(field.FieldType) || typeof(IDictionary).IsAssignableFrom(field.FieldType))
            {
                (field.GetValue(null) as IList)?.Clear();
                (field.GetValue(null) as IDictionary)?.Clear();
            }
            else if (field.FieldType.IsValueType || field.FieldType == typeof(string))
                field.SetValue(null, field.FieldType.IsValueType ? Activator.CreateInstance(field.FieldType) : null);
        }
    }

    private static void ClearCollectionField(Type type, string name)
    {
        var value = type.GetField(name, BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as IDictionary;
        value?.Clear();
    }
}
