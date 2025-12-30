using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_BiomeMountain : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_BiomeMountain")]
	public static float m_baseTTL = 600f;

	public float resistModifier = 0.8f;

	public float staminaRegen = 5f;

	private float m_timer = 0f;

	private float m_interval = 5f;

	public float biomeDamageOffset = 26f;

	public float casterPower = 1f;

	public float casterLevel = 0f;

	public bool doOnce = true;

	public Player caster;

	public SE_BiomeMountain()
	{
		base.name = "SE_VL_BiomeMountain";
		m_icon = AbilityIcon;
		m_tooltip = $"Deals frost damage on attack\n" +
			$"Frost resistance increased\n" +
			$"Cold Immunity\n";
		m_name = "Biome: Mountain";
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
				casterLevel = 40;
				casterPower = 60;
			}
			float num = EpicMMOSystem.LevelSystem.Instance.getLevel() * 10f / 6f * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			biomeDamageOffset = (5f + (casterLevel / 2f) * (1f + casterPower / 300f)) * VL_GlobalConfigs.g_DamageModifer;
			m_ttl = m_baseTTL + 3f * num;
			staminaRegen = 5f + 0.075f * num;
			resistModifier = 0.85f - 0.001f * num;
			m_tooltip = $"Deals {biomeDamageOffset.ToString("#.#")} frost damage on attack\n" +
				$"Frost resistance increased by {((1f - resistModifier) * 100f).ToString("#.#")} %\n" +
				$"Cold Immunity\n";
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
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_interval;
			m_character.AddStamina(staminaRegen);
		}
		base.UpdateStatusEffect(dt);
	}

	public override void OnDamaged(HitData hit, Character attacker)
	{
		hit.m_damage.m_frost *= resistModifier;
		base.OnDamaged(hit, attacker);
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
