using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_IceArmor : StatusEffect
{
	public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("FreezeGland").GetComponent<ItemDrop>().m_itemData.GetIcon();

	public static GameObject GO_SEFX;

	public bool doOnce = true;

	public float resistModifier = 0.9f;

	public SE_IceArmor()
	{
		base.name = "SE_VL_IceArmor";
		m_icon = ZNetScene.instance.GetPrefab("FreezeGland").GetComponent<ItemDrop>().m_itemData.GetIcon();
		m_tooltip = "Ice Armor will reduce physical damage and have a chance to slow attackers";
		m_name = "Ice Armor";
		doOnce = true;
	}

	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			float level = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
			resistModifier = 0.95f - 0.001f * level;
			m_tooltip = "Ice Armor will reduce physical damage and have a chance to slow attackers" +
                "\n Physical damage resist increased by " + ((1f - resistModifier) * 100f).ToString("#.#") + "% " +
                "\n Chance to cast a Frost Nova that will slow attackers: " + (100f * (0.025f + (level / 1200f))).ToString("#.#") + "%";
        }
		base.UpdateStatusEffect(dt);
	}

	public override void OnDamaged(HitData hit, Character m_character)
	{
		hit.m_damage.m_blunt *= resistModifier;
		hit.m_damage.m_pierce *= resistModifier;
		hit.m_damage.m_slash *= resistModifier;
		base.OnDamaged(hit, m_character);
	}

	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
