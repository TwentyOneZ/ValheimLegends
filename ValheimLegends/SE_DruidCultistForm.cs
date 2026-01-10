using HarmonyLib;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System;

namespace ValheimLegends
{
    public class SE_DruidCultistForm : StatusEffect
    {
        // Cultist Set (vanilla internal prefab names)
        private const string VIS_HELMET = "HelmetCultist";
        private const string VIS_CHEST = "ArmorCultistChest";
        private const string VIS_LEGS = "ArmorCultistLegs";
        private const string VIS_SHOULDER = "CapeCultist";

        // Reapply watchdog (mantém a skin durante o buff, mesmo após equips)
        private float _visCheckTimer = 0f;
        private const float VIS_CHECK_INTERVAL = 0.10f; // 5x/seg (leve e suficiente)

        // Icon safe-init (evita null no load)
        private static Sprite _abilityIcon;
        public static Sprite AbilityIcon
        {
            get
            {
                if (_abilityIcon == null && ZNetScene.instance != null)
                {
                    var go = ZNetScene.instance.GetPrefab("TrophyCultist");
                    var id = go != null ? go.GetComponent<ItemDrop>() : null;
                    _abilityIcon = id != null ? id.m_itemData.GetIcon() : null;
                }
                return _abilityIcon;
            }
        }

        public static GameObject GO_SEFX;

        public float regenBonus = 1f;
        private float m_timer = 0f;
        private float m_interval = 5f;

        public float casterPower = 1f;
        public float casterLevel = 0f;

        public bool doOnce = true;
        public static float BaseTTL = 60f;

        public float resistModifier = 0.9f;

        // ===== Visual shapeshift state =====
        private bool _visualApplied;
        private VisSnapshot _oldVis;

        // Reflection: Humanoid.m_visEquipment is protected in your build
        private static readonly FieldInfo FI_VisEquipment = AccessTools.Field(typeof(Humanoid), "m_visEquipment");

        // Internal field candidates (version tolerant)
        private static readonly string[] F_HELMET = { "m_helmetItem", "m_currentHelmetItem", "m_helmet" };
        private static readonly string[] F_CHEST = { "m_chestItem", "m_currentChestItem", "m_chest" };
        private static readonly string[] F_LEGS = { "m_legItem", "m_legsItem", "m_currentLegItem", "m_leg", "m_legs" };
        private static readonly string[] F_SHOULDER = { "m_shoulderItem", "m_currentShoulderItem", "m_shoulder" };
        private static readonly string[] F_UTILITY = { "m_utilityItem", "m_currentUtilityItem", "m_utility" };

        private static readonly string[] F_HELMET_VAR = { "m_helmetItemVariant", "m_currentHelmetVariant", "m_helmetVariant" };
        private static readonly string[] F_CHEST_VAR = { "m_chestItemVariant", "m_currentChestVariant", "m_chestVariant" };
        private static readonly string[] F_LEGS_VAR = { "m_legItemVariant", "m_legsItemVariant", "m_currentLegVariant", "m_legVariant", "m_legsVariant" };
        private static readonly string[] F_SHOULDER_VAR = { "m_shoulderItemVariant", "m_currentShoulderVariant", "m_shoulderVariant" };
        private static readonly string[] F_UTILITY_VAR = { "m_utilityItemVariant", "m_currentUtilityVariant", "m_utilityVariant" };

        // Methods (some builds use (string), others (string,int))
        private static readonly MethodInfo MI_SetHelmet_1 = AccessTools.Method(typeof(VisEquipment), "SetHelmetItem", new[] { typeof(string) });
        private static readonly MethodInfo MI_SetHelmet_2 = AccessTools.Method(typeof(VisEquipment), "SetHelmetItem", new[] { typeof(string), typeof(int) });

        private static readonly MethodInfo MI_SetChest_1 = AccessTools.Method(typeof(VisEquipment), "SetChestItem", new[] { typeof(string) });
        private static readonly MethodInfo MI_SetChest_2 = AccessTools.Method(typeof(VisEquipment), "SetChestItem", new[] { typeof(string), typeof(int) });

        private static readonly MethodInfo MI_SetLegs_1 = AccessTools.Method(typeof(VisEquipment), "SetLegItem", new[] { typeof(string) });
        private static readonly MethodInfo MI_SetLegs_2 = AccessTools.Method(typeof(VisEquipment), "SetLegItem", new[] { typeof(string), typeof(int) });

        private static readonly MethodInfo MI_SetShoulder_2 = AccessTools.Method(typeof(VisEquipment), "SetShoulderItem", new[] { typeof(string), typeof(int) });
        private static readonly MethodInfo MI_SetShoulder_1 = AccessTools.Method(typeof(VisEquipment), "SetShoulderItem", new[] { typeof(string) });

