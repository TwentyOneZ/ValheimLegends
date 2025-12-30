using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_BiomeBlackForest : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_BiomeBlackForest")]
	public static float m_baseTTL = 600f;

	public float carryModifier = 50f;

	public bool underRoof = true;

	public float biomeDamageOffset = 26f;

	public float reflectModifier = 0.25f;

	public float critChance = 0.05f;
	
	public float casterPower = 1f;

	public float casterLevel = 0f;

	public bool doOnce = true;

	public Player caster;

	public SE_BiomeBlackForest()
	{
		base.name = "SE_VL_BiomeBlackForest";
		m_icon = AbilityIcon;
		m_tooltip = $"You are sheltered anywhere.\n" +
			$"Chance to deal backstab damage\n" +
			$"Thorns: reflect part of received damage to attackers\n";
		m_name = "Biome: Black Forest";
		m_ttl = m_baseTTL;
		doOnce = true;
	}

	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			if (m_character.IsPlayer() && casterLevel == 0f)
			{
				casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
				casterPower = m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
					.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
			}
			else if (casterLevel == 0f)
			{
				casterLevel = 15;
				casterPower = 20;
			}
			float num = casterLevel * 10f / 6f * (1f + casterPower / 300f);
			biomeDamageOffset = (2f + (casterLevel / 5f) * (1f + casterPower / 300f)) * VL_GlobalConfigs.g_DamageModifer;
			m_ttl = m_baseTTL + 3f * num;
			carryModifier = 50f + num;
			reflectModifier = 0.2f + 0.002f * num;
			critChance = 0.02f + 0.0004f * num;
			m_tooltip = $"You are sheltered anywhere.\n" +
				$"{(critChance * 100f).ToString("#.#")} % chance to deal backstab damage\n" +
				$"{(critChance * 100f).ToString("#.#")} % chance to steal coins when attacking with melee weapons\n" +
				$"Thorns: reflect {(reflectModifier * 100f).ToString("#.#")} % of received damage to attackers\n";
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomeMeadows".GetStableHashCode()))
			{
				SE_BiomeMeadows statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomeMeadows".GetStableHashCode()) as SE_BiomeMeadows;
				if (statusEffect.caster == caster)
				{
					m_character.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomeSwamp".GetStableHashCode()))
			{
				SE_BiomeSwamp statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomeSwamp".GetStableHashCode()) as SE_BiomeSwamp;
				if (statusEffect.caster == caster)
				{
					m_character.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomeMountain".GetStableHashCode()))
			{
				SE_BiomeMountain statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomeMountain".GetStableHashCode()) as SE_BiomeMountain;
				if (statusEffect.caster == caster)
				{
					m_character.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomePlains".GetStableHashCode()))
			{
				SE_BiomePlains statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomePlains".GetStableHashCode()) as SE_BiomePlains;
				if (statusEffect.caster == caster)
				{
					m_character.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomeOcean".GetStableHashCode()))
			{
				SE_BiomeOcean statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomeOcean".GetStableHashCode()) as SE_BiomeOcean;
				if (statusEffect.caster == caster)
				{
					m_character.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomeMist".GetStableHashCode()))
			{
				SE_BiomeMist statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomeMist".GetStableHashCode()) as SE_BiomeMist;
				if (statusEffect.caster == caster)
				{
					m_character.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomeAsh".GetStableHashCode()))
			{
				SE_BiomeAsh statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomeAsh".GetStableHashCode()) as SE_BiomeAsh;
				if (statusEffect.caster == caster)
				{
					m_character.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
		}
		base.UpdateStatusEffect(dt);
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
