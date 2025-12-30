using System;
using System.Linq;
using UnityEngine;

namespace ValheimLegends;

public class Class_Necromancer
{
	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

	private static int ScriptChar_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character");

	private static GameObject GO_CastFX;

	private static GameObject GO_Fireball;

	private static Projectile P_Fireball;

	private static StatusEffect SE_Fireball;

	private static GameObject GO_Meteor;

	private static Projectile P_Meteor;

	private static StatusEffect SE_Meteor;

	private static bool meteorCharging = false;

	private static int meteorCount;

	private static int meteorChargeAmount;

	private static int meteorChargeAmountMax;

	private static float meteorSkillGain = 0f;

	public static void Process_Input(Player player)
	{
		System.Random random = new System.Random();
		if (VL_Utility.Ability3_Input_Down && !meteorCharging)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetMeteorCost)
				{
					StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
					statusEffect.m_ttl = VL_Utility.GetMeteorCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect);
					player.UseStamina(VL_Utility.GetMeteorCost);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to begin Meteor : (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetMeteorCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else
		{
			if ((VL_Utility.Ability3_Input_Pressed && meteorCharging && player.GetStamina() > 1f) || VL_Utility.Ability3_Input_Up || (player.GetStamina() <= 1f && meteorCharging))
			{
				return;
			}
			if (VL_Utility.Ability2_Input_Down)
			{
				if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
				{
					if (player.GetStamina() >= VL_Utility.GetFrostNovaCost)
					{
						StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
						statusEffect2.m_ttl = VL_Utility.GetFrostNovaCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect2);
						player.UseStamina(VL_Utility.GetFrostNovaCost);
						float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
						player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFrostNovaSkillGain);
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Frost Nova: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetDefenderCost + ")");
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
				}
			}
			else if (VL_Utility.Ability1_Input_Down)
			{
				if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
				{
					if (player.GetStamina() >= VL_Utility.GetFireballCost)
					{
						float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
						StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
						statusEffect3.m_ttl = VL_Utility.GetFireballCooldownTime - 0.02f * level2;
						player.GetSEMan().AddStatusEffect(statusEffect3);
						player.UseStamina(VL_Utility.GetFireballCost + 0.5f * level2);
						player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain);
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to for Fireball: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetFireballCost + ")");
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
				}
			}
			else
			{
				ValheimLegends.isChanneling = false;
			}
		}
	}
}
