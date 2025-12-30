using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class SE_Berserk : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Berserk")]
	public static float m_baseTTL = 18f;

	public float speedModifier = 1.2f;

	public float damageModifier = 1.2f;

	public float healthAbsorbPercent = 0.15f;

	private float m_timer = 0f;

	private float m_interval = 3f;

	private float savedStaminaRegenDelay = 1f;

	public SE_Berserk()
	{
		base.name = "SE_VL_Berserk";
		m_icon = AbilityIcon;
		m_tooltip = "Berserk";
		m_name = "Berserk";
		m_ttl = m_baseTTL;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		speed *= speedModifier;
	}

	public override void Setup(Character character)
	{
		savedStaminaRegenDelay = Traverse.Create((Player)character).Field("m_staminaRegenDelay").GetValue<float>();
		Traverse.Create((Player)character).Field("m_staminaRegenDelay").SetValue(0f);
		base.Setup(character);
	}

	public override void UpdateStatusEffect(float dt)
	{
		base.UpdateStatusEffect(dt);
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_interval;
			HitData hitData = new HitData();
			hitData.m_damage.m_spirit = Mathf.Clamp(0.05f * m_character.GetMaxHealth(), 1f, 15f);
			hitData.m_point = m_character.GetEyePoint();
			if (m_character.GetHealth() < Mathf.Clamp(0.10f * m_character.GetMaxHealth(), 5f, 30f))
			{
				SE_Berserk sE_Berserk = (SE_Berserk)m_character.GetSEMan().GetStatusEffect("SE_VL_Berserk".GetStableHashCode());
				m_character.GetSEMan().RemoveStatusEffect(sE_Berserk, quiet: true);
				m_character.Message(MessageHud.MessageType.Center, "Low health!");
				m_character.Message(MessageHud.MessageType.TopLeft, "Berserk dissipated due to low health!");
			}
			else
			{ 
				m_character.ApplyDamage(hitData, showDamageText: true, triggerEffects: true);
			}
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_deathsquito_hit"), m_character.GetCenterPoint(), UnityEngine.Quaternion.identity);
		}
	}

	public override bool IsDone()
	{
		if (m_ttl > 0f && m_time > m_ttl)
		{
			Traverse.Create((Player)m_character).Field("m_staminaRegenDelay").SetValue(savedStaminaRegenDelay);
		}
		return base.IsDone();
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
