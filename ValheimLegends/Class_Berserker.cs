using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ValheimLegends;

public class Class_Berserker
{
	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "vehicle", "viewblock", "piece");

	private static int Player_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle");

	private static GameObject GO_CastFX;

	public static void Execute_Dash(Player player, ref float altitude, ref Rigidbody playerBody)
	{
		UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_perfectblock"), player.transform.position, UnityEngine.Quaternion.identity);
		UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_stonegolem_attack_hit"), player.transform.position, UnityEngine.Quaternion.identity);
		float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
			.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
		float num = 0.6f + level * 0.015f * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_berserkerDash;
		if (player.GetSEMan().HaveStatusEffect("SE_VL_Berserk".GetStableHashCode()) || player.GetSEMan().HaveStatusEffect("SE_VL_Execute".GetStableHashCode()))
		{
			SE_Berserk sE_Berserk = (SE_Berserk)player.GetSEMan().GetStatusEffect("SE_VL_Berserk".GetStableHashCode());
			if (sE_Berserk != null)
			{
				num *= sE_Berserk.damageModifier;
			}
		}
		UnityEngine.Vector3 lookDir = player.GetLookDir();
		lookDir.y = 0f;
		player.transform.rotation = UnityEngine.Quaternion.LookRotation(lookDir);
		UnityEngine.Vector3 vector = default(Vector3);
		UnityEngine.Vector3 forward = player.transform.forward;
		UnityEngine.Vector3 position = player.transform.position;
		UnityEngine.Vector3 vector2 = player.transform.position;
		vector2.y += 0.1f;
		List<int> list = new List<int>();
		float num2 = 1f;
		int i;
		for (i = 0; i <= 10; i++)
		{
			RaycastHit hitInfo = default(RaycastHit);
			bool flag = false;
			for (int j = 0; j <= 10; j++)
			{
				UnityEngine.Vector3 vector3 = UnityEngine.Vector3.MoveTowards(player.transform.position, player.transform.position + forward * 100f, (float)i + (float)j * 0.1f);
				vector3.y = vector2.y;
				if (vector3.y < ZoneSystem.instance.GetGroundHeight(vector3))
				{
					vector2.y = ZoneSystem.instance.GetGroundHeight(vector3) + 1f;
					vector3.y = vector2.y;
				}
				flag = Physics.SphereCast(vector3, 0.05f, forward, out hitInfo, float.PositiveInfinity, Script_Layermask);
				if (flag && (bool)hitInfo.collider)
				{
					vector = hitInfo.point;
					break;
				}
			}
			position = UnityEngine.Vector3.MoveTowards(player.transform.position, player.transform.position + forward * 100f, i);
			position.y = ((ZoneSystem.instance.GetSolidHeight(position) - ZoneSystem.instance.GetGroundHeight(position) <= 1f) ? ZoneSystem.instance.GetSolidHeight(position) : ZoneSystem.instance.GetGroundHeight(position));
			if (flag && UnityEngine.Vector3.Distance(new UnityEngine.Vector3(position.x, vector2.y, position.z), vector) <= 1f)
			{
				vector2 = UnityEngine.Vector3.MoveTowards(vector, vector2, 1f);
				UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_beehive_hit"), vector2, UnityEngine.Quaternion.identity);
				break;
			}
			UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_beehive_hit"), vector2, UnityEngine.Quaternion.identity);
			vector2 = new UnityEngine.Vector3(position.x, vector2.y, position.z);
			foreach (Character allCharacter in Character.GetAllCharacters())
			{
				HitData hitData = new HitData();
				hitData.m_damage = player.GetCurrentWeapon().GetDamage();
				hitData.ApplyModifier(UnityEngine.Random.Range(0.8f, 1.2f) * num / num2);
				hitData.m_point = allCharacter.GetCenterPoint();
				hitData.m_dir = allCharacter.transform.position - position;
				hitData.m_skill = ValheimLegends.DisciplineSkill;
				float num3 = UnityEngine.Vector3.Distance(allCharacter.transform.position, position);
				if (!BaseAI.IsEnemy(allCharacter, player) || !(num3 <= 3f) || list.Contains(allCharacter.GetInstanceID()))
				{
					continue;
				}
				SE_Execute sE_Execute = (SE_Execute)player.GetSEMan().GetStatusEffect("SE_VL_Execute".GetStableHashCode());
				if (sE_Execute != null)
				{
					hitData.ApplyModifier(sE_Execute.damageBonus);
					sE_Execute.hitCount--;
					if (sE_Execute.hitCount <= 0)
					{
						player.GetSEMan().RemoveStatusEffect(sE_Execute);
					}
				}
				num2 += 0.6f;
				allCharacter.Damage(hitData);
				UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_crit"), allCharacter.GetCenterPoint(), UnityEngine.Quaternion.identity);
				list.Add(allCharacter.GetInstanceID());
			}
		}
		list.Clear();
		if (i > 10 && ZoneSystem.instance.GetSolidHeight(vector2) - vector2.y <= 2f)
		{
			vector2.y = ZoneSystem.instance.GetSolidHeight(vector2);
		}
		playerBody.position = vector2;
		altitude = 0f;
		player.transform.rotation = UnityEngine.Quaternion.LookRotation(forward);
	}

	public static void Process_Input(Player player, ref float altitude)
	{
		System.Random random = new System.Random();
		UnityEngine.Vector3 vector = default(Vector3);
		if (VL_Utility.Ability3_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				if (player.GetStamina() > VL_Utility.GetDashCost(player))
				{
					StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
					statusEffect.m_ttl = VL_Utility.GetDashCooldown(player);
					player.GetSEMan().AddStatusEffect(statusEffect);
					player.UseStamina(VL_Utility.GetDashCost(player));
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_longsword2");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), player.transform.position, UnityEngine.Quaternion.identity);
					ValheimLegends.isChargingDash = true;
					ValheimLegends.dashCounter = 0;
					player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetDashSkillGain(player));
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Dash: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetDashCost(player) + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability2_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.GetStamina() > VL_Utility.GetBerserkCost(player))
				{
					StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect2.m_ttl = VL_Utility.GetBerserkCooldown(player);
					player.GetSEMan().AddStatusEffect(statusEffect2);
					player.UseStamina(VL_Utility.GetBerserkCost(player));
					ValheimLegends.shouldUseGuardianPower = false;
					player.StartEmote("challenge");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_GP_Stone"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					SE_Berserk sE_Berserk = (SE_Berserk)ScriptableObject.CreateInstance(typeof(SE_Berserk));
					sE_Berserk.m_ttl = SE_Berserk.m_baseTTL;
					sE_Berserk.speedModifier = 1.2f + 0.006f * level;
					sE_Berserk.damageModifier = 1.2f + 0.006f * level * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_berserkerBerserk;
					sE_Berserk.healthAbsorbPercent = 0.2f + 0.002f * level;
					player.GetSEMan().AddStatusEffect(sE_Berserk);
					player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetBerserkSkillGain(player));
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Berserk: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetBerserkCost(player) + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else
		{
			if (!VL_Utility.Ability1_Input_Down)
			{
				return;
			}
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
			{
				if (player.GetStamina() > VL_Utility.GetExecuteCost(player))
				{
					StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect3.m_ttl = VL_Utility.GetExecuteCooldown(player);
					player.GetSEMan().AddStatusEffect(statusEffect3);
					player.UseStamina(VL_Utility.GetExecuteCost(player));
					player.StartEmote("point");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_backstab"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
					SE_Execute sE_Execute = (SE_Execute)ScriptableObject.CreateInstance(typeof(SE_Execute));
					sE_Execute.hitCount = Mathf.RoundToInt(3f + 0.04f * level2);
					sE_Execute.damageBonus = 1.4f + 0.005f * level2 * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_berserkerExecute;
					sE_Execute.staggerForce = 1.5f + 0.005f * level2;
					if (player.GetSEMan().HaveStatusEffect("SE_VL_Execute".GetStableHashCode()))
					{
						StatusEffect statusEffect4 = player.GetSEMan().GetStatusEffect("SE_VL_Execute".GetStableHashCode());
						player.GetSEMan().RemoveStatusEffect(statusEffect4);
					}
					player.GetSEMan().AddStatusEffect(sE_Execute);
					player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetExecuteSkillGain(player));
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Execute: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetExecuteCost(player) + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
	}
}
