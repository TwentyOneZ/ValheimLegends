using UnityEngine;
using ValheimLegends;

namespace ValheimLegends
{
    public class SE_ElementalMastery : StatusEffect
    {
        private float m_timer = 0f;
        private const float m_consumptionInterval = 15f;

        // Hash para buscar a afinidade de forma performática
        private int Hash_ArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();

        public SE_ElementalMastery()
        {
            base.name = "SE_VL_ElementalMastery";
            m_name = "Elemental Mastery";
            m_tooltip = "Your spells are empowered by your weapon's elemental damage.\nConsumes 1 Arcane Charge every 15s.";
            m_startMessage = "Elemental Mastery Activated";
            m_stopMessage = "Elemental Mastery Deactivated";

            // Verificação de segurança para o ZNetScene
            if (ZNetScene.instance)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab("TrophyEikthyr");
                if (prefab)
                {
                    m_icon = prefab.GetComponent<ItemDrop>().m_itemData.GetIcon();
                }
            }
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            m_timer += dt;
            if (m_timer >= m_consumptionInterval)
            {
                m_timer = 0f; // Reinicia o timer para os próximos 15s

                if (m_character.IsPlayer())
                {
                    // Busca a afinidade no jogador
                    var seman = m_character.GetSEMan();
                    SE_MageArcaneAffinity affinity = seman.GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;

                    // Verifica se tem afinidade e cargas suficientes
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        affinity.ConsumeCharges(1);
                        // Opcional: Efeito visual discreto de consumo de mana/carga
                    }
                    else
                    {
                        // Remove o buff se não conseguir pagar o custo
                        m_character.Message(MessageHud.MessageType.TopLeft, "Elemental Mastery fades (No Charges)");
                        seman.RemoveStatusEffect(this.name.GetStableHashCode());
                    }
                }
            }
        }
    }
}