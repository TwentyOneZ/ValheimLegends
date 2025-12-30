using UnityEngine;

namespace ValheimLegends;

public class SE_Enrage : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Enrage")]
	public static float m_baseTTL = 16f;

	public float speedModifier = 1.2f;

	private float m_timer = 0f;

	private float m_interval = 1f;

	public float staminaModifier = 10f;

	public bool doOnce = true;

	public SE_Enrage()
	{
		base.name = "SE_VL_Enrage";
		m_icon = AbilityIcon;
		m_tooltip = "Enrage";
		m_name = "Enrage";
		m_ttl = m_baseTTL;
		doOnce = true;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		speed *= speedModifier;
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		if (doOnce)
		{
			doOnce = false;
			float num = EpicMMOSystem.LevelSystem.Instance.getLevel() * 10f / 6f;
			m_ttl = 20f + 0.2f * num;
			staminaModifier = (5f + 0.1f * num) * VL_GlobalConfigs.c_shamanEnrage;
			speedModifier = 1.2f + 0.002f * num * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_interval;
			m_character.AddStamina(staminaModifier);
		}
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
