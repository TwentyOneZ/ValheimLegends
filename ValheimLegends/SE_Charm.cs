using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_Charm : SE_Stats
{
	public static Sprite AbilityIcon;

	public static GameObject GO_SEFX;

	[Header("SE_VL_Charm")]
	public static float m_baseTTL = 30f;

	public float speedModifier = 1.2f;

	public float healthRegen = 1f;

	public float damageModifier = 1f;

	private float m_timer = 0f;

	private float m_interval = 1f;

	public float charmPower = 1f;

	public bool doOnce = true;

	public Player summoner;

	public Character.Faction originalFaction;

	public SE_Charm()
	{
		base.name = "SE_VL_Charm";
		m_icon = AbilityIcon;
		m_tooltip = "Charm";
		m_name = "VL_Charm";
		m_ttl = m_baseTTL;
		doOnce = true;
	}

	public override void UpdateStatusEffect(float dt)
	{
		if (doOnce)
		{
			doOnce = false;
			Mathf.Clamp(m_ttl, 10f, 40f);
			Mathf.Clamp(charmPower, 1f, m_character.GetMaxHealth());
			m_character.SetTamed(true);
		}
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			//Debug.Log($"EpicMMO Level of " + m_character.m_name.ToString() + ": " + EpicMMOSystem.LevelSystem.Instance.getLevel().ToString("#.#") + " s");
			m_timer = m_interval;
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_boar_pet"), m_character.GetEyePoint(), UnityEngine.Quaternion.identity);
			if (GetRemaningTime() <= m_interval)
			{
				m_character.m_faction = originalFaction;
				m_character.SetTamed(tamed: false);
				StatusEffect statusEffect = (SE_CharmImmunity)ScriptableObject.CreateInstance(typeof(SE_CharmImmunity));
				statusEffect.m_ttl = Mathf.Clamp(m_character.GetHealthPercentage() * VL_GlobalConfigs.g_CooldownModifer * 60f, 5f, 300f);
				m_character.GetSEMan().AddStatusEffect(statusEffect);
				Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), m_character.GetEyePoint(), UnityEngine.Quaternion.identity);
			}
		}
		base.UpdateStatusEffect(dt);
	}

	public override bool CanAdd(Character character)
	{
		return !character.IsPlayer();
	}
}
