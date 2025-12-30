using UnityEngine;

namespace ValheimLegends;

public class SE_Weaken : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Weaken")]
	public static float m_baseTTL = 30f;

	public float interval = 1f;

	public float speedReduction = 0.8f;

	public float damageReduction = 0.15f;

	public float staminaDrain = 0.1f;

	private float m_timer = 1f;

	public SE_Weaken()
	{
		base.name = "SE_VL_Weaken";
		m_icon = AbilityIcon;
		m_tooltip = "Weaken";
		m_name = "Weaken";
		m_ttl = m_baseTTL;
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = interval;
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_WeakenStatus"), m_character.GetEyePoint(), UnityEngine.Quaternion.identity);
		}
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		speed *= speedReduction;
		base.ModifySpeed(baseSpeed, ref speed, character, dir);
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
