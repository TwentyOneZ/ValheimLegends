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
        

        private static readonly int HASH_HELMET = VIS_HELMET.GetStableHashCode();
        private static readonly int HASH_CHEST = VIS_CHEST.GetStableHashCode();
        private static readonly int HASH_LEGS = VIS_LEGS.GetStableHashCode();
        private static readonly int HASH_SHOULDER = string.IsNullOrEmpty(VIS_SHOULDER) ? 0 : VIS_SHOULDER.GetStableHashCode();

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

        public override bool CanAdd(Character character) => character.IsPlayer() && ValheimLegends.vl_player != null && ValheimLegends.vl_player.vl_class == ValheimLegends.PlayerClass.Druid;

        // ===============================
        // Visual logic (Cultist set) + "lock" watchdog
        // ===============================

        private void TryApplyCultistVisual()
        {
            if (_visualApplied) return;
            if (m_character == null || !m_character.IsPlayer()) return;

            var player = m_character as Player;
            if (player == null) return;

            var nview = player.GetComponent<ZNetView>();
            if (nview == null || !nview.IsOwner()) return;

            var ve = player.GetComponent<VisEquipment>();
            if (ve == null) return;

            ApplyCultistVisual(ve);
            _visualApplied = true;
            _visCheckTimer = 0f;
        }

        private void MaintainCultistVisual(float dt)
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
                ApplyCultistVisual(ve);
            }
        }

        private void ApplyCultistVisual(VisEquipment ve)
        {
            if (ve == null) return;
            ve.SetHelmetItem(HASH_HELMET);
            ve.SetChestItem(HASH_CHEST);
            ve.SetLegItem(HASH_LEGS);
            ve.SetShoulderItem(HASH_SHOULDER, 0, 1);
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

        public override bool IsDone()
        {
            if (ValheimLegends.vl_player == null || ValheimLegends.vl_player.vl_class != ValheimLegends.PlayerClass.Druid)
            {
                return true;
            }
            return base.IsDone();
        }
    }
}
