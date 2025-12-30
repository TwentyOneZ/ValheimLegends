using System.Linq;
using UnityEngine;

namespace ValheimLegends;

public class SE_Valkyrie : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Valkyrie")]
	public static float m_baseTTL = 5f;

	private float m_timer = 0f;

	public int hitCount = 0;

	private float m_interval = 15f;

	public int maxHitCount = 5;

	public bool refreshed = false;

	public SE_Valkyrie()
	{
		base.name = "SE_VL_Valkyrie";
		m_icon = AbilityIcon;
		m_tooltip = "Valkyrie";
		m_name = "Valkyrie";
		m_ttl = m_baseTTL;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		base.ModifySpeed(baseSpeed, ref speed, character, dir);
	}

	public override void UpdateStatusEffect(float dt)
	{
		if (refreshed)
		{
			m_timer = m_interval * VL_GlobalConfigs.c_valkyrieChargeDuration;
			refreshed = false;
		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			maxHitCount = 8 + Mathf.RoundToInt(Mathf.Sqrt(Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f))));
			m_timer = m_interval * VL_GlobalConfigs.c_valkyrieChargeDuration;
			hitCount--;
			hitCount = Mathf.Clamp(hitCount, 0, maxHitCount);
		}
		m_ttl = hitCount;
		m_time = 0f;
		base.UpdateStatusEffect(dt);
	}

	public override bool IsDone()
	{
		return ValheimLegends.vl_player.vl_class != ValheimLegends.PlayerClass.Valkyrie;
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer() && ValheimLegends.vl_player.vl_class == ValheimLegends.PlayerClass.Valkyrie;
	}
}
