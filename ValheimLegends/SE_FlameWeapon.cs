using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_FlameWeapon : StatusEffect
{
	public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("StaffFireball").GetComponent<ItemDrop>().m_itemData.GetIcon();

	public static GameObject GO_SEFX;

	public bool doOnce = true;

	public SE_FlameWeapon()
	{
		base.name = "SE_VL_FlameWeapon";
		m_icon = ZNetScene.instance.GetPrefab("StaffFireball").GetComponent<ItemDrop>().m_itemData.GetIcon();
		m_tooltip = "Attacks are imbued with fire.";
		m_name = "Flame Weapon";
		doOnce = true;
	}
	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			float level = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			m_tooltip = "Attacks are imbued with Fire " +
				"\n" + "Hits do extra " + Mathf.Max(0.5f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 6f) * (1f + (level / 150f)), 0.1f).ToString("#.#") + "-" + Mathf.Max(1.3f * (EpicMMOSystem.LevelSystem.Instance.getLevel() / 6f) * (1f + (level / 150f)),0.1f).ToString("#.#") + " average Fire damage";
		}
		base.UpdateStatusEffect(dt);
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
