using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_Lightningaffinity : StatusEffect
{
	public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("DragonTear").GetComponent<ItemDrop>().m_itemData.GetIcon();

	public static GameObject GO_SEFX;

	public bool doOnce = true;

	public SE_Lightningaffinity()
	{
		base.name = "SE_VL_Lightningaffinity";
		m_icon = ZNetScene.instance.GetPrefab("DragonTear").GetComponent<ItemDrop>().m_itemData.GetIcon();
		m_tooltip = "Attacks are imbued with Lightning, jumps to nearby targets";
		m_name = "Enchant Lightning";
		doOnce = true;
	}
	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			if (m_character.IsPlayer())
			{
				float level = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
					.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
				m_tooltip = "Attacks are imbued with Lightning" +
					"\n" + ((0.1f + (level / 300f)) * 100f).ToString("#.#") + "% chance to hit for extra " + (0.1f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 4f) * (1f + (level / 150f))).ToString("#.#") + "-" + (1.9f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 4f) * (1f + (level / 150f))).ToString("#.#") + " Lightning damage that jumps for nearby targets";
			}
		}
		base.UpdateStatusEffect(dt);
	}
	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
