using UnityEngine;

namespace ValheimLegends;

public class SE_Regeneration : StatusEffect
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Regeneration")]
	public float m_damageInterval = 2f;

	public static float m_baseTTL = 20f;

	public float m_TTLPerDamagePlayer = 2f;

	public float m_TTLPerDamage = 2f;

	public float m_TTLPower = 0.5f;

	public float m_HealAmount = 2f;

	private float m_timer = 0f;

	public SE_Regeneration()
	{
		base.name = "SE_VL_Regeneration";
		m_icon = AbilityIcon;
		m_tooltip = "Regeneration";
		m_name = "Regeneration";
		m_activationAnimation = "vfx_Potion_health_medium";
		m_ttl = m_baseTTL;
	}

	public override void SetLevel(int itemLevel, float skillLevel)
	{
		if (itemLevel == 1 || itemLevel >= 1000)
		{
			m_HealAmount = skillLevel;
			m_ttl = itemLevel >= 1000 ? itemLevel / 1000f : m_baseTTL;
		}
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_damageInterval;
			m_character.Heal(m_HealAmount);
		}
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
