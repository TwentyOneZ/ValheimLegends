using UnityEngine;
using ValheimLegends;

namespace ValheimLegends
{
    public class SE_Frozen : StatusEffect
    {
        public SE_Frozen()
        {
            base.name = "SE_VL_Frozen";
            m_name = "Frozen";
            m_tooltip = "Frozen solid. Unable to move.";
            m_startMessage = "Frozen";
            m_stopMessage = "Thawed";
            m_ttl = 10f; // Duração base do congelamento

            // Tenta pegar o icone do Freezing vanilla ou um cubo de gelo
            if (ZNetScene.instance)
            {
                // Ícone placeholder: Freeze Gland
                GameObject prefab = ZNetScene.instance.GetPrefab("FreezeGland");
                if (prefab) m_icon = prefab.GetComponent<ItemDrop>().m_itemData.GetIcon();
            }
        }

        public override void ModifySpeed(float baseSpeed, ref float speed, Character character, Vector3 dir)
        {
            // imobiliza
            speed *= 0f;
            base.ModifySpeed(baseSpeed, ref speed, character, dir);
        }

        public override void Setup(Character character)
        {
            base.Setup(character);
            if (character)
            {
                // Efeito visual de congelamento (VFX)
                GameObject vfx = ZNetScene.instance.GetPrefab("vfx_Freezing");
                if (vfx)
                {
                    Transform vfxTrans = UnityEngine.Object.Instantiate(vfx, character.transform).transform;
                    vfxTrans.localPosition = Vector3.up;
                }
            }
        }
    }
}