using UnityEngine;

namespace ValheimLegends;

public class SE_CharmImmunity : StatusEffect
{
	public static Sprite AbilityIcon = ValheimLegends.Ability1_Sprite;

	public static GameObject GO_SEFX;

	public static float m_baseTTL = 30f;

	private float m_timer = 30f;

	private float m_interval = 3f;

	private bool finished = false;


	public SE_CharmImmunity()
	{
		base.name = "SE_VL_CharmImmunity";
		m_icon = ValheimLegends.Ability1_Sprite;
		m_tooltip = "Charm Immunity";
		m_name = "Charm Immunity";
		m_ttl = m_baseTTL;
}

public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_interval;
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_AbsorbSpirit"), m_character.GetCenterPoint(), UnityEngine.Quaternion.identity);
		}
		if (GetRemaningTime() <= m_interval && !finished)
		{
			finished = true;
			Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_crow_death"), m_character.GetEyePoint(), UnityEngine.Quaternion.identity);
		}
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