        private static readonly MethodInfo MI_SetUtility_1 = AccessTools.Method(typeof(VisEquipment), "SetUtilityItem", new[] { typeof(string) });
        private static readonly MethodInfo MI_SetUtility_2 = AccessTools.Method(typeof(VisEquipment), "SetUtilityItem", new[] { typeof(string), typeof(int) });

        public SE_DruidCultistForm()
        {
            name = "SE_VL_DruidCultistForm";

            if (ZNetScene.instance != null)
            {
                var go = ZNetScene.instance.GetPrefab("TrophyCultist");
                var id = go != null ? go.GetComponent<ItemDrop>() : null;
                if (id != null) m_icon = id.m_itemData.GetIcon();
            }

            m_tooltip = "Cultist Form\nFireball / Inferno / Meditation\n+Eitr Regen\n+Health Regen";
            m_name = "Shapeshift: Cultist";
            doOnce = true;
        }

        public override void Setup(Character character)
        {
            base.Setup(character);
            TryApplyCultistVisual();
        }

        public override void OnDestroy()
        {
            TryRestoreVisual();
            base.OnDestroy();
        }

        public override void UpdateStatusEffect(float dt)
        {
            if (doOnce)
            {
                doOnce = false;

                // Aplica no começo; se Setup rodou cedo demais, isso garante.
                TryApplyCultistVisual();

                casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
                casterPower = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
                    .m_level * (1f + Mathf.Clamp(
                        (EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) +
                        (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f),
                        0f, 0.5f));

                float num = casterLevel * 5f / 6f * (1f + casterPower / 150f);
                regenBonus = (3f + 0.3f * num) * VL_GlobalConfigs.g_DamageModifer;
                m_tooltip = "Increased Health and Eitr Regen.";
            }

            // ===== regen =====
            m_timer -= dt;
            if (m_timer <= 0f)
            {
                m_timer = m_interval;
                m_character.Heal(regenBonus * (1f - m_character.GetHealthPercentage()));
                m_character.AddEitr(regenBonus * (1f - m_character.GetEitrPercentage()));
            }

            // ===== mantém a skin durante o buff (equipar arma/armadura não quebra mais) =====
            MaintainCultistVisual(dt);

            base.UpdateStatusEffect(dt);
        }

        public override bool CanAdd(Character character) => character.IsPlayer();

        // ===============================
        // Visual logic (Cultist set) + "lock" watchdog
        // ===============================

        private void TryApplyCultistVisual()
        {
            if (_visualApplied) return;
            if (m_character == null || !m_character.IsPlayer()) return;

            var player = m_character as Player;
            if (player == null) return;

            // MP-safe: só owner escreve visual (ZDO)
            var nview = player.GetComponent<ZNetView>();
            if (nview == null || !nview.IsOwner()) return;

            var ve = GetVisEquipment(player);
            if (ve == null) return;

            // Backup atual (uma vez)
            _oldVis = ReadVisSnapshot(ve);

            // Aplica Cultist (variant 0)
            ApplyCultistVisual(ve);

            _visualApplied = true;
            _visCheckTimer = 0f; // força primeira checagem imediatamente
        }

        private void MaintainCultistVisual(float dt)
        {
            if (!_visualApplied) return;
            if (m_character == null || !m_character.IsPlayer()) return;

            var player = m_character as Player;
            if (player == null) return;

            // Só o owner mantém (evita spam e conflitos em MP)
            var nview = player.GetComponent<ZNetView>();
            if (nview == null || !nview.IsOwner()) return;

            _visCheckTimer -= dt;
            if (_visCheckTimer > 0f) return;
            _visCheckTimer = VIS_CHECK_INTERVAL;

            var ve = GetVisEquipment(player);
            if (ve == null) return;

            // Se qualquer slot principal não bater (equipou algo e o jogo recalculou), reaplica.
            if (!IsCultistStillApplied(ve))
            {
                ApplyCultistVisual(ve);
            }
        }

        private bool IsCultistStillApplied(VisEquipment ve)
        {
            var cur = ReadVisSnapshot(ve);

            // Se você quiser permitir aliases, é aqui que você inclui ORs.
            bool okHelmet = string.Equals(cur.helmet ?? "", VIS_HELMET, StringComparison.Ordinal);
            bool okChest = string.Equals(cur.chest ?? "", VIS_CHEST, StringComparison.Ordinal);
            bool okLegs = string.Equals(cur.legs ?? "", VIS_LEGS, StringComparison.Ordinal);
            bool okShoulder = string.Equals(cur.shoulder ?? "", VIS_SHOULDER, StringComparison.Ordinal);

            return okHelmet && okChest && okLegs && okShoulder;
        }

