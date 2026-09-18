using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Valkyrie
{
	public enum ValkyrieAttackType
	{
		ShieldRelease = 12,
		HarpoonPull = 20
	}

	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

	private static int ScriptChar_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "piece", "terrain", "vehicle", "viewblock", "character", "character_noenv", "character_trigger", "character_net", "character_ghost", "Water");

	private static GameObject GO_CastFX;

	public static bool inFlight = false;

	public static bool isBlocking = false;

	public static ValkyrieAttackType QueuedAttack;

	public static bool PlayerUsingShield
	{
		get
		{
			Player localPlayer = Player.m_localPlayer;
			ItemDrop.ItemData value = Traverse.Create(localPlayer).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
			if (value != null)
			{
				ItemDrop.ItemData.SharedData shared = value.m_shared;
				if (shared != null && shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
				{
					return true;
				}
			}
			return false;
		}
	}

	public static void Execute_Attack(Player player, ref Rigidbody playerBody, ref float altitude)
	{
		SE_Valkyrie sE_Valkyrie = (SE_Valkyrie)player.GetSEMan().GetStatusEffect("SE_VL_Valkyrie".GetStableHashCode());
		if (QueuedAttack == ValkyrieAttackType.ShieldRelease)
		{
			UnityEngine.Vector3 vector = player.GetEyePoint() + player.GetLookDir() * 0.2f + player.transform.up * -0.4f + player.transform.right * -0.4f;
			for (int i = 0; i < sE_Valkyrie.hitCount; i++)
			{
				UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ShieldRelease"), vector, UnityEngine.Quaternion.LookRotation(player.transform.forward));
			}
			float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
			List<Character> list = new List<Character>();
			list.Clear();
			List<Character> list2 = new List<Character>();
			list2.Clear();
			Character.GetCharactersInRange(vector + player.transform.forward * 2f, 2.5f, list);
			Character.GetCharactersInRange(vector + player.transform.forward * 6f, 3f, list2);
			list.AddRange(list2);
			RaycastHit hitInfo = default(RaycastHit);
			UnityEngine.Vector3 position = player.transform.position;
			UnityEngine.Vector3 vector2 = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
			Physics.SphereCast(player.GetEyePoint(), 0.1f, player.GetLookDir(), out hitInfo, 4f, ScriptChar_Layermask);
			if (hitInfo.collider != null && hitInfo.collider.gameObject != null)
			{
				Character component = null;
				hitInfo.collider.gameObject.TryGetComponent<Character>(out component);
				bool flag = component != null;
				if (component == null)
				{
					component = (Character)hitInfo.collider.GetComponentInParent(typeof(Character));
					flag = component != null;
					if (component == null)
					{
						component = hitInfo.collider.GetComponentInChildren<Character>();
						flag = component != null;
					}
				}
				if (flag && BaseAI.IsEnemy(component, player) && !list.Contains(component))
				{
					list.Add(component);
				}
			}
			foreach (Character item in list)
			{
				if (BaseAI.IsEnemy(player, item))
				{
					UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
					HitData hitData = new HitData();
					hitData.m_damage.m_frost = UnityEngine.Random.Range(Mathf.Max((1f + 0.2f * level) * VL_GlobalConfigs.g_DamageModifer, 0.5f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f)), Mathf.Max((4f + 0.4f * level) * VL_GlobalConfigs.g_DamageModifer, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f))) * (float)sE_Valkyrie.hitCount * VL_GlobalConfigs.c_valkyrieBonusChillWave;
					hitData.m_damage.m_spirit = UnityEngine.Random.Range(Mathf.Max((1f + 0.2f * level) * VL_GlobalConfigs.g_DamageModifer, 0.5f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f)), Mathf.Max((4f + 0.4f * level) * VL_GlobalConfigs.g_DamageModifer, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f))) * (float)sE_Valkyrie.hitCount * VL_GlobalConfigs.c_valkyrieBonusChillWave;
					//hitData.m_damage.m_spirit = UnityEngine.Random.Range((float)sE_Valkyrie.hitCount * (1f + 0.02f * level), (float)sE_Valkyrie.hitCount * (2f + 0.015f * level)) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_valkyrieBonusChillWave;
					//hitData.m_damage.m_frost = UnityEngine.Random.Range((float)sE_Valkyrie.hitCount * (1f + 0.02f * level), (float)sE_Valkyrie.hitCount * (2f + 0.015f * level)) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_valkyrieBonusChillWave;
					hitData.m_point = item.GetEyePoint();
					hitData.m_dir = dir;
					hitData.m_skill = ValheimLegends.AbjurationSkill;
					item.Damage(hitData);
				}
			}
			sE_Valkyrie.hitCount = 0;
		}
		else if (QueuedAttack == ValkyrieAttackType.HarpoonPull)
		{
			float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			UnityEngine.Vector3 vector3 = player.GetEyePoint() + player.GetLookDir() * 0.2f + player.transform.up * 0.1f + player.transform.right * 0.28f;
			GameObject prefab = ZNetScene.instance.GetPrefab("VL_ValkyrieSpear");
			GameObject gameObject = UnityEngine.Object.Instantiate(prefab, vector3, UnityEngine.Quaternion.identity);
			Projectile component2 = gameObject.GetComponent<Projectile>();
			component2.name = "VL_ValkyrieSpear";
			component2.m_respawnItemOnHit = false;
			component2.m_spawnOnHit = null;
			component2.m_ttl = 6f;
			component2.m_gravity = 2f;
			component2.m_aoe = 1f;
			component2.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector3));
			gameObject.transform.localScale = UnityEngine.Vector3.one * 0.8f;
			RaycastHit hitInfo2 = default(RaycastHit);
			UnityEngine.Vector3 position2 = player.transform.position;
			UnityEngine.Vector3 target = ((!Physics.Raycast(vector3, player.GetLookDir(), out hitInfo2, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo2.collider) ? (position2 + player.GetLookDir() * 1000f) : hitInfo2.point);
			HitData hitData2 = new HitData();
			hitData2.m_skill = ValheimLegends.DisciplineSkill;
			hitData2.m_dir = player.GetLookDir() * -1f;
			hitData2.m_damage.m_frost = UnityEngine.Random.Range(Mathf.Max((12f + 0.5f * level2) * VL_GlobalConfigs.g_DamageModifer, 0.5f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level2 / 300f)), Mathf.Max((24f + level2) * VL_GlobalConfigs.g_DamageModifer, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level2 / 300f)))  * VL_GlobalConfigs.c_valkyrieBonusChillWave;
			hitData2.m_damage.m_spirit = UnityEngine.Random.Range(Mathf.Max((12f + 0.5f * level2) * VL_GlobalConfigs.g_DamageModifer, 0.5f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level2 / 300f)), Mathf.Max((24f + level2) * VL_GlobalConfigs.g_DamageModifer, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level2 / 300f))) * VL_GlobalConfigs.c_valkyrieBonusChillWave;
			//hitData2.m_damage.m_frost = UnityEngine.Random.Range(1f + 0.2f * level2, 2f + 0.3f * level2) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_valkyrieBonusIceLance;
			//hitData2.m_damage.m_spirit = UnityEngine.Random.Range(1f + 0.2f * level2, 2f + 0.3f * level2) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_valkyrieBonusIceLance;
			hitData2.SetAttacker(player);
			UnityEngine.Vector3 vector4 = UnityEngine.Vector3.MoveTowards(gameObject.transform.position, target, 1f);
			component2.Setup(player, (vector4 - gameObject.transform.position) * 40f, -1f, hitData2, null, null);
			Traverse.Create(component2).Field("m_skill").SetValue(ValheimLegends.DisciplineSkill);
			gameObject = null;
		}
	}

	public static void Impact_Effect(Player player, float altitude)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		inFlight = false;
		ValheimLegends.shouldValkyrieImpact = false;
		foreach (Character item in allCharacters)
		{
			float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 6f + 0.03f * level && VL_Utility.LOS_IsValid(item, player.transform.position, player.GetCenterPoint()))
			{
				UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
				HitData hitData = new HitData();
				hitData.m_damage.m_blunt = 5f + 3f * altitude + UnityEngine.Random.Range(Mathf.Max(1.5f * level, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f)), Mathf.Max(2.5f * level, 2f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f))) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_valkyrieLeap;
				hitData.m_pushForce = 20f * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_valkyrieLeap;
				hitData.m_point = item.GetEyePoint();
				hitData.m_dir = dir;
				hitData.m_skill = ValheimLegends.DisciplineSkill;
				item.Damage(hitData);
			}
		}
		((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).StopAllCoroutines();
		((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("battleaxe_attack2");
		GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_gdking_stomp"), player.transform.position, UnityEngine.Quaternion.identity);
	}

	public static void Process_Input(Player player)
	{
		System.Random random = new System.Random();
		UnityEngine.Vector3 vector = default(Vector3);
		if (player.IsBlocking() && ZInput.GetButtonDown("Attack"))
		{
			SE_Valkyrie sE_Valkyrie = (SE_Valkyrie)player.GetSEMan().GetStatusEffect("SE_VL_Valkyrie".GetStableHashCode());
			if ((float)sE_Valkyrie.hitCount >= VL_Utility.GetHarpoonPullCost)
			{
				sE_Valkyrie.hitCount -= (int)VL_Utility.GetHarpoonPullCost;
				sE_Valkyrie.refreshed = true;
				VL_Utility.RotatePlayerToTarget(player);
				((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).StopAllCoroutines();
				((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("spear_throw");
				ValheimLegends.isChargingDash = true;
				ValheimLegends.dashCounter = 0;
				QueuedAttack = ValkyrieAttackType.HarpoonPull;
				player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetHarpoonPullSkillGain);
			}
		}
		if (VL_Utility.Ability3_Input_Down)
		{
			SE_Valkyrie sE_Valkyrie2 = (SE_Valkyrie)player.GetSEMan().GetStatusEffect("SE_VL_Valkyrie".GetStableHashCode());
			if (player.IsBlocking())
			{
				if (PlayerUsingShield && sE_Valkyrie2 != null && sE_Valkyrie2.hitCount > 0)
				{
					VL_Utility.RotatePlayerToTarget(player);
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).StopAllCoroutines();
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("unarmed_attack1");
					ValheimLegends.isChargingDash = true;
					ValheimLegends.dashCounter = 0;
					QueuedAttack = ValkyrieAttackType.ShieldRelease;
					player.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetShieldReleaseSkillGain * (float)sE_Valkyrie2.hitCount);
				}
			}
			else if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetLeapCost)
				{
					StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
					statusEffect.m_ttl = VL_Utility.GetLeapCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect);
					player.UseStamina(VL_Utility.GetLeapCost);
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("knife_secondary");
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetSpeed(0.3f);
					UnityEngine.Vector3 velocity = player.GetVelocity();
					Rigidbody value = Traverse.Create(player).Field("m_body").GetValue<Rigidbody>();
					inFlight = true;
					UnityEngine.Vector3 zero = UnityEngine.Vector3.zero;
					zero.z = value.linearVelocity.z;
					zero.x = value.linearVelocity.x;
					value.linearVelocity = velocity * 2f + new UnityEngine.Vector3(0f, 15f, 0f) + zero * 3f;
					value.linearVelocity *= 0.8f + player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
						.m_level * 0.005f * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
					GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_perfectblock"), player.transform.position, UnityEngine.Quaternion.identity);
					GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_perfectblock"), player.transform.position, UnityEngine.Quaternion.identity);
					player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetLeapSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Leap: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetLeapCost + ")");
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
				if (player.GetStamina() >= VL_Utility.GetStaggerCost)
				{
					StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect2.m_ttl = VL_Utility.GetStaggerCooldownTime * VL_GlobalConfigs.c_valkyrieStaggerCooldown;
					player.GetSEMan().AddStatusEffect(statusEffect2);
					player.UseStamina(VL_Utility.GetStaggerCost);
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("battleaxe_attack1");
					GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_troll_rock_destroyed"), player.transform.position, UnityEngine.Quaternion.identity);
					GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_sledge_iron_hit"), player.transform.position, UnityEngine.Quaternion.identity);
					List<Character> allCharacters = Character.GetAllCharacters();
					foreach (Character item in allCharacters)
					{
						if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 6f && VL_Utility.LOS_IsValid(item, player.transform.position, player.GetCenterPoint()))
						{
							UnityEngine.Vector3 forceDirection = item.transform.position - player.transform.position;
							item.Stagger(forceDirection);
						}
					}
					player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetStaggerSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to Stagger: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetStaggerCost + ")");
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
				if (player.GetStamina() >= VL_Utility.GetBulwarkCost)
				{
					StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect3.m_ttl = VL_Utility.GetBulwarkCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					player.UseStamina(VL_Utility.GetBulwarkCost);
					ValheimLegends.shouldUseGuardianPower = false;
					player.StartEmote("challenge");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_guardstone_deactivate"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_metal_blocked"), player.transform.position, UnityEngine.Quaternion.identity);
					SE_Bulwark sE_Bulwark = (SE_Bulwark)ScriptableObject.CreateInstance(typeof(SE_Bulwark));
					sE_Bulwark.m_ttl = SE_Bulwark.m_baseTTL + (float)Mathf.RoundToInt(player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
						.m_level * 0.2f * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f)));
					player.GetSEMan().AddStatusEffect(sE_Bulwark);
					player.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetBulwarkSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Bulwark: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetBulwarkCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
	}
}
