using UnityEngine;

namespace ValheimLegends;

public class SE_Shapeshift_CD : StatusEffect
{
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

	public SE_Shapeshift_CD()
	{
        base.name = "SE_VL_Shapeshift_CD";
        if (ZNetScene.instance != null)
        {
            var go = ZNetScene.instance.GetPrefab("TrophyFenring");
            var id = go != null ? go.GetComponent<ItemDrop>() : null;
            if (id != null) m_icon = id.m_itemData.GetIcon();
        }

        m_tooltip = "You need some time before you are able to shapeshift again.";
        m_name = "Shapeshifting Cooldown";
    }

    public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
