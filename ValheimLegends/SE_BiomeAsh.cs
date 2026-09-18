using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_BiomeAsh : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_BiomeAsh")]
	public static float m_baseTTL = 600f;

	public float resistModifier = 0.8f;

	public float biomeDamageOffset = 26f;

	public float casterPower = 1f;

	public bool doLight = true;

	public float casterLevel = 0f;

	private float m_timer = 0f;

	private float m_interval = 3f;

	public bool doOnce = true;

	public Player caster;

	public Light biomeLight;
	public SE_BiomeAsh()
	{
		base.name = "SE_VL_BiomeAsh";
		m_icon = AbilityIcon;
		m_tooltip = $"Deals fire damage on attack\n" +
			$"Fire resistance increased \n" +
			$"Emits a small amount light around you.\n";
		m_name = "Biome: Ashlands";
		m_ttl = m_baseTTL;
		doOnce = true;
		doLight = true;
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
				casterLevel = 60;
				casterPower = 120;
			}
			float num = casterLevel * 10f / 6f * (1f + casterPower / 300f);
			m_ttl = m_baseTTL + 5f * num;
			biomeDamageOffset = (5f + (casterLevel / 2f) * (1f + casterPower / 300f)) * VL_GlobalConfigs.g_DamageModifer;
			resistModifier = 0.85f - 0.001f * num;
			m_tooltip = $"Deals {biomeDamageOffset.ToString("#.#")} fire damage on attack\n" +
				$"Fire resistance increased by {((1f - resistModifier) * 100f).ToString("#.#")} %\n" +
				$"Emits a small amount light around you.\n";
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
		}
		if (doLight)
		{
			doLight = false;
			m_character.gameObject.AddComponent<Light>();
			m_character.GetComponent<Light>().range = 30f;
			m_character.GetComponent<Light>().color = new Color(233f, 240f, 226f);
			m_character.GetComponent<Light>().intensity = 0.0035f;
			m_character.GetComponent<Light>().enabled = true;
			biomeLight = m_character.GetComponent<Light>();
		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_interval;
			if (GetRemaningTime() <= m_interval && biomeLight != null)
			{
				Object.Destroy(biomeLight);
			}
		}
		base.UpdateStatusEffect(dt);
	}

	public override void OnDamaged(HitData hit, Character m_character)
	{
		hit.m_damage.m_fire *= resistModifier;
		base.OnDamaged(hit, m_character);
	}

	public override bool CanAdd(Character character)
	{
		return true;
	}
}
