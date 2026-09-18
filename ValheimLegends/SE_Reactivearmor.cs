using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_Reactivearmor : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Reactivearmor")]

	public float staminaModifier = 1f;

	public static float m_baseTTL = 3f;

	public int hitCount = 3;

	public bool doOnce = true;

	public SE_Reactivearmor()
	{
		base.name = "SE_VL_Reactivearmor";
		m_icon = AbilityIcon;
		m_tooltip = "Reactive Armor";
		m_name = "Reactive Armor";
		doOnce = true;
		m_ttl = m_baseTTL;
		hitCount = (int)m_ttl;
	}

	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			float level = Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
			staminaModifier = (1f - (level / 300f));
			hitCount = 3 + Mathf.RoundToInt(Mathf.Sqrt(level * 2));
		}
		m_ttl = hitCount;
		m_time = 0f;
		base.UpdateStatusEffect(dt);
	}

	public override bool IsDone()
	{
		if (ValheimLegends.vl_player == null || ValheimLegends.vl_player.vl_class != ValheimLegends.PlayerClass.Metavoker)
		{
			return true;
		}
		return base.IsDone();
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
		return base.CanAdd(character) && character.IsPlayer() && ValheimLegends.vl_player != null && ValheimLegends.vl_player.vl_class == ValheimLegends.PlayerClass.Metavoker;
	}
}
