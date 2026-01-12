using System.Linq;
using UnityEngine;

namespace ValheimLegends;

public class SE_Monk : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Monk")]
	public static float m_baseTTL = 5f;

	public float m_timer = 0f;

	public int hitCount = 0;

	public float m_interval = 12f;

	public int maxHitCount = 5;

	public bool surging = false;

	public bool refreshed = false;

	private float m_SurgeTimer = 2.0f;

	private float m_SurgeInterval = 2.0f;

	public SE_Monk()
	{
		base.name = "SE_VL_Monk";
		m_icon = AbilityIcon;
		m_tooltip = "Monk";
		m_name = "Monk";
		m_ttl = m_baseTTL;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		if (surging)
		{
			float level = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			speed *= 1.2f + 0.003f * level;
		}
		base.ModifySpeed(baseSpeed, ref speed, character, dir);
	}

	public override void UpdateStatusEffect(float dt)
	{
		if (refreshed) {
			m_timer = m_interval * VL_GlobalConfigs.c_monkChiDuration;
			refreshed = false;
		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			maxHitCount = 8 + Mathf.RoundToInt(m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * 0.2f * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f)));
			m_timer = m_interval * VL_GlobalConfigs.c_monkChiDuration;
			hitCount--;
			hitCount = Mathf.Clamp(hitCount, 0, maxHitCount);
		}
		m_SurgeTimer -= dt;
		if (surging && m_SurgeTimer <= 0f)
		{
			hitCount--;
			refreshed = true;
			m_SurgeTimer = m_SurgeInterval;
			float level = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			m_character.Heal(5f + m_character.GetMaxHealth() * level * VL_GlobalConfigs.c_monkSurge / 2000f);
			//m_character.AddStamina(0.2f * level * VL_GlobalConfigs.c_monkSurge);
			if (hitCount <= 0)
			{
				hitCount = 0;
				surging = false;
				m_SurgeTimer = m_SurgeInterval;
			}
		}
		m_ttl = hitCount;
		m_time = 0f;
		base.UpdateStatusEffect(dt);
	}

	public override bool IsDone()
	{
		return ValheimLegends.vl_player.vl_class != ValheimLegends.PlayerClass.Monk;
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer() && ValheimLegends.vl_player.vl_class == ValheimLegends.PlayerClass.Monk;
	}
}
