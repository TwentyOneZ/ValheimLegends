using UnityEngine;

namespace ValheimLegends;

/// <summary>
/// Centralized asset and runtime icon resolver for Valheim Legends.
/// Safely resolves prefabs and icons from ZNetScene only when the game runtime is ready,
/// preventing any static initialization crashes (.cctor) or premature singleton access.
/// </summary>
public static class VLGameAssets
{
    public static Sprite TryResolveItemIcon(string prefabName)
    {
        if (ZNetScene.instance == null) return null;

        GameObject prefab = ZNetScene.instance.GetPrefab(prefabName);
        if (prefab == null)
        {
            ZLog.LogWarning($"[VL] Missing prefab for icon: {prefabName}");
            return null;
        }

        ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();
        if (itemDrop == null || itemDrop.m_itemData == null)
        {
            ZLog.LogWarning($"[VL] Prefab {prefabName} has no ItemDrop or m_itemData");
            return null;
        }

        return itemDrop.m_itemData.GetIcon();
    }

    public static void ResolveAllRuntimeIcons()
    {
        if (ZNetScene.instance == null) return;

        ZLog.Log("[VL] Resolving runtime icons from ZNetScene...");
        int total = 0;
        int resolved = 0;

        void Resolve(ref Sprite target, string prefabName)
        {
            total++;
            Sprite s = TryResolveItemIcon(prefabName);
            if (s != null)
            {
                target = s;
                resolved++;
            }
        }

        // --- Class & Elemental Status Effect Icons ---
        Resolve(ref SE_FlameArmor.AbilityIcon, "SurtlingCore");
        Resolve(ref SE_FlameWeapon.AbilityIcon, "StaffFireball");
        Resolve(ref SE_Fireaffinity.AbilityIcon, "StaffFireball");
        Resolve(ref SE_IceArmor.AbilityIcon, "FreezeGland");
        Resolve(ref SE_IceWeapon.AbilityIcon, "StaffIceShards");
        Resolve(ref SE_Frostaffinity.AbilityIcon, "StaffIceShards");
        Resolve(ref SE_ThunderArmor.AbilityIcon, "Thunderstone");
        Resolve(ref SE_ThunderWeapon.AbilityIcon, "DragonTear");
        Resolve(ref SE_Lightningaffinity.AbilityIcon, "DragonTear");
        Resolve(ref SE_Reactivearmor.AbilityIcon, "StaffShield");
        Resolve(ref SE_Charmcontrol.AbilityIcon, "StaffSkeleton");
        Resolve(ref SE_DyingLight_CD.AbilityIcon, "TrophySkeleton");
        Resolve(ref SE_Regeneration.AbilityIcon, "TrophyGreydwarfShaman");
        Resolve(ref SE_Bulwark.AbilityIcon, "ShieldBlackmetalTower");
        Resolve(ref SE_Enrage.AbilityIcon, "TrophyGoblinBrute");
        Resolve(ref SE_Shell.AbilityIcon, "ShieldSerpentscale");
        Resolve(ref SE_SpiritDrain.AbilityIcon, "TrophyDragonQueen");
        Resolve(ref SE_Berserk.AbilityIcon, "TrophyGoblinKing");
        Resolve(ref SE_Execute.AbilityIcon, "SwordCheat");
        Resolve(ref SE_PowerShot.AbilityIcon, "ArrowFire");
        Resolve(ref SE_ShadowStalk.AbilityIcon, "TrophyWraith");
        Resolve(ref SE_Companion.AbilityIcon, "TrophyWolf");
        Resolve(ref SE_RootsBuff.AbilityIcon, "TrophyWolf");
        Resolve(ref SE_Slow.AbilityIcon, "TrophyWolf");
        Resolve(ref SE_Ability1_CD.AbilityIcon, "ShieldWood");
        Resolve(ref SE_Ability2_CD.AbilityIcon, "ShieldBanded");
        Resolve(ref SE_Ability3_CD.AbilityIcon, "ShieldSilver");

        // --- Mod Texture Icons ---
        SE_Riposte.AbilityIcon = ValheimLegends.RiposteIcon;
        SE_Rogue.AbilityIcon = ValheimLegends.RogueIcon;
        SE_Monk.AbilityIcon = ValheimLegends.MonkIcon;
        SE_Ranger.AbilityIcon = ValheimLegends.RangerIcon;
        SE_Valkyrie.AbilityIcon = ValheimLegends.ValkyrieIcon;
        SE_Weaken.AbilityIcon = ValheimLegends.WeakenIcon;
        SE_BiomeMeadows.AbilityIcon = ValheimLegends.BiomeMeadowsIcon;
        SE_BiomeBlackForest.AbilityIcon = ValheimLegends.BiomeBlackForestIcon;
        SE_BiomeSwamp.AbilityIcon = ValheimLegends.BiomeSwampIcon;
        SE_BiomeMountain.AbilityIcon = ValheimLegends.BiomeMountainIcon;
        SE_BiomePlains.AbilityIcon = ValheimLegends.BiomePlainsIcon;
        SE_BiomeOcean.AbilityIcon = ValheimLegends.BiomeOceanIcon;
        SE_BiomeMist.AbilityIcon = ValheimLegends.BiomeMistIcon;
        SE_BiomeAsh.AbilityIcon = ValheimLegends.BiomeAshIcon;

        // --- Update instances currently registered in ObjectDB ---
        UpdateObjectDBStatusEffectIcons();

        ZLog.Log($"[VL] Runtime icons resolved: {resolved}/{total}");
    }

