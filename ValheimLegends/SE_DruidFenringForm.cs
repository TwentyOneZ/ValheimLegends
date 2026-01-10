using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ValheimLegends
{
    public class SE_DruidFenringForm : StatusEffect
    {
        // Cultist visual override (do seu exemplo atual)
        private const string VIS_HELMET = "HelmetCultist";
        private const string VIS_CHEST = "ArmorCultistChest";
        private const string VIS_LEGS = "ArmorCultistLegs";
        private const string VIS_SHOULDER = "CapeCultist";

        private float _visCheckTimer = 0f;
        private const float VIS_CHECK_INTERVAL = 0.2f;

        public float speedBonus = 1.1f;

        private static Sprite _abilityIcon;
        public static Sprite AbilityIcon
        {
            get
            {
                if (_abilityIcon == null && ZNetScene.instance != null)
                {
                    var go = ZNetScene.instance.GetPrefab("TrophyFenring");
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
        private float m_sustainTimer = 30f;
        private float m_sustainInterval = 1f;

        public float casterPower = 1f;
        public float casterLevel = 0f;

        public bool doOnce = true;
        public float resistModifier = 0.9f;

        private bool _visualApplied;

        private static readonly FieldInfo FI_VisEquipment = AccessTools.Field(typeof(Humanoid), "m_visEquipment");

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

        // ✅ CHAVE: força o jogo recalcular o visual real dos slots equipados
        private static readonly MethodInfo MI_UpdateEquipmentVisuals =
            AccessTools.Method(typeof(Humanoid), "UpdateEquipmentVisuals");

        public SE_DruidFenringForm()
        {
            name = "SE_VL_DruidFenringForm";

            if (ZNetScene.instance != null)
            {
                var go = ZNetScene.instance.GetPrefab("TrophyFenring");
                var id = go != null ? go.GetComponent<ItemDrop>() : null;
                if (id != null) m_icon = id.m_itemData.GetIcon();
            }

            m_tooltip = "Fenring Form\nDash / Stagger / ShadowStalk\n+Stamina Regen\n+Health Regen";
            m_name = "Shapeshift: Fenring";
            doOnce = true;
        }



        public override void Setup(Character character)
        {
            base.Setup(character);
            TryApplyFenrisVisual();
        }

        public override void OnDestroy()
        {
            TryRestoreVisual();
            base.OnDestroy();
        }

        public override void UpdateStatusEffect(float dt)
        {
            casterPower = m_character.GetSkills().GetSkillList()
                .FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
                .m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) +
                                    (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
            if (doOnce)
            {
                doOnce = false;

                TryApplyFenrisVisual();

                casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();

                float num = casterLevel * 5f / 6f * (1f + casterPower / 150f);
                regenBonus = (3f + 0.3f * num);
                m_tooltip = "Increased Health and Stamina Regen.\nUnarmed damage imbued with extra Slash damage\nIncreased moving speed.\nDrains Eitr to sustain shapeshift.";
                m_sustainTimer = 30f * (1 + (casterPower / 150f));

            }

            m_timer -= dt;
            UpdateName();
            if (m_timer <= 0f)
            {
                m_timer = m_interval;
                m_character.Heal(regenBonus * (1f - m_character.GetHealthPercentage()));
                m_character.AddStamina(regenBonus * (1f - m_character.GetStaminaPercentage()));
            }
            m_sustainTimer -= dt;
            if (m_sustainTimer <= 0f)
            {
                m_sustainTimer = m_sustainInterval;
                float eitrCost = (1 / (1 + casterPower / 150f));
                if (!m_character.HaveEitr(eitrCost))
                {
                    Class_Druid.TryActivate_HumanForm((Player)m_character, true);
                    return;
                }
                m_character.UseEitr(eitrCost);
            }

            MaintainFenrisVisual(dt);

            base.UpdateStatusEffect(dt);
        }

        private void UpdateName()
        {
            if (m_sustainTimer > 1f)
            {
                m_name = $"Fenring Form:\n{Math.Round(m_sustainTimer)}s to Eitr drain";
            }
            else
            {
                m_name = $"Shapeshift: Eitr drained";
            }
            
        }

        private void MaintainFenrisVisual(float dt)
        {
            if (!_visualApplied) return;
            if (m_character == null || !m_character.IsPlayer()) return;

            var player = m_character as Player;
            if (player == null) return;

            var nview = player.GetComponent<ZNetView>();
            if (nview == null || !nview.IsOwner()) return;

            _visCheckTimer -= dt;
            if (_visCheckTimer > 0f) return;
            _visCheckTimer = VIS_CHECK_INTERVAL;

            var ve = GetVisEquipment(player);
            if (ve == null) return;

            if (!IsFenrisStillApplied(ve))
            {
                ApplyFenrisVisual(ve);
            }
        }

        private bool IsFenrisStillApplied(VisEquipment ve)
        {
            var cur = ReadVisSnapshot(ve);

            bool okHelmet = string.Equals(cur.helmet ?? "", VIS_HELMET, StringComparison.Ordinal);
            bool okChest = string.Equals(cur.chest ?? "", VIS_CHEST, StringComparison.Ordinal);
            bool okLegs = string.Equals(cur.legs ?? "", VIS_LEGS, StringComparison.Ordinal);
            bool okShoulder = string.Equals(cur.shoulder ?? "", VIS_SHOULDER, StringComparison.Ordinal);

            return okHelmet && okChest && okLegs && okShoulder;
        }

        private void ApplyFenrisVisual(VisEquipment ve)
        {
            InvokeSetItem(ve, MI_SetHelmet_2, MI_SetHelmet_1, VIS_HELMET, 0);
            InvokeSetItem(ve, MI_SetChest_2, MI_SetChest_1, VIS_CHEST, 0);
            InvokeSetItem(ve, MI_SetLegs_2, MI_SetLegs_1, VIS_LEGS, 0);
            InvokeSetItem(ve, MI_SetShoulder_2, MI_SetShoulder_1, VIS_SHOULDER, 0);
        }

        public override bool CanAdd(Character character) => character.IsPlayer();

        private void TryApplyFenrisVisual()
        {
            if (_visualApplied) return;
            if (m_character == null || !m_character.IsPlayer()) return;

            var player = m_character as Player;
            if (player == null) return;

            var nview = player.GetComponent<ZNetView>();
            if (nview == null || !nview.IsOwner()) return;

            var ve = GetVisEquipment(player);
            if (ve == null) return;

            ApplyFenrisVisual(ve);
            _visualApplied = true;
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

            // ✅ importantíssimo: desliga o watchdog ANTES de restaurar
            _visualApplied = false;

            // 1) Remove overrides do VisEquipment (limpa “forma”)
            InvokeSetItem(ve, MI_SetHelmet_2, MI_SetHelmet_1, "", 0);
            InvokeSetItem(ve, MI_SetChest_2, MI_SetChest_1, "", 0);
            InvokeSetItem(ve, MI_SetLegs_2, MI_SetLegs_1, "", 0);
            InvokeSetItem(ve, MI_SetShoulder_2, MI_SetShoulder_1, "", 0);
            InvokeSetItem(ve, MI_SetUtility_2, MI_SetUtility_1, "", 0);

            // 2) Pede pro Valheim recalcular o visual real baseado nos slots equipados
            // (isso é MUITO mais confiável do que tentar inferir prefab name)
            try
            {
                MI_UpdateEquipmentVisuals?.Invoke(player, null);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[VL] UpdateEquipmentVisuals invoke failed: " + e);
            }
        }
        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
        {
            speed *= speedBonus;
            base.ModifySpeed(baseSpeed, ref speed, character, dir);
        }

        public override void ModifyEitrRegen(ref float eitrRegen)
        {
            eitrRegen = 0f;
            base.ModifyEitrRegen(ref eitrRegen);
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

        public override bool IsDone()
        {
            return false; // só sai via RemoveStatusEffect(hash)
        }
    }
}