        private void ApplyCultistVisual(VisEquipment ve)
        {
            InvokeSetItem(ve, MI_SetHelmet_2, MI_SetHelmet_1, VIS_HELMET, 0);
            InvokeSetItem(ve, MI_SetChest_2, MI_SetChest_1, VIS_CHEST, 0);
            InvokeSetItem(ve, MI_SetLegs_2, MI_SetLegs_1, VIS_LEGS, 0);
            InvokeSetItem(ve, MI_SetShoulder_2, MI_SetShoulder_1, VIS_SHOULDER, 0);
        }
        private void TryRestoreVisual()
        {
            if (!_visualApplied) return;
            if (m_character == null || !m_character.IsPlayer()) return;

            var player = m_character as Player;
            if (player == null) return;

            var nview = player.GetComponent<ZNetView>();
            if (nview == null || !nview.IsOwner()) return;

            var ve = GetVisEquipment(player);
            if (ve == null) return;

            ApplyVisSnapshot(ve, _oldVis);
            _visualApplied = false;
        }

        private static VisEquipment GetVisEquipment(Player player)
        {
            if (player == null) return null;

            var ve = FI_VisEquipment?.GetValue(player) as VisEquipment;
            if (ve != null) return ve;

            return player.GetComponent<VisEquipment>();
        }

        private struct VisSnapshot
        {
            public string helmet, chest, legs, shoulder, utility;
            public int helmetVar, chestVar, legsVar, shoulderVar, utilityVar;
        }

        private static VisSnapshot ReadVisSnapshot(VisEquipment ve)
        {
            var s = new VisSnapshot
            {
                helmet = ReadStringField(ve, F_HELMET),
                chest = ReadStringField(ve, F_CHEST),
                legs = ReadStringField(ve, F_LEGS),
                shoulder = ReadStringField(ve, F_SHOULDER),
                utility = ReadStringField(ve, F_UTILITY),

                helmetVar = ReadIntField(ve, F_HELMET_VAR),
                chestVar = ReadIntField(ve, F_CHEST_VAR),
                legsVar = ReadIntField(ve, F_LEGS_VAR),
                shoulderVar = ReadIntField(ve, F_SHOULDER_VAR),
                utilityVar = ReadIntField(ve, F_UTILITY_VAR)
            };
            return s;
        }

        private static void ApplyVisSnapshot(VisEquipment ve, VisSnapshot s)
        {
            InvokeSetItem(ve, MI_SetHelmet_2, MI_SetHelmet_1, s.helmet, s.helmetVar);
            InvokeSetItem(ve, MI_SetChest_2, MI_SetChest_1, s.chest, s.chestVar);
            InvokeSetItem(ve, MI_SetLegs_2, MI_SetLegs_1, s.legs, s.legsVar);

            // Shoulder: prefer (string,int) first (seu build exige)
            InvokeSetItem(ve, MI_SetShoulder_2, MI_SetShoulder_1, s.shoulder, s.shoulderVar);
            InvokeSetItem(ve, MI_SetUtility_2, MI_SetUtility_1, s.utility, s.utilityVar);
        }

        private static void InvokeSetItem(VisEquipment ve, MethodInfo mi2, MethodInfo mi1, string item, int variant)
        {
            try
            {
                if (mi2 != null)
                {
                    mi2.Invoke(ve, new object[] { item ?? "", variant });
                    return;
                }
                if (mi1 != null)
                {
                    mi1.Invoke(ve, new object[] { item ?? "" });
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VL] VisEquipment setter invoke failed: " + e);
            }
        }

        private static string ReadStringField(object obj, string[] candidates)
        {
            if (obj == null || candidates == null) return null;
            Type t = obj.GetType();

            foreach (string name in candidates)
            {
                if (string.IsNullOrEmpty(name)) continue;
                FieldInfo fi = AccessTools.Field(t, name);
                if (fi != null && fi.FieldType == typeof(string))
                    return fi.GetValue(obj) as string;
            }
            return null;
        }

        private static int ReadIntField(object obj, string[] candidates)
        {
            if (obj == null || candidates == null) return 0;
            Type t = obj.GetType();

            foreach (string name in candidates)
            {
                if (string.IsNullOrEmpty(name)) continue;
                FieldInfo fi = AccessTools.Field(t, name);
                if (fi != null && fi.FieldType == typeof(int))
                    return (int)fi.GetValue(obj);
            }
            return 0;
        }
    }
}