    public static void UpdateObjectDBStatusEffectIcons()
    {
        if (ObjectDB.instance == null || ObjectDB.instance.m_StatusEffects == null) return;

        foreach (StatusEffect se in ObjectDB.instance.m_StatusEffects)
        {
            if (se == null || string.IsNullOrEmpty(se.name)) continue;

            switch (se.name)
            {
                case "SE_VL_FlameArmor":
                    if (SE_FlameArmor.AbilityIcon != null) se.m_icon = SE_FlameArmor.AbilityIcon;
                    break;
                case "SE_VL_FlameWeapon":
                    if (SE_FlameWeapon.AbilityIcon != null) se.m_icon = SE_FlameWeapon.AbilityIcon;
                    break;
                case "SE_VL_Fireaffinity":
                    if (SE_Fireaffinity.AbilityIcon != null) se.m_icon = SE_Fireaffinity.AbilityIcon;
                    break;
                case "SE_VL_IceArmor":
                    if (SE_IceArmor.AbilityIcon != null) se.m_icon = SE_IceArmor.AbilityIcon;
                    break;
                case "SE_VL_IceWeapon":
                    if (SE_IceWeapon.AbilityIcon != null) se.m_icon = SE_IceWeapon.AbilityIcon;
                    break;
                case "SE_VL_Frostaffinity":
                    if (SE_Frostaffinity.AbilityIcon != null) se.m_icon = SE_Frostaffinity.AbilityIcon;
                    break;
                case "SE_VL_ThunderArmor":
                    if (SE_ThunderArmor.AbilityIcon != null) se.m_icon = SE_ThunderArmor.AbilityIcon;
                    break;
                case "SE_VL_ThunderWeapon":
                    if (SE_ThunderWeapon.AbilityIcon != null) se.m_icon = SE_ThunderWeapon.AbilityIcon;
                    break;
                case "SE_VL_Lightningaffinity":
                    if (SE_Lightningaffinity.AbilityIcon != null) se.m_icon = SE_Lightningaffinity.AbilityIcon;
                    break;
                case "SE_VL_Reactivearmor":
                    if (SE_Reactivearmor.AbilityIcon != null) se.m_icon = SE_Reactivearmor.AbilityIcon;
                    break;
                case "SE_VL_Charmcontrol":
                    if (SE_Charmcontrol.AbilityIcon != null) se.m_icon = SE_Charmcontrol.AbilityIcon;
                    break;
                case "SE_VL_DyingLight_CD":
                    if (SE_DyingLight_CD.AbilityIcon != null) se.m_icon = SE_DyingLight_CD.AbilityIcon;
                    break;
                case "SE_VL_ManaShield":
                    if (se.m_icon == null)
                    {
                        Sprite icon = TryResolveItemIcon("StaminaUpgrade_Greydwarf");
                        if (icon != null) se.m_icon = icon;
                    }
                    break;
                case "SE_VL_ArcaneIntellect":
                    if (se.m_icon == null)
                    {
                        Sprite icon = TryResolveItemIcon("HelmetPointyHat");
                        if (icon != null) se.m_icon = icon;
                    }
                    break;
                case "SE_VL_ElementalMastery":
                    if (se.m_icon == null)
                    {
                        Sprite icon = TryResolveItemIcon("Eitr");
                        if (icon != null) se.m_icon = icon;
                    }
                    break;
                case "SE_VL_Frozen":
                    if (se.m_icon == null)
                    {
                        Sprite icon = TryResolveItemIcon("FreezeGland");
                        if (icon != null) se.m_icon = icon;
                    }
                    break;
                case "SE_VL_DruidCultistForm":
                    if (se.m_icon == null)
                    {
                        Sprite icon = TryResolveItemIcon("TrophyCultist");
                        if (icon != null) se.m_icon = icon;
                    }
                    break;
                case "SE_VL_DruidFenringForm":
                case "SE_VL_Shapeshift_CD":
                    if (se.m_icon == null)
                    {
                        Sprite icon = TryResolveItemIcon("TrophyFenring");
                        if (icon != null) se.m_icon = icon;
                    }
                    break;
                case "SE_VL_Regeneration":
                    if (SE_Regeneration.AbilityIcon != null) se.m_icon = SE_Regeneration.AbilityIcon;
                    break;
                case "SE_VL_Bulwark":
                    if (SE_Bulwark.AbilityIcon != null) se.m_icon = SE_Bulwark.AbilityIcon;
                    break;
                case "SE_VL_Enrage":
                    if (SE_Enrage.AbilityIcon != null) se.m_icon = SE_Enrage.AbilityIcon;
                    break;
                case "SE_VL_Shell":
                    if (SE_Shell.AbilityIcon != null) se.m_icon = SE_Shell.AbilityIcon;
                    break;
                case "SE_VL_SpiritDrain":
                    if (SE_SpiritDrain.AbilityIcon != null) se.m_icon = SE_SpiritDrain.AbilityIcon;
                    break;
                case "SE_VL_Berserk":
                    if (SE_Berserk.AbilityIcon != null) se.m_icon = SE_Berserk.AbilityIcon;
                    break;
                case "SE_VL_Execute":
                    if (SE_Execute.AbilityIcon != null) se.m_icon = SE_Execute.AbilityIcon;
                    break;
                case "SE_VL_PowerShot":
                    if (SE_PowerShot.AbilityIcon != null) se.m_icon = SE_PowerShot.AbilityIcon;
                    break;
                case "SE_VL_ShadowStalk":
                    if (SE_ShadowStalk.AbilityIcon != null) se.m_icon = SE_ShadowStalk.AbilityIcon;
                    break;
                case "SE_VL_Companion":
                    if (SE_Companion.AbilityIcon != null) se.m_icon = SE_Companion.AbilityIcon;
                    break;
                case "SE_VL_RootsBuff":
                    if (SE_RootsBuff.AbilityIcon != null) se.m_icon = SE_RootsBuff.AbilityIcon;
                    break;
                case "SE_VL_Slow":
                    if (SE_Slow.AbilityIcon != null) se.m_icon = SE_Slow.AbilityIcon;
                    break;
                case "SE_VL_Ability1_CD":
                    if (SE_Ability1_CD.AbilityIcon != null) se.m_icon = SE_Ability1_CD.AbilityIcon;
                    break;
                case "SE_VL_Ability2_CD":
                    if (SE_Ability2_CD.AbilityIcon != null) se.m_icon = SE_Ability2_CD.AbilityIcon;
                    break;
                case "SE_VL_Ability3_CD":
                    if (SE_Ability3_CD.AbilityIcon != null) se.m_icon = SE_Ability3_CD.AbilityIcon;
                    break;
                case "SE_VL_Riposte":
                    if (SE_Riposte.AbilityIcon != null) se.m_icon = SE_Riposte.AbilityIcon;
                    break;
                case "SE_VL_Rogue":
                    if (SE_Rogue.AbilityIcon != null) se.m_icon = SE_Rogue.AbilityIcon;
                    break;
                case "SE_VL_Monk":
                    if (SE_Monk.AbilityIcon != null) se.m_icon = SE_Monk.AbilityIcon;
                    break;
                case "SE_VL_Ranger":
                    if (SE_Ranger.AbilityIcon != null) se.m_icon = SE_Ranger.AbilityIcon;
                    break;
                case "SE_VL_Valkyrie":
                    if (SE_Valkyrie.AbilityIcon != null) se.m_icon = SE_Valkyrie.AbilityIcon;
                    break;
                case "SE_VL_Weaken":
                    if (SE_Weaken.AbilityIcon != null) se.m_icon = SE_Weaken.AbilityIcon;
                    break;
            }
        }
    }
}
