using UnityEngine;
using System.Linq;
using HarmonyLib;
using System.Collections.Generic;

namespace ValheimLegends
{
    public class SE_MageAffinityBase : StatusEffect
    {
        public int m_currentCharges = 0;
        public float m_chargeTimer = 0f;
        public bool isFocused = false;

        // Configurações de Regeneração
        protected float regenIntervalFocused = 10f;
        protected float regenIntervalUnfocused = 30f; // Nova regra
        protected float regenIntervalResting = 1.0f;

        public override void Setup(Character character)
        {
            base.Setup(character);
            m_currentCharges = 5;
            isFocused = false;
            m_ttl = 0;
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            if (!m_character.IsPlayer()) return;
            Player player = m_character as Player;

            float evocationLevel = 0f;
            Skills.Skill skill = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef);
            if (skill != null) evocationLevel = skill.m_level;

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
                    interval = regenIntervalUnfocused; // 30s para não focados
                }

                // Verifica se atingiu o tempo
                if (m_chargeTimer >= interval)
                {
                    m_currentCharges++;
                    m_chargeTimer = 0f; // Reinicia o timer ao ganhar a carga
                }
            }

            string focusIcon = isFocused ? " <color=yellow>👁️</color>" : "";
            m_name = $"{m_name.Split(':')[0]}: <color=orange>{m_currentCharges}</color>/{maxCharges}{focusIcon}";
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

            float evocationLevel = 0f;
            Skills.Skill skill = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef);
            if (skill != null) evocationLevel = skill.m_level;

            int maxCharges = 10 + Mathf.FloorToInt(evocationLevel * 0.2f);
            if (maxCharges > 30) maxCharges = 30;

            m_currentCharges += amount;
            if (m_currentCharges > maxCharges) m_currentCharges = maxCharges;
        }

        public void SetFocus(bool focused)
        {
            isFocused = focused;
            // Opcional: Reiniciar o timer ao mudar o foco para evitar abusos ou manter para continuidade
            // m_chargeTimer = 0f; 
        }

        private static bool IsResting(Player p)
        {
            try
            {
                string[] candidates = { "InRestingArea", "InComfortZone", "InShelter", "InSafeZone" };
                foreach (var name in candidates)
                {
                    var mi = AccessTools.Method(p.GetType(), name);
                    if (mi != null && mi.ReturnType == typeof(bool) && mi.GetParameters().Length == 0)
                        return (bool)mi.Invoke(p, null);
                }
                return true;
            }
            catch { return true; }
        }
    }

    // --- Subclasses (Mantidas iguais) ---

    public class SE_MageFireAffinity : SE_MageAffinityBase
    {
        public SE_MageFireAffinity()
        {
            base.name = "SE_VL_MageFireAffinity";
            m_name = "Flame Affinity";
            m_tooltip = "Concentration on Fire magic.\nPassive: Generates Fire Charges.";
            m_icon = ZNetScene.instance.GetPrefab("StaffFireball").GetComponent<ItemDrop>().m_itemData.GetIcon();
        }
    }

    public class SE_MageFrostAffinity : SE_MageAffinityBase
    {
        public SE_MageFrostAffinity()
        {
            base.name = "SE_VL_MageFrostAffinity";
            m_name = "Frost Affinity";
            m_tooltip = "Concentration on Frost magic.\nPassive: Generates Frost Charges.";
            m_icon = ZNetScene.instance.GetPrefab("StaffIceShards").GetComponent<ItemDrop>().m_itemData.GetIcon();
        }
    }

    public class SE_MageArcaneAffinity : SE_MageAffinityBase
    {
        public SE_MageArcaneAffinity()
        {
            base.name = "SE_VL_MageArcaneAffinity";
            m_name = "Arcane Affinity";
            m_tooltip = "Concentration on Arcane magic.\nPassive: Generates Arcane Charges.";
            m_icon = ZNetScene.instance.GetPrefab("StaffShield").GetComponent<ItemDrop>().m_itemData.GetIcon();
        }
    }
}