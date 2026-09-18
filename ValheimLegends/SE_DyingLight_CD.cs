using UnityEngine;

namespace ValheimLegends;

public class SE_DyingLight_CD : StatusEffect
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	public SE_DyingLight_CD()
	{
		base.name = "SE_VL_DyingLight_CD";
		m_icon = AbilityIcon;
		m_tooltip = "Dying Light has prevented a killing blow. Dying Light will not trigger again until this cooldown expires.";
		m_name = "Dying Light";
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
