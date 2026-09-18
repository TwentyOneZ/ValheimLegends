using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_BiomeOcean : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_BiomeOcean")]
	public static float m_baseTTL = 600f;

	public float resistModifier = 0.8f;

	public float blockModifier = 0.8f;

	public float swimSpeed = 1.5f;

	public float biomeDamageOffset = 26f;

	public float casterPower = 1f;

	public float casterLevel = 0f;

	public bool doOnce = true;

	public Player caster;

	public SE_BiomeOcean()
	{
		base.name = "SE_VL_BiomeOcean";
		m_icon = AbilityIcon;
		m_tooltip = $"Deals spirit damage on attack\n" +
			$"Physical resist increased\n" +
			$"Block power increased\n" +
			$"Swim speed increased";
		m_name = "Biome: Ocean";
		m_ttl = m_baseTTL;
		doOnce = true;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		if (m_character.IsSwimming())
		{
			speed *= swimSpeed;
		}
		base.ModifySpeed(baseSpeed, ref speed, character, dir);
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
				casterLevel = 30;
				casterPower = 40;
			}
			float num = casterLevel * 10f / 6f * (1f + casterPower / 300f);
			biomeDamageOffset = (5f + (casterLevel / 2f) * (1f + casterPower / 300f)) * VL_GlobalConfigs.g_DamageModifer;
			m_ttl = m_baseTTL + 3f * num;
			swimSpeed = 1.5f + 0.01f * num;
			resistModifier = 0.9f - 0.001f * num;
			blockModifier = 0.2f + 0.002f * num;
			m_tooltip = $"Deals {biomeDamageOffset.ToString("#.#")} spirit damage on attack\n" +
				$"Block power increased by {(blockModifier * 100f).ToString("#.#")} %\n" +
				$"Physical Resist increased by {(resistModifier * 100f).ToString("#.#")} %\n" +
				$"Swim speed increased by {(swimSpeed * 100f).ToString("#.#")} %";
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomeMeadows".GetStableHashCode()))
			{
				SE_BiomeMeadows statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomeMeadows".GetStableHashCode()) as SE_BiomeMeadows;
				if (statusEffect.caster == caster)
				{
					m_character.GetSEMan().RemoveStatusEffect(statusEffect);
				}
			}
			if (m_character.GetSEMan() != null && m_character.GetSEMan().HaveStatusEffect("SE_VL_BiomeBlackForest".GetStableHashCode()))
			{
				SE_BiomeBlackForest statusEffect = m_character.GetSEMan().GetStatusEffect("SE_VL_BiomeBlackForest".GetStableHashCode()) as SE_BiomeBlackForest;
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

	public override void OnDamaged(HitData hit, Character m_character)
	{
		hit.m_damage.m_blunt *= resistModifier;
		hit.m_damage.m_pierce *= resistModifier;
		hit.m_damage.m_slash *= resistModifier;
		base.OnDamaged(hit, m_character);
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
