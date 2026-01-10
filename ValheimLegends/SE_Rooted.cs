using UnityEngine;

namespace ValheimLegends
{
    public class SE_Rooted : SE_Stats
    {
        public static float m_baseTTL = 1f;

        public SE_Rooted()
        {
            name = "SE_VL_Rooted";
            m_name = "Rooted";
            m_tooltip = "Rooted";
            m_ttl = m_baseTTL;

            // opcional: use o ícone da habilidade 3
            m_icon = ValheimLegends.Ability3_Sprite;
        }

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
        {
            // imobiliza
            speed *= 0f;
            base.ModifySpeed(baseSpeed, ref speed, character, dir);
        }

        public override bool CanAdd(Character character) => true;
    }
}
