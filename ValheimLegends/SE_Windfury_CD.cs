using UnityEngine;

namespace ValheimLegends;

public class SE_Windfury_CD : StatusEffect
{
	public static Sprite AbilityIcon = ValheimLegends.Ability1_Sprite;

	public static GameObject GO_SEFX;

	public SE_Windfury_CD()
	{
		base.name = "SE_VL_Windfury_CD";
		m_icon = AbilityIcon;
		m_tooltip = "Windfury won't activate again until you kill a monster or cast Spirit Bomb";
		m_name = "Windfury Cooldown";
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
