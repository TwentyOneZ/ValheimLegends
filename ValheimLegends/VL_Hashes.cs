namespace ValheimLegends
{
    /// <summary>
    /// Hashes estáveis pré-calculados (static readonly int) para todos os StatusEffects frequentemente consultados.
    /// Elimina o recálculo contínuo de "SE_...".GetStableHashCode() em hot paths como Update, Damage e Input.
    /// </summary>
    public static class VL_Hashes
    {
        // Cooldowns
        public static readonly int Ability1_CD = "SE_VL_Ability1_CD".GetStableHashCode();
        public static readonly int Ability2_CD = "SE_VL_Ability2_CD".GetStableHashCode();
        public static readonly int Ability3_CD = "SE_VL_Ability3_CD".GetStableHashCode();
        public static readonly int Shapeshift_CD = "SE_VL_Shapeshift_CD".GetStableHashCode();
        public static readonly int DyingLight_CD = "SE_VL_DyingLight_CD".GetStableHashCode();
        public static readonly int Windfury_CD = "SE_VL_Windfury_CD".GetStableHashCode();
        public static readonly int SE_VL_Ability1_CD = Ability1_CD;
        public static readonly int SE_VL_Ability2_CD = Ability2_CD;
        public static readonly int SE_VL_Ability3_CD = Ability3_CD;

        // Berserker
        public static readonly int Berserk = "SE_VL_Berserk".GetStableHashCode();
        public static readonly int Execute = "SE_VL_Execute".GetStableHashCode();

        // Druid
        public static readonly int DruidFenringForm = "SE_VL_DruidFenringForm".GetStableHashCode();
        public static readonly int DruidCultistForm = "SE_VL_DruidCultistForm".GetStableHashCode();
        public static readonly int Regeneration = "SE_VL_Regeneration".GetStableHashCode();
        public static readonly int RootsBuff = "SE_VL_RootsBuff".GetStableHashCode();

        // Enchanter Armors & Weapons
        // Enchanter Armors & Weapons & Affinities
        public static readonly int FlameArmor = "SE_VL_FlameArmor".GetStableHashCode();
        public static readonly int IceArmor = "SE_VL_IceArmor".GetStableHashCode();
        public static readonly int ThunderArmor = "SE_VL_ThunderArmor".GetStableHashCode();
        public static readonly int FlameWeapon = "SE_VL_FlameWeapon".GetStableHashCode();
        public static readonly int IceWeapon = "SE_VL_IceWeapon".GetStableHashCode();
        public static readonly int ThunderWeapon = "SE_VL_ThunderWeapon".GetStableHashCode();
        public static readonly int FireAffinity = "SE_VL_Fireaffinity".GetStableHashCode();
        public static readonly int FrostAffinity = "SE_VL_Frostaffinity".GetStableHashCode();
        public static readonly int LightningAffinity = "SE_VL_Lightningaffinity".GetStableHashCode();
        public static readonly int Weaken = "SE_VL_Weaken".GetStableHashCode();
        public static readonly int Charm = "SE_VL_Charm".GetStableHashCode();
        public static readonly int CharmImmunity = "SE_VL_CharmImmunity".GetStableHashCode();

        // Biomes
        public static readonly int BiomeAsh = "SE_VL_BiomeAsh".GetStableHashCode();
        public static readonly int BiomeBlackForest = "SE_VL_BiomeBlackForest".GetStableHashCode();
        public static readonly int BiomeMeadows = "SE_VL_BiomeMeadows".GetStableHashCode();
        public static readonly int BiomeMist = "SE_VL_BiomeMist".GetStableHashCode();
        public static readonly int BiomeMountain = "SE_VL_BiomeMountain".GetStableHashCode();
        public static readonly int BiomeOcean = "SE_VL_BiomeOcean".GetStableHashCode();
        public static readonly int BiomePlains = "SE_VL_BiomePlains".GetStableHashCode();
        public static readonly int BiomeSwamp = "SE_VL_BiomeSwamp".GetStableHashCode();

        // Classes & Passives
        public static readonly int Ranger = "SE_VL_Ranger".GetStableHashCode();
        public static readonly int PowerShot = "SE_VL_PowerShot".GetStableHashCode();
        public static readonly int Rogue = "SE_VL_Rogue".GetStableHashCode();
        public static readonly int ShadowStalk = "SE_VL_ShadowStalk".GetStableHashCode();
        public static readonly int Valkyrie = "SE_VL_Valkyrie".GetStableHashCode();
        public static readonly int Monk = "SE_VL_Monk".GetStableHashCode();
        public static readonly int Slow = "SE_VL_Slow".GetStableHashCode();
        public static readonly int Bulwark = "SE_VL_Bulwark".GetStableHashCode();
        public static readonly int Riposte = "SE_VL_Riposte".GetStableHashCode();
        public static readonly int Companion = "SE_VL_Companion".GetStableHashCode();
        public static readonly int Shell = "SE_VL_Shell".GetStableHashCode();

        // Mage
        public static readonly int MageFireAffinity = "SE_VL_MageFireAffinity".GetStableHashCode();
        public static readonly int MageFrostAffinity = "SE_VL_MageFrostAffinity".GetStableHashCode();
        public static readonly int MageArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();
        public static readonly int ManaShield = "SE_VL_ManaShield".GetStableHashCode();
        public static readonly int ElementalMastery = "SE_VL_ElementalMastery".GetStableHashCode();
        public static readonly int ArcaneIntellect = "SE_VL_ArcaneIntellect".GetStableHashCode();

        // Metavoker
        public static readonly int ReactiveArmor = "SE_VL_Reactivearmor".GetStableHashCode();
        public static readonly int CDReactiveArmor = "SE_VL_CDReactivearmor".GetStableHashCode();

        // Vanilla Game Status Effects
        public static readonly int Burning = "Burning".GetStableHashCode();
        public static readonly int Frost = "Frost".GetStableHashCode();
        public static readonly int Freeze = "Freeze".GetStableHashCode();
        public static readonly int Wet = "Wet".GetStableHashCode();
        public static readonly int Smoked = "Smoked".GetStableHashCode();
    }
}
