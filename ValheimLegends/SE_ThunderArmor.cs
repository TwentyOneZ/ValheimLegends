using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_ThunderArmor : StatusEffect
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	public bool doOnce = true;

	public float speedBonus = 1.1f;
	public SE_ThunderArmor()
	{
		base.name = "SE_VL_ThunderArmor";
		m_icon = AbilityIcon;
		m_tooltip = "Chance to burst a chain lightning back at attackers. Increases movement speed.";
		m_name = "Thunder Armor";
		doOnce = true;
	}
	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			float level = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
			speedBonus = 1.05f + 0.001f * level;
			m_tooltip = "Chance to burst a chain lightning back at attackers. Increases movement speed." +
				"\n" + ((0.1f + (level / 300f)) * 100f).ToString("#.#") + "% chance to cast a chain lightning of " + Mathf.Max((0.1f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 4f) * (1f + (level / 150f))),0.1f).ToString("#.#") + "-" + Mathf.Max((1.9f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 4f) * (1f + (level / 150f))),0.1f).ToString("#.#") + " Lightning damage that jumps for nearby targets" +
				"\nMovement Speed increased in " + ((speedBonus - 1f) * 100f).ToString("#.#") + "%";
		}
		base.UpdateStatusEffect(dt);
	}
	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		speed *= speedBonus;
		base.ModifySpeed(baseSpeed, ref speed, character, dir);
	}

	public override bool IsDone()
	{
		if (ValheimLegends.vl_player == null || ValheimLegends.vl_player.vl_class != ValheimLegends.PlayerClass.Enchanter)
		{
			return true;
		}
		return base.IsDone();
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
		return base.CanAdd(character) && character.IsPlayer() && ValheimLegends.vl_player != null && ValheimLegends.vl_player.vl_class == ValheimLegends.PlayerClass.Enchanter;
	}
}
