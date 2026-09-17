using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;
using static ValheimLegends.Class_Mage;

namespace ValheimLegends
{
    public class SE_MageAffinityBase : StatusEffect
    {
        public int m_currentCharges = 0;
        public float m_chargeTimer = 0f;
        public bool isFocused = false;

        // Configurações de Regeneração
        protected float regenIntervalFocused = 10f;
        protected float regenIntervalUnfocused = 30f;
        protected float regenIntervalResting = 1.0f;

        protected virtual string BaseDisplayName => "Mage Affinity";
        private int _lastDisplayedCharges = -1;
        private int _lastDisplayedMax = -1;
        private bool _lastDisplayedFocused = false;

        private static MethodInfo _restingMethod;
        private static bool _restingMethodResolved;

        public override void Setup(Character character)
        {
            base.Setup(character);
            m_currentCharges = 5;
            isFocused = false;
            m_ttl = 0;
            var arcane = ScriptableObject.CreateInstance<SE_MageArcaneAffinity>();
            character.GetSEMan().AddStatusEffect(arcane);
            if (GetCurrentFocus((Player)character) == MageAffinity.None) arcane.SetFocus(true);
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            if (!m_character.IsPlayer()) return;
            Player player = m_character as Player;

            float evocationLevel = VL_SkillHelper.GetSkillLevel(player, ValheimLegends.EvocationSkillDef);

            int maxCharges = 10 + Mathf.FloorToInt(evocationLevel * 0.2f);
            if (maxCharges > 30) maxCharges = 30;

            // Lógica de Regeneração Atualizada
            if (m_currentCharges < maxCharges)
            {
                bool isResting = IsResting(player);
                m_chargeTimer += dt;

                // Define o intervalo baseado no estado
                float interval;
                if (isResting)
                {
                    interval = regenIntervalResting;
                }
                else if (isFocused)
                {
                    interval = regenIntervalFocused;
                }
                else
                {
                    interval = regenIntervalUnfocused;
                }

                // Verifica se atingiu o tempo
                if (m_chargeTimer >= interval)
                {
                    m_currentCharges++;
                    m_chargeTimer = 0f;
                }
            }

            // Só atualiza a string de m_name se houve alteração nas cargas, max ou foco
            if (_lastDisplayedCharges != m_currentCharges || _lastDisplayedMax != maxCharges || _lastDisplayedFocused != isFocused)
            {
                _lastDisplayedCharges = m_currentCharges;
                _lastDisplayedMax = maxCharges;
                _lastDisplayedFocused = isFocused;
                string focusIcon = isFocused ? " <color=yellow>👁️</color>" : "";
                m_name = $"{BaseDisplayName}: <color=orange>{m_currentCharges}</color>/{maxCharges}{focusIcon}";
            }
        }

        public void ConsumeCharges(int amount)
        {
            m_currentCharges -= amount;
            if (m_currentCharges < 0) m_currentCharges = 0;
        }

        public void AddCharges(int amount)
        {
            if (!m_character.IsPlayer()) return;
            Player player = m_character as Player;

            float evocationLevel = Class_Mage.GetEvocationLevel(player);

            int maxCharges = 10 + Mathf.FloorToInt(evocationLevel / 7.5f);
            if (maxCharges > 30) maxCharges = 30;

            m_currentCharges += amount;
            if (m_currentCharges > maxCharges) m_currentCharges = maxCharges;
        }

        public void SetFocus(bool focused)
        {
            isFocused = focused;
        }

        private static bool IsResting(Player p)
        {
            if (p == null) return false;
            if (!_restingMethodResolved)
            {
                string[] candidates = { "InRestingArea", "InComfortZone", "InShelter", "InSafeZone" };
                foreach (var name in candidates)
                {
                    var mi = AccessTools.Method(typeof(Player), name);
                    if (mi != null && mi.ReturnType == typeof(bool) && mi.GetParameters().Length == 0)
                    {
                        _restingMethod = mi;
                        break;
                    }
                }
                _restingMethodResolved = true;
            }

            if (_restingMethod != null)
            {
                try { return (bool)_restingMethod.Invoke(p, null); }
                catch { return true; }
            }
            return p.InShelter();
        }
    }

    // --- Subclasses ---

    public class SE_MageFireAffinity : SE_MageAffinityBase
    {
        protected override string BaseDisplayName => "Flame Affinity";

        public SE_MageFireAffinity()
        {
            base.name = "SE_VL_MageFireAffinity";
            m_name = "Flame Affinity";
            m_tooltip = "Concentration on Fire magic.\nPassive: Generates Fire Charges.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("StaffFireball");
                if (prefab) m_icon = prefab.GetComponent<ItemDrop>()?.m_itemData?.GetIcon();
            }
        }
    }

    public class SE_MageFrostAffinity : SE_MageAffinityBase
    {
        protected override string BaseDisplayName => "Frost Affinity";

        public SE_MageFrostAffinity()
        {
            base.name = "SE_VL_MageFrostAffinity";
            m_name = "Frost Affinity";
            m_tooltip = "Concentration on Frost magic.\nPassive: Generates Frost Charges.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("StaffIceShards");
                if (prefab) m_icon = prefab.GetComponent<ItemDrop>()?.m_itemData?.GetIcon();
            }
        }
    }

    public class SE_MageArcaneAffinity : SE_MageAffinityBase
    {
        protected override string BaseDisplayName => "Arcane Affinity";

        public SE_MageArcaneAffinity()
        {
            base.name = "SE_VL_MageArcaneAffinity";
            m_name = "Arcane Affinity";
            m_tooltip = "Concentration on Arcane magic.\nPassive: Generates Arcane Charges.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("StaffShield");
                if (prefab) m_icon = prefab.GetComponent<ItemDrop>()?.m_itemData?.GetIcon();
            }
        }
    }
}