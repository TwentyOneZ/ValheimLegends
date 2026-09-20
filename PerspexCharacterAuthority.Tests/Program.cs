using PerspexCharacterAuthority;

var root = Path.Combine(Path.GetTempPath(), "pca-check-" + Guid.NewGuid().ToString("N"));
try
{
    var snapshot = new CharacterSnapshot
    {
        AccountId = "account-a",
        CharacterId = 4242,
        CharacterName = "A",
        CreatedUtcTicks = DateTime.UtcNow.Ticks,
        UpdatedUtcTicks = DateTime.UtcNow.Ticks,
        VanillaPlayerData = new byte[] { 1, 2, 3 },
        Extensions = { ["ValheimLegends"] = new ExtensionPayload { SchemaVersion = 1, Data = new byte[] { 4 } } }
    };
    var encoded = PcaSnapshotCodec.Serialize(snapshot, 1024);
    Assert(PcaSnapshotCodec.TryDeserialize(encoded, 1024, out var decoded, out _));
    Assert(decoded.CharacterId == 4242 && decoded.Extensions["ValheimLegends"].Data[0] == 4);
    encoded[8] ^= 1;
    Assert(!PcaSnapshotCodec.TryDeserialize(encoded, 1024, out _, out _));

    var store = new PcaStore(root, 1024, true, 2);
    Assert(store.TryAuthorize("account-a", 4242, "A", true, out _));
    Assert(!store.TryAuthorize("account-a", 4243, "B", true, out _));
    Assert(store.TryAuthorize("account-a", 4243, "B", false, out _));
    Assert(store.TryGetBinding("account-a", out var binding) && binding.CharacterId == 4242);
    Assert(!store.TryAuthorize("account-b", 4242, "A", false, out _));
    Assert(store.TrySave(snapshot, out _));
    Assert(store.TryRead("account-a", 4242, out var loaded, out var recovered) && !recovered && loaded.VanillaPlayerData.SequenceEqual(new byte[] { 1, 2, 3 }));
    snapshot.VanillaPlayerData = new byte[] { 9 };
    snapshot.UpdatedUtcTicks = DateTime.UtcNow.Ticks;
    Assert(store.TrySave(snapshot, out _));
    var current = Directory.GetFiles(root, "current.pca", SearchOption.AllDirectories).Single();
    File.WriteAllBytes(current, new byte[] { 0, 1, 2 });
    Assert(store.TryRead("account-a", 4242, out loaded, out recovered) && recovered && loaded.VanillaPlayerData.SequenceEqual(new byte[] { 1, 2, 3 }));

    Assert(store.TryAuthorize("account-c", 5252, "C", true, out _));
    var bindingFile = Directory.GetFiles(Path.Combine(root, "Accounts"), "*.pcb").Single(path =>
        PcaBindingCodec.TryDeserialize(File.ReadAllBytes(path), out var candidate) && candidate.AccountId == "account-c");
    File.WriteAllBytes(bindingFile, new byte[] { 1, 2, 3 });
    Assert(!store.TryAuthorize("account-c", 5353, "D", true, out var corruptBindingError) && corruptBindingError.Contains("corrupt"));

    var noBackup = new CharacterSnapshot
    {
        AccountId = "account-d", CharacterId = 6262, CharacterName = "D",
        CreatedUtcTicks = DateTime.UtcNow.Ticks, UpdatedUtcTicks = DateTime.UtcNow.Ticks,
        VanillaPlayerData = new byte[] { 7 }
    };
    var noBackupStore = new PcaStore(root, 1024, false, 0);
    Assert(noBackupStore.TrySave(noBackup, out _));
    var noBackupCurrent = Directory.GetFiles(root, "current.pca", SearchOption.AllDirectories)
        .Single(path => path.Contains(Path.DirectorySeparatorChar + "6262" + Path.DirectorySeparatorChar));
    File.WriteAllBytes(noBackupCurrent, new byte[] { 0 });
    Assert(noBackupStore.HasSnapshotData("account-d", 6262));
    Assert(!noBackupStore.TryRead("account-d", 6262, out _, out _));
    var validForeignBackup = Directory.GetFiles(root, "*.pca", SearchOption.AllDirectories)
        .First(path => path.Contains(Path.DirectorySeparatorChar + "4242" + Path.DirectorySeparatorChar + "backups"));
    var wrongTargetBackups = Path.Combine(Path.GetDirectoryName(noBackupCurrent), "backups");
    Directory.CreateDirectory(wrongTargetBackups);
    var wrongTargetBackup = Path.Combine(wrongTargetBackups, "foreign.pca");
    File.Copy(validForeignBackup, wrongTargetBackup);
    Assert(!noBackupStore.TryRestore("account-d", 6262, Path.GetFileName(wrongTargetBackup), out var restoreError) && restoreError.Contains("identity"));

    var zeroRoot = Path.Combine(root, "zero-retention");
    var zeroStore = new PcaStore(zeroRoot, 1024, true, 0);
    var zeroSnapshot = new CharacterSnapshot
    {
        AccountId = "account-zero", CharacterId = 7070, CharacterName = "Zero",
        CreatedUtcTicks = DateTime.UtcNow.Ticks, UpdatedUtcTicks = DateTime.UtcNow.Ticks,
        VanillaPlayerData = new byte[] { 1 }
    };
    Assert(zeroStore.TrySave(zeroSnapshot, out _));
    zeroSnapshot.VanillaPlayerData = new byte[] { 2 };
    Assert(zeroStore.TrySave(zeroSnapshot, out _));
    Assert(Directory.GetFiles(zeroRoot, "*.pca", SearchOption.AllDirectories).Count(path => path.Contains("backups")) == 1);
    Console.WriteLine("PCA storage/binding checks passed.");
}
finally
{
    if (Directory.Exists(root)) Directory.Delete(root, true);
}

static void Assert(bool condition)
{
    if (!condition) throw new InvalidOperationException("PCA check failed.");
}
