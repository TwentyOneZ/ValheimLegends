using UnityEngine;

namespace ValheimLegends;

public class SE_CDReactivearmor : StatusEffect
{
	public static Sprite AbilityIcon = ValheimLegends.Ability1_Sprite;

	public static GameObject GO_SEFX;

	public SE_CDReactivearmor()
	{
		base.name = "SE_VL_CDReactivearmor";
		m_icon = ValheimLegends.Ability1_Sprite;
		m_tooltip = "Reactive Armor Cooldown";
		m_name = "Reactive Armor Cooldown";
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
