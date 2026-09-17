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

        private static readonly int HASH_HELMET = VIS_HELMET.GetStableHashCode();
        private static readonly int HASH_CHEST = VIS_CHEST.GetStableHashCode();
        private static readonly int HASH_LEGS = VIS_LEGS.GetStableHashCode();
        private static readonly int HASH_SHOULDER = string.IsNullOrEmpty(VIS_SHOULDER) ? 0 : VIS_SHOULDER.GetStableHashCode();

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

            var ve = player.GetComponent<VisEquipment>();
            if (ve != null)
            {
                ApplyFenrisVisual(ve);
            }
        }

        private void ApplyFenrisVisual(VisEquipment ve)
        {
            if (ve == null) return;
            ve.SetHelmetItem(HASH_HELMET);
            ve.SetChestItem(HASH_CHEST);
            ve.SetLegItem(HASH_LEGS);
            ve.SetShoulderItem(HASH_SHOULDER, 0, 1);
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

            var ve = player.GetComponent<VisEquipment>();
            if (ve == null) return;

            ApplyFenrisVisual(ve);
            _visualApplied = true;
        }

        private static readonly MethodInfo MI_SetupVisEquipment = AccessTools.Method(typeof(Humanoid), "SetupVisEquipment", new[] { typeof(VisEquipment), typeof(bool) });

        private void TryRestoreVisual()
        {
            if (!_visualApplied) return;
            _visualApplied = false;

            if (m_character is Player player)
            {
                var ve = player.GetComponent<VisEquipment>();
                if (ve != null)
                {
                    MI_SetupVisEquipment?.Invoke(player, new object[] { ve, true });
                }
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

        public override bool IsDone()
        {
            return false;
        }
    }
}
