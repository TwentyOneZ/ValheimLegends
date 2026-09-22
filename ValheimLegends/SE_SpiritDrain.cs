using UnityEngine;

namespace ValheimLegends;

public class SE_SpiritDrain : StatusEffect
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_SpiritDrain")]
	public float m_damageInterval = 1f;

	public static float m_baseTTL = 10f;

	private float m_timer = 0f;

	public float damageModifier = 1f;
	public ZDOID attackerId = ZDOID.None;

	public override void SetLevel(int itemLevel, float skillLevel)
	{
		if (itemLevel == 1) damageModifier = skillLevel;
	}

	public SE_SpiritDrain()
	{
		base.name = "SE_VL_SpiritDrain";
		m_icon = AbilityIcon;
		m_tooltip = "Spirit Drain";
		m_name = "Spirit Drain";
		m_ttl = m_baseTTL;
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_damageInterval;
			HitData hitData = new HitData();
			hitData.m_damage.m_spirit = damageModifier;
			hitData.m_attacker = attackerId;
			hitData.m_skill = ValheimLegends.EvocationSkill;
			hitData.m_point = m_character.GetEyePoint();
			m_character.ApplyDamage(hitData, showDamageText: true, triggerEffects: true);
			Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_wraith_hit"), m_character.GetCenterPoint(), UnityEngine.Quaternion.identity);
		}
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
