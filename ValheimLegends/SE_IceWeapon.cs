using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_IceWeapon : StatusEffect
{
	public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("StaffIceShards").GetComponent<ItemDrop>().m_itemData.GetIcon();

	public static GameObject GO_SEFX;

	public bool doOnce = true;

	public SE_IceWeapon()
	{
		base.name = "SE_VL_IceWeapon";
		m_icon = ZNetScene.instance.GetPrefab("StaffIceShards").GetComponent<ItemDrop>().m_itemData.GetIcon();
		m_tooltip = "Attacks are imbued with Frost";
		m_name = "Ice Weapon";
		doOnce = true;
	}

	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			float level = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
            m_tooltip = "Attacks are imbued with Frost " +
                "\n" + Mathf.Max(((0.025f + (level / 1200f)) * 100f), 0.1f).ToString("#.#") + "% chance to hit for extra " + Mathf.Max(0.9f * (EpicMMOSystem.LevelSystem.Instance.getLevel()) * (1f + (level / 150f)),0.1f).ToString("#.#") + "-" + (1.1f * (EpicMMOSystem.LevelSystem.Instance.getLevel()) * (1f + (level / 150f))).ToString("#.#") + " Frost damage";
        }
		base.UpdateStatusEffect(dt);
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
