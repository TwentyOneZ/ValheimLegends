using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace ValheimLegends;

public class SE_FlameArmor : StatusEffect
{
	public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("SurtlingCore").GetComponent<ItemDrop>().m_itemData.GetIcon();

	public static GameObject GO_SEFX;

	private static int Light_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost");

	public bool doOnce = true;

	public float regenBonus = 1f;

	private float m_timer = 0f;

	private float m_interval = 5f;

	public float casterPower = 1f;

	public float casterLevel = 0f;

	private static GameObject GO_Light;

	private static Projectile P_Light;

	public float resistModifier = 0.9f;
	public SE_FlameArmor()
	{
		base.name = "SE_VL_FlameArmor";
		m_icon = ZNetScene.instance.GetPrefab("SurtlingCore").GetComponent<ItemDrop>().m_itemData.GetIcon();
		m_tooltip = "Your wounds are being cauterized. Immune to Cold. Magical resist increased.";
		m_name = "Flame Armor";
		doOnce = true;
	}
	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
			casterPower = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
			float num = casterLevel * 10f / 6f * (1f + casterPower / 150f);
			regenBonus = (3f + 0.3f * num) * VL_GlobalConfigs.g_DamageModifer;
			resistModifier = 0.95f - 0.001f * casterPower;
			m_tooltip = "Your wounds are being cauterized. Immune to Cold. Magical resist increased." +
				"\n Magical damage resist increased by " + ((1f - resistModifier) * 100f).ToString("#.#") + "% " +
				"\n" + "Heals " + (regenBonus).ToString("#.#") + " * the percentage of you lost health every 5 seconds.";

		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_interval;
			m_character.Heal(regenBonus * (1f - m_character.GetHealthPercentage()));
		}
		base.UpdateStatusEffect(dt);
	}

	public override bool IsDone()
	{
		return base.IsDone();
	}

	public override void OnDamaged(HitData hit, Character m_character)
	{
		hit.m_damage.m_spirit *= resistModifier;
		hit.m_damage.m_poison *= resistModifier;
		hit.m_damage.m_fire *= resistModifier;
		hit.m_damage.m_frost *= resistModifier;
		hit.m_damage.m_lightning *= resistModifier;
		base.OnDamaged(hit, m_character);
	}


	public override bool CanAdd(Character character)
	{
		return character.IsPlayer();
	}
}
