using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_BiomePlains : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_BiomePlains")]
	public static float m_baseTTL = 600f;

	public float resistModifier = 0.9f;

	public float dodgeModifier = 0.8f;

	public float speedBonus = 1.1f;

	public float biomeDamageOffset = 26f;

	public float casterPower = 1f;

	public float cooldownChance = 0.25f;

	public float casterLevel = 0f;

	public bool doOnce = true;

	public Player caster;

	public SE_BiomePlains()
	{
		base.name = "SE_VL_BiomePlains";
		m_icon = AbilityIcon;
		m_tooltip = $"Chance to reduce cooldown os class skills on attack\n" +
			$"Dodge cost reduced\n" +
			$"Run speed increased\n";
		m_name = "Biome: Plains";
		m_ttl = m_baseTTL;
		doOnce = true;
	}

	public override void ModifySpeed(float baseSpeed, ref float speed, Character character, UnityEngine.Vector3 dir)
	{
		speed *= speedBonus;
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
				casterLevel = 50;
				casterPower = 80;
			}
			float num = casterLevel * 10f / 6f * (1f + casterPower / 300f);
			biomeDamageOffset = (5f + (casterLevel / 2f) * (1f + casterPower / 300f)) * VL_GlobalConfigs.g_DamageModifer;
			m_ttl = m_baseTTL + 3f * num;
			speedBonus = 1.05f + 0.001f * num;
			resistModifier = 0.9f - 0.001f * num;
			dodgeModifier = 0.8f - 0.008f * num;
			cooldownChance = 0.25f + (num / 300f);
			m_tooltip = $"{(cooldownChance * 100f).ToString("#.#")} % chance to reduce cooldown os class skills on attack\n" +
				$"Dodge cost reduced by {(dodgeModifier * 100f).ToString("#.#")} %\n" +
				$"Run speed increased by {(speedBonus * 100f).ToString("#.#")} %\n";
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
