using UnityEngine;
using System.Linq;

namespace ValheimLegends;

public class SE_Charmcontrol : StatusEffect
{
	public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("StaffSkeleton").GetComponent<ItemDrop>().m_itemData.GetIcon();

	public static GameObject GO_SEFX;

	[Header("SE_VL_Charmcontrol")]
	public static float m_baseTTL = 30f;

	public float damageModifier = 1f;

	private float m_timer = 0f;

	private float m_interval = 1f;

	public Character.Faction originalFaction;

	public SE_Charmcontrol()
	{
		base.name = "SE_VL_Charmcontrol";
		m_icon = ZNetScene.instance.GetPrefab("StaffSkeleton").GetComponent<ItemDrop>().m_itemData.GetIcon();
		m_tooltip = "Charm Limit";
		m_name = "Charm Control";
		m_ttl = m_baseTTL;
	}

	public override void UpdateStatusEffect(float dt)
	{
		m_timer -= dt;
		if (m_timer <= 0f)
		{
			m_timer = m_interval;
			if (GetRemaningTime() <= m_interval)
			{
				foreach (Character allCharacter in Character.GetAllCharacters())
				{
					if (!(allCharacter != null) || allCharacter.GetSEMan() == null)
					{
						continue;
					}
					if (allCharacter.GetSEMan().HaveStatusEffect("SE_VL_Charm".GetStableHashCode()))
					{
						SE_Charm sE_Charm = (SE_Charm)allCharacter.GetSEMan().GetStatusEffect("SE_VL_Charm".GetStableHashCode());
						allCharacter.m_faction = sE_Charm.originalFaction;
						allCharacter.SetTamed(tamed: false);
						StatusEffect statusEffect = (SE_CharmImmunity)ScriptableObject.CreateInstance(typeof(SE_CharmImmunity));
						statusEffect.m_ttl = Mathf.Clamp(allCharacter.GetHealthPercentage() * VL_GlobalConfigs.g_CooldownModifer * 60f, 5f, 300f);
						allCharacter.GetSEMan().AddStatusEffect(statusEffect);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), allCharacter.GetEyePoint(), UnityEngine.Quaternion.identity);
					}
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
