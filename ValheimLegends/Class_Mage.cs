using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Mage
{
	public enum MageAttackType
	{
		IceDagger = 20,
		FlameNova = 60
	}

	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

	private static int ScriptChar_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost");

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

	public static MageAttackType QueuedAttack;

	public static void Execute_Attack(Player player)
	{
		if (QueuedAttack == MageAttackType.FlameNova)
		{
			float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_FlameBurst"), player.transform.position, UnityEngine.Quaternion.identity);
			List<Character> allCharacters = Character.GetAllCharacters();
			foreach (Character item in allCharacters)
			{
				if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 8f + 0.1f * level && VL_Utility.LOS_IsValid(item, player.GetCenterPoint(), player.transform.position + player.transform.up * 0.2f))
				{
					UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
					HitData hitData = new HitData();
					hitData.m_damage.m_fire = UnityEngine.Random.Range(5f + 2.75f * level, 10f + 3.5f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageInferno;
					hitData.m_pushForce = 0f;
					hitData.m_point = item.GetEyePoint();
					hitData.m_dir = dir;
					hitData.m_skill = ValheimLegends.EvocationSkill;
					item.Damage(hitData);
				}
			}
			GameCamera.instance.AddShake(player.transform.position, 15f, 2f, continous: false);
			player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFrostNovaSkillGain * 1.5f);
		}
		else if (QueuedAttack == MageAttackType.IceDagger)
		{
			float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			UnityEngine.Vector3 vector = player.GetEyePoint() + player.GetLookDir() * 0.2f + player.transform.up * 0.1f + player.transform.right * 0.28f;
			GameObject prefab = ZNetScene.instance.GetPrefab("VL_FrostDagger");
			GameObject gameObject = UnityEngine.Object.Instantiate(prefab, vector, UnityEngine.Quaternion.identity);
			Projectile component = gameObject.GetComponent<Projectile>();
			component.name = "IceDagger";
			component.m_respawnItemOnHit = false;
			component.m_spawnOnHit = null;
			component.m_ttl = 0.6f;
			component.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector));
			gameObject.transform.localScale = UnityEngine.Vector3.one * 0.8f;
			RaycastHit hitInfo = default(RaycastHit);
			UnityEngine.Vector3 position = player.transform.position;
			UnityEngine.Vector3 target = ((!Physics.Raycast(vector, player.GetLookDir(), out hitInfo, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
			HitData hitData2 = new HitData();
			hitData2.m_damage.m_pierce = UnityEngine.Random.Range(2f + 0.25f * level2, 5f + 0.75f * level2) * VL_GlobalConfigs.c_mageFrostDagger * VL_GlobalConfigs.g_DamageModifer;
			hitData2.m_damage.m_frost = UnityEngine.Random.Range(0.5f * level2, 2f + 1f * level2) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFrostDagger;
			hitData2.m_skill = ValheimLegends.EvocationSkill;
			hitData2.SetAttacker(player);
			UnityEngine.Vector3 vector2 = UnityEngine.Vector3.MoveTowards(gameObject.transform.position, target, 1f);
			component.Setup(player, (vector2 - gameObject.transform.position) * 55f, -1f, hitData2, null, null);
			Traverse.Create(component).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
			gameObject = null;
		}
	}

	public static void Process_Input(Player player, float altitude)
	{
		System.Random random = new System.Random();
		if (VL_Utility.Ability3_Input_Down && !meteorCharging)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				if (player.IsBlocking())
                {
					float maxEitrRecover = player.GetMaxEitr() - player.GetEitr();
					if (maxEitrRecover > 0f) {
						float maxStaminaCost = maxEitrRecover * 2f * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
						float recoveredEitr = maxEitrRecover;
						if (player.GetStamina() < maxStaminaCost)
                        {
							recoveredEitr = player.GetStamina() / (2f * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f)));
						}
						StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
						statusEffect.m_ttl = recoveredEitr * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f)) * 0.5f;
						player.GetSEMan().AddStatusEffect(statusEffect);
						player.UseStamina(recoveredEitr * 2f * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f)));
						player.AddEitr(recoveredEitr);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ChiPulse"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_Potion_frostresist"), player.transform.position, UnityEngine.Quaternion.identity);
					}
					else
                    {
						player.Message(MessageHud.MessageType.TopLeft, "You don't need to Meditate.");
					}
				}
				else 
				{ 
					if (player.GetStamina() >= VL_Utility.GetMeteorCost)
					{
						ValheimLegends.shouldUseGuardianPower = false;
						ValheimLegends.isChanneling = true;
						meteorSkillGain = 0f;
						StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
						statusEffect.m_ttl = VL_Utility.GetMeteorCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect);
						player.UseStamina(VL_Utility.GetMeteorCost);
						float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Flames"), player.transform.position, UnityEngine.Quaternion.identity);
						meteorCharging = true;
						meteorChargeAmount = 0;
						meteorChargeAmountMax = Mathf.RoundToInt(60f * (1f - level / 200f));
						meteorCount = 1;
						meteorSkillGain += VL_Utility.GetMeteorSkillGain;
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to begin Meteor : (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetMeteorCost + ")");
					}
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability3_Input_Pressed && meteorCharging && player.GetStamina() > 1f && Mathf.Max(0f, altitude - player.transform.position.y) <= 1f)
		{
			VL_Utility.SetTimer();
			meteorChargeAmount++;
			player.UseStamina(VL_Utility.GetMeteorCostPerUpdate);
			ValheimLegends.isChanneling = true;
			if (meteorChargeAmount >= meteorChargeAmountMax)
			{
				meteorCount++;
				meteorChargeAmount = 0;
				((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
				GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Flames"), player.transform.position, UnityEngine.Quaternion.identity);
				meteorSkillGain += 0.2f;
			}
		}
		else if ((VL_Utility.Ability3_Input_Up || player.GetStamina() <= 1f || Mathf.Max(0f, altitude - player.transform.position.y) > 1f) && meteorCharging)
		{
			UnityEngine.Vector3 vector = player.transform.position + player.transform.up * 2f + player.GetLookDir() * 1f;
			GameObject prefab = ZNetScene.instance.GetPrefab("projectile_meteor");
			meteorCharging = false;
			float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			for (int i = 0; i < meteorCount; i++)
			{
				GO_Meteor = UnityEngine.Object.Instantiate(prefab, new UnityEngine.Vector3(vector.x + (float)random.Next(-100, 100), vector.y + 250f, vector.z + (float)random.Next(-100, 100)), UnityEngine.Quaternion.identity);
				P_Meteor = GO_Meteor.GetComponent<Projectile>();
				P_Meteor.name = "Meteor" + i;
				P_Meteor.m_respawnItemOnHit = false;
				P_Meteor.m_spawnOnHit = null;
				P_Meteor.m_ttl = 6f;
				P_Meteor.m_gravity = 0f;
				P_Meteor.m_rayRadius = 0.1f;
				P_Meteor.m_aoe = 8f + 0.04f * level2;
				P_Meteor.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector));
				GO_Meteor.transform.localScale = UnityEngine.Vector3.zero;
				RaycastHit hitInfo = default(RaycastHit);
				UnityEngine.Vector3 position = player.transform.position;
				UnityEngine.Vector3 target = ((!Physics.Raycast(vector, player.GetLookDir(), out hitInfo, 1000f, ScriptChar_Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
				target.x += random.Next(-8, 8);
				target.y += random.Next(-8, 8);
				target.z += random.Next(-8, 8);
				HitData hitData = new HitData();
				hitData.m_damage.m_fire = UnityEngine.Random.Range(30f + 0.5f * level2, 50f + level2) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageMeteor;
				hitData.m_damage.m_blunt = UnityEngine.Random.Range(15f + 0.25f * level2, 30f + 0.5f * level2) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageMeteor;
				hitData.m_pushForce = 10f;
				hitData.SetAttacker(player);
				hitData.m_skill = ValheimLegends.EvocationSkill;
				UnityEngine.Vector3 vector2 = UnityEngine.Vector3.MoveTowards(GO_Meteor.transform.position, target, 1f);
				P_Meteor.Setup(player, (vector2 - GO_Meteor.transform.position) * UnityEngine.Random.Range(78f, 86f), -1f, hitData, null, null);
				Traverse.Create(P_Meteor).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
				GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_guardstone_permitted_removed"), player.transform.position + player.transform.right * UnityEngine.Random.Range(-1f, 1f) + player.transform.up * UnityEngine.Random.Range(0f, 1.5f), UnityEngine.Quaternion.identity);
			}
			meteorCount = 0;
			meteorChargeAmount = 0;
			GO_Meteor = null;
			player.RaiseSkill(ValheimLegends.EvocationSkill, meteorSkillGain);
			meteorSkillGain = 0f;
			ValheimLegends.isChanneling = false;
            ValheimLegends.channelingBlocksMovement = true;
        }
		else if (VL_Utility.Ability2_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.IsBlocking())
				{
					if (player.GetStamina() >= VL_Utility.GetFrostNovaCost * 2f)
					{
						StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
						statusEffect2.m_ttl = VL_Utility.GetFrostNovaCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect2);
						player.UseStamina(VL_Utility.GetFrostNovaCost * 2f);
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_sledge");
						ValheimLegends.isChargingDash = true;
						ValheimLegends.dashCounter = 0;
						QueuedAttack = MageAttackType.FlameNova;
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Inferno: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetFrostNovaCost * 2f + ")");
					}
				}
				else if (player.GetStamina() >= VL_Utility.GetFrostNovaCost)
				{
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = VL_Utility.GetFrostNovaCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					player.UseStamina(VL_Utility.GetFrostNovaCost);
					float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_axe1");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_guardstone_activate"), player.transform.position, UnityEngine.Quaternion.identity);
					if (player.GetSEMan().HaveStatusEffect("Burning".GetStableHashCode()))
					{
						player.GetSEMan().RemoveStatusEffect("Burning".GetStableHashCode());
					}
					List<Character> allCharacters = Character.GetAllCharacters();
					foreach (Character item in allCharacters)
					{
						if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 10f + 0.1f * level3 && VL_Utility.LOS_IsValid(item, player.GetCenterPoint(), player.transform.position + player.transform.up * 0.15f))
						{
							UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
							HitData hitData2 = new HitData();
							hitData2.m_damage.m_frost = UnityEngine.Random.Range(10f + 0.5f * level3, 20f + level3) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFrostNova;
							hitData2.m_pushForce = 20f;
							hitData2.m_point = item.GetEyePoint();
							hitData2.m_dir = dir;
							hitData2.m_skill = ValheimLegends.EvocationSkill;
							item.Damage(hitData2);
							SE_Slow sE_Slow = (SE_Slow)ScriptableObject.CreateInstance(typeof(SE_Slow));
							item.GetSEMan().AddStatusEffect(sE_Slow.name.GetStableHashCode(), resetTime: true);
						}
					}
					player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFrostNovaSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Frost Nova: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetFrostNovaCost + ")");
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
				float level4 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
					.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
				if (player.IsBlocking())
				{
					if (player.GetStamina() >= VL_Utility.GetFireballCost * 0.5f)
					{
						StatusEffect statusEffect4 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
						statusEffect4.m_ttl = 0.5f;
						player.GetSEMan().AddStatusEffect(statusEffect4);
						player.UseStamina(VL_Utility.GetFireballCost * 0.5f);
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_axe2");
						ValheimLegends.isChargingDash = true;
						ValheimLegends.dashCounter = 0;
						QueuedAttack = MageAttackType.IceDagger;
						player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.5f);
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Ice Dagger: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetFireballCost * 0.5f + ")");
					}
				}
				else if (player.GetStamina() >= VL_Utility.GetFireballCost + 0.5f * level4)
				{
					ValheimLegends.shouldUseGuardianPower = false;
					StatusEffect statusEffect5 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect5.m_ttl = VL_Utility.GetFireballCooldownTime - 0.02f * level4;
					player.GetSEMan().AddStatusEffect(statusEffect5);
					player.UseStamina(VL_Utility.GetFireballCost + 0.5f * level4);
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetSpeed(3f);
					GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Flames"), player.transform.position, UnityEngine.Quaternion.identity);
					UnityEngine.Vector3 vector3 = player.transform.position + player.transform.up * 2.5f + player.GetLookDir() * 0.5f;
					GameObject prefab2 = ZNetScene.instance.GetPrefab("Imp_fireball_projectile");
					GO_Fireball = UnityEngine.Object.Instantiate(prefab2, new UnityEngine.Vector3(vector3.x, vector3.y, vector3.z), UnityEngine.Quaternion.identity);
					P_Fireball = GO_Fireball.GetComponent<Projectile>();
					P_Fireball.name = "Fireball";
					P_Fireball.m_respawnItemOnHit = false;
					P_Fireball.m_spawnOnHit = null;
					P_Fireball.m_ttl = 60f;
					P_Fireball.m_gravity = 2.5f;
					P_Fireball.m_rayRadius = 0.1f;
					P_Fireball.m_aoe = 3f + 0.03f * level4;
					P_Fireball.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector3));
					GO_Fireball.transform.localScale = UnityEngine.Vector3.zero;
					RaycastHit hitInfo2 = default(RaycastHit);
					UnityEngine.Vector3 position2 = player.transform.position;
					UnityEngine.Vector3 target2 = ((!Physics.Raycast(vector3, player.GetLookDir(), out hitInfo2, float.PositiveInfinity, Script_Layermask) || !hitInfo2.collider) ? (position2 + player.GetLookDir() * 1000f) : hitInfo2.point);
					HitData hitData3 = new HitData();
					hitData3.m_damage.m_fire = UnityEngine.Random.Range(5f + 1.6f * level4, 10f + 1.8f * level4) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFireball;
					hitData3.m_damage.m_blunt = UnityEngine.Random.Range(5f + 0.9f * level4, 10f + 1.1f * level4) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFireball;
					hitData3.m_pushForce = 2f;
					hitData3.m_skill = ValheimLegends.EvocationSkill;
					hitData3.SetAttacker(player);
					UnityEngine.Vector3 vector4 = UnityEngine.Vector3.MoveTowards(GO_Fireball.transform.position, target2, 1f);
					P_Fireball.Setup(player, (vector4 - GO_Fireball.transform.position) * 25f, -1f, hitData3, null, null);
					Traverse.Create(P_Fireball).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
					GO_Fireball = null;
					player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to for Fireball: (" + player.GetStamina().ToString("#.#") + "/" + (VL_Utility.GetFireballCost + 0.5f * level4) + ")");
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
            ValheimLegends.channelingBlocksMovement = true;
        }
	}
}
