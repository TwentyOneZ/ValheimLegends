using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ValheimLegends;

public class SE_Rogue : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Rogue")]
	public static float m_baseTTL = 2f;

	private float m_timer = 0f;

	public int hitCount = 1;

	private float m_interval = 20f;

	public int maxHitCount = 1;

	public List<int> lastSnatched = new List<int>();

	public SE_Rogue()
	{
		base.name = "SE_VL_Rogue";
		m_icon = AbilityIcon;
		m_tooltip = "Rogue";
		m_name = "Rogue";
		m_ttl = m_baseTTL;
		lastSnatched = new List<int>();
		lastSnatched.Clear();
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		if (m_character.IsSneaking())
		{
			speed *= 1.5f + 0.01f * m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
		}
		base.ModifySpeed(baseSpeed, ref speed, character, dir);
	}

	public override void UpdateStatusEffect(float dt)
	{
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			maxHitCount = 1 + Mathf.RoundToInt(m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * 0.1f * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f)));
			m_timer = m_interval * VL_GlobalConfigs.c_rogueTrickCharge;
			hitCount++;
			hitCount = Mathf.Clamp(hitCount, 0, maxHitCount);
			if (lastSnatched == null)
			{
				lastSnatched = new List<int>();
				lastSnatched.Clear();
			}
			if (lastSnatched.Count > 50)
			{
				lastSnatched.Clear();
			}
		}
		m_ttl = hitCount;
		m_time = 0f;
		base.UpdateStatusEffect(dt);
	}

	public override bool IsDone()
	{
		return ValheimLegends.vl_player.vl_class != ValheimLegends.PlayerClass.Rogue;
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer() && ValheimLegends.vl_player.vl_class == ValheimLegends.PlayerClass.Rogue;
	}
}
