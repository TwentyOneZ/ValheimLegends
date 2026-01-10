using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Monk
{
	public enum MonkAttackType
	{
		MeteorPunch = 12,
		MeteorSlam = 13,
		FlyingKick = 1,
		FlyingKickStart = 8,
		Surge = 20,
		Psibolt = 15
	}

	private static int Script_Solidmask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "character", "character_noenv", "character_trigger", "character_net", "character_ghost");

	private static int ScriptChar_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "piece", "terrain", "vehicle", "viewblock", "character", "character_noenv", "character_trigger", "character_net", "character_ghost", "Water");

	public static MonkAttackType QueuedAttack;

	private static int fkickCount;

	private static int fkickCountMax;

	private static UnityEngine.Vector3 kickDir;

	private static List<int> kicklist;

	public static bool PlayerIsUnarmed
	{
		get
		{
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer.GetCurrentWeapon() != null)
			{
				ItemDrop.ItemData value = Traverse.Create(localPlayer).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
				ItemDrop.ItemData.SharedData shared = localPlayer.GetCurrentWeapon().m_shared;
				//Debug.Log("Name:" + shared.m_name.ToLower().ToString());
				if (shared != null && (shared.m_name.ToLower() == "unarmed" || shared.m_name.ToLower().Contains("fist") || shared.m_attachOverride == ItemDrop.ItemData.ItemType.Hands) && value == null)
				{
					return true;
				}
			}
			return false;
		}
	}

    public static bool PlayerIsBareHanded
    {
        get
        {
            Player localPlayer = Player.m_localPlayer;
            if (localPlayer.GetCurrentWeapon() != null)
            {
                ItemDrop.ItemData value = Traverse.Create(localPlayer).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
                ItemDrop.ItemData.SharedData shared = localPlayer.GetCurrentWeapon().m_shared;
                Debug.Log("Name:" + shared.m_name.ToLower().ToString());
                if (shared != null && (shared.m_name.ToLower() == "unarmed" || (shared.m_attachOverride == ItemDrop.ItemData.ItemType.Hands) && value == null))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public static void Impact_Effect(Player player, float altitude)
	{
		List<Character> allCharacters = Character.GetAllCharacters();
		ValheimLegends.shouldValkyrieImpact = false;
		foreach (Character item in allCharacters)
		{
			float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 6f + 0.03f * level && VL_Utility.LOS_IsValid(item, player.transform.position))
			{
				UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
				HitData hitData = new HitData();
				hitData.m_damage.m_blunt = 5f + 3f * altitude + Random.Range(Mathf.Min(level * VL_GlobalConfigs.g_DamageModifer, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f)), Mathf.Min((2f * level) * VL_GlobalConfigs.g_DamageModifer, 2f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f))) * VL_GlobalConfigs.c_monkChiSlam;
				hitData.m_pushForce = 20f * VL_GlobalConfigs.g_DamageModifer;
				hitData.m_point = item.GetEyePoint();
				hitData.m_dir = dir;
				hitData.m_skill = ValheimLegends.DisciplineSkill;
				item.Damage(hitData);
			}
		}
		Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_MeteorSlam"), player.transform.position + player.transform.up * 0.2f, UnityEngine.Quaternion.identity);
	}

	public static void Execute_Attack(Player player, ref Rigidbody playerBody, ref float altitude)
	{
		if (QueuedAttack == MonkAttackType.MeteorPunch)
		{
			UnityEngine.Vector3 vector = player.GetEyePoint() + player.GetLookDir() * 0.2f + player.transform.up * -0.4f + player.transform.right * -0.4f;
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Shockwave"), vector, UnityEngine.Quaternion.LookRotation(player.transform.forward));
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ReverseLightburst"), vector, UnityEngine.Quaternion.LookRotation(player.transform.forward));
			float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			RaycastHit hitInfo = default(RaycastHit);
			UnityEngine.Vector3 position = player.transform.position;
			UnityEngine.Vector3 vector2 = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
			Physics.SphereCast(player.GetEyePoint(), 0.1f, player.GetLookDir(), out hitInfo, 4f, ScriptChar_Layermask);
			if (!(hitInfo.collider != null) || !(hitInfo.collider.gameObject != null))
			{
				return;
			}
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
			List<Character> list = new List<Character>();
			list.Clear();
			Character.GetCharactersInRange(vector + player.transform.forward * 2f, 2.5f, list);
			if (flag && !component.IsPlayer())
			{
				list.Add(component);
			}
			{
				foreach (Character item in list)
				{
					if (BaseAI.IsEnemy(player, item))
					{
						UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
						HitData hitData = new HitData();
						hitData.m_damage.m_blunt = Random.Range(Mathf.Max((12f + 0.5f * level) * VL_GlobalConfigs.g_DamageModifer, 0.5f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f)), Mathf.Max((24f + level) * VL_GlobalConfigs.g_DamageModifer, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level / 300f))) * VL_GlobalConfigs.c_monkChiPunch;
						hitData.m_pushForce = 45f + 0.5f * level;
						hitData.m_point = item.GetEyePoint();
						hitData.m_dir = dir;
						hitData.m_skill = ValheimLegends.DisciplineSkill;
						item.Damage(hitData);
					}
				}
				return;
			}
		}
		if (QueuedAttack == MonkAttackType.MeteorSlam)
		{
			playerBody.linearVelocity += new UnityEngine.Vector3(0f, -8f, 0f);
			for (int i = 0; i < 4; i++)
			{
				Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ReverseLightburst"), player.transform.position + player.transform.up * Random.Range(0f, 0.5f) + player.transform.right * Random.Range(-0.3f, 0.3f), UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(0f, -1f, 0f)));
			}
			Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_perfectblock"), player.transform.position, UnityEngine.Quaternion.identity);
			Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_perfectblock"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
		}
		else if (QueuedAttack == MonkAttackType.Surge)
		{
			SE_Monk sE_Monk = (SE_Monk)player.GetSEMan().GetStatusEffect("SE_VL_Monk".GetStableHashCode());
			sE_Monk.surging = true;
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_Potion_frostresist"), player.transform.position, UnityEngine.Quaternion.identity);
			ValheimLegends.isChanneling = false;
            ValheimLegends.channelingBlocksMovement = true;
        }
		else if (QueuedAttack == MonkAttackType.Psibolt)
		{
			float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			SE_Monk sE_Monk2 = (SE_Monk)player.GetSEMan().GetStatusEffect("SE_VL_Monk".GetStableHashCode());
			UnityEngine.Vector3 vector3 = player.GetEyePoint() + player.GetLookDir() * 0.4f + player.transform.up * 0.1f + player.transform.right * 0.22f;
			GameObject prefab = ZNetScene.instance.GetPrefab("VL_PsiBolt");
			GameObject gameObject = Object.Instantiate(prefab, vector3, UnityEngine.Quaternion.identity);
			Projectile component2 = gameObject.GetComponent<Projectile>();
			component2.name = "PsiBolt";
			component2.m_respawnItemOnHit = false;
			component2.m_spawnOnHit = null;
			component2.m_ttl = 12f;
			component2.m_rayRadius = 0.01f;
			component2.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector3));
			gameObject.transform.localScale = UnityEngine.Vector3.one;
			RaycastHit hitInfo2 = default(RaycastHit);
			UnityEngine.Vector3 position2 = player.transform.position;
			UnityEngine.Vector3 target = ((!Physics.Raycast(vector3, player.GetLookDir(), out hitInfo2, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo2.collider) ? (position2 + player.GetLookDir() * 1000f) : hitInfo2.point);
			HitData hitData2 = new HitData();
			hitData2.m_damage.m_slash = Random.Range(Mathf.Max((1f + 0.2f * level2) * VL_GlobalConfigs.g_DamageModifer, 0.5f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level2 / 300f)), Mathf.Max((4f + 0.4f * level2) * VL_GlobalConfigs.g_DamageModifer, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level2 / 300f))) * (float)sE_Monk2.hitCount * VL_GlobalConfigs.c_monkChiBlast;
			hitData2.m_damage.m_spirit = Random.Range(Mathf.Max((0.2f * level2) * VL_GlobalConfigs.g_DamageModifer, 0.5f * player.GetCurrentWeapon().GetDamage().m_damage * (1f + level2 / 300f)), Mathf.Max((1f + 0.2f * level2) * VL_GlobalConfigs.g_DamageModifer, player.GetCurrentWeapon().GetDamage().m_damage * (1f + level2 / 300f))) * (float)sE_Monk2.hitCount * VL_GlobalConfigs.c_monkChiBlast;
			hitData2.m_skill = ValheimLegends.DisciplineSkill;
			hitData2.SetAttacker(player);
			UnityEngine.Vector3 vector4 = UnityEngine.Vector3.MoveTowards(gameObject.transform.position, target, 1f);
			component2.Setup(player, (vector4 - gameObject.transform.position) * 60f, -1f, hitData2, null, null);
			Traverse.Create(component2).Field("m_skill").SetValue(ValheimLegends.DisciplineSkill);
			sE_Monk2.hitCount = 0;
			sE_Monk2.refreshed = true;
			gameObject = null;
		}
		else
		{
			if (QueuedAttack != MonkAttackType.FlyingKick && QueuedAttack != MonkAttackType.FlyingKickStart)
			{
				return;
			}
			float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			float num = 0.5f + level3 * 0.005f * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_monkFlyingKick;
			SE_Monk sE_Monk3 = (SE_Monk)player.GetSEMan().GetStatusEffect("SE_VL_Monk".GetStableHashCode());
			UnityEngine.Vector3 lookDir = player.GetLookDir();
			lookDir.y = 0f;
			player.transform.rotation = UnityEngine.Quaternion.AngleAxis(40 * fkickCount, player.transform.up) * UnityEngine.Quaternion.LookRotation(kickDir);
			UnityEngine.Vector3 vector5 = default(Vector3);
			UnityEngine.Vector3 vector6 = kickDir;
			UnityEngine.Vector3 position3 = player.transform.position;
			UnityEngine.Vector3 vector7 = player.transform.position;
			UnityEngine.Vector3 vector8 = player.transform.position + player.transform.forward * 0.2f + player.transform.up * 0.7f;
			vector7.y += 0.1f;
			int j;
			for (j = 0; j <= 1; j++)
			{
				RaycastHit hitInfo3 = default(RaycastHit);
				bool flag2 = false;
				for (int k = 0; k <= 10; k++)
				{
					UnityEngine.Vector3 vector9 = UnityEngine.Vector3.MoveTowards(player.transform.position, player.transform.position + vector6 * 100f, (float)j + (float)k * 0.1f);
					vector9.y = vector7.y;
					if (vector9.y < ZoneSystem.instance.GetGroundHeight(vector9))
					{
						vector7.y = ZoneSystem.instance.GetGroundHeight(vector9) + 1f;
						vector9.y = vector7.y;
					}
					flag2 = Physics.SphereCast(vector9, 0.05f, vector6, out hitInfo3, float.PositiveInfinity, ScriptChar_Layermask);
					if (flag2 && (bool)hitInfo3.collider)
					{
						vector5 = hitInfo3.point;
						break;
					}
				}
				position3 = UnityEngine.Vector3.MoveTowards(player.transform.position, player.transform.position + vector6 * 100f, (float)j * 0.6f);
				position3.y = ((ZoneSystem.instance.GetSolidHeight(position3) - ZoneSystem.instance.GetGroundHeight(position3) <= 1f) ? ZoneSystem.instance.GetSolidHeight(position3) : ZoneSystem.instance.GetGroundHeight(position3));
				if (flag2 && UnityEngine.Vector3.Distance(new UnityEngine.Vector3(position3.x, vector7.y, position3.z), vector5) <= 1.5f)
				{
					vector7 = UnityEngine.Vector3.MoveTowards(vector5, vector7, 1f);
					break;
				}
				vector7 = new UnityEngine.Vector3(position3.x, vector7.y, position3.z);
				foreach (Character allCharacter in Character.GetAllCharacters())
				{
					HitData hitData3 = new HitData();
					hitData3.m_damage = player.GetCurrentWeapon().GetDamage();
					hitData3.ApplyModifier(Random.Range(0.8f, 1.2f) * num);
					hitData3.m_point = allCharacter.GetCenterPoint();
					hitData3.m_pushForce = 4f;
					hitData3.m_dir = allCharacter.transform.position - position3;
					hitData3.m_skill = ValheimLegends.DisciplineSkill;
					float num2 = UnityEngine.Vector3.Distance(allCharacter.transform.position, player.transform.position);
					if (BaseAI.IsEnemy(allCharacter, player) && num2 <= 2.5f && !kicklist.Contains(allCharacter.GetInstanceID()))
					{
						allCharacter.Damage(hitData3);
						Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_HeavyCrit"), allCharacter.GetCenterPoint(), UnityEngine.Quaternion.identity);
						kicklist.Add(allCharacter.GetInstanceID());
					}
				}
			}
			if (j > 10 && ZoneSystem.instance.GetSolidHeight(vector7) - vector7.y <= 2f)
			{
				vector7.y = ZoneSystem.instance.GetSolidHeight(vector7);
			}
			if ((float)fkickCount <= (float)fkickCountMax * 0.4f)
			{
				RaycastHit hitInfo4 = default(RaycastHit);
				UnityEngine.Vector3 vector10 = ((!Physics.Raycast(player.GetEyePoint(), player.transform.up, out hitInfo4, 1f, Script_Solidmask) || !hitInfo4.collider) ? (player.transform.position + UnityEngine.Vector3.up * 10f) : hitInfo4.point);
				if (Vector3.Distance(hitInfo4.point, player.GetEyePoint()) > 0.8f)
				{
					vector7.y += 0.15f;
				}
				else
				{
					vector7.y -= 0.18f;
				}
			}
			if (fkickCount % 3 == 0)
			{
				kicklist.Clear();
			}
			if (fkickCount < fkickCountMax)
			{
				fkickCount++;
				ValheimLegends.isChargingDash = true;
				ValheimLegends.dashCounter = 0;
			}
			playerBody.position = vector7;
			bool flag3 = false;
			if (fkickCount >= 9)
			{
				RaycastHit hitInfo5 = default(RaycastHit);
				bool flag4 = false;
				if (Physics.SphereCast(vector7, 0.05f, vector6, out hitInfo5, 1f, ScriptChar_Layermask) && (bool)hitInfo5.collider && hitInfo5.collider.gameObject != null)
				{
					vector5 = hitInfo5.point;
					Character component3 = null;
					hitInfo5.collider.gameObject.TryGetComponent<Character>(out component3);
					flag3 = component3 != null;
					if (component3 == null)
					{
						component3 = (Character)hitInfo5.collider.GetComponentInParent(typeof(Character));
						flag3 = component3 != null;
					}
					if (flag3 && BaseAI.IsEnemy(component3, player))
					{
						HitData hitData4 = new HitData();
						hitData4.m_damage = player.GetCurrentWeapon().GetDamage();
						hitData4.ApplyModifier(Random.Range(1f, 1.5f) * num);
						hitData4.m_point = vector5;
						hitData4.m_pushForce = 10f;
						hitData4.m_dir = vector5 - player.transform.position;
						hitData4.m_skill = ValheimLegends.DisciplineSkill;
						component3.Damage(hitData4);
						Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_perfectblock"), player.transform.position, UnityEngine.Quaternion.identity);
						Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_perfectblock"), vector5, UnityEngine.Quaternion.identity);
					}
					else
					{
						flag3 = false;
					}
				}
			}
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ChiPulse"), vector8 + player.transform.up * 0.2f, UnityEngine.Quaternion.LookRotation(player.transform.forward));
			QueuedAttack = MonkAttackType.FlyingKick;
			if (flag3)
			{
				VL_Utility.RotatePlayerToTarget(player);
				((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("jump");
				vector6.y = 0f;
				playerBody.linearVelocity = vector6 * -1.5f + new UnityEngine.Vector3(0f, 10f, 0f);
				ValheimLegends.isChargingDash = false;
				sE_Monk3.maxHitCount = 8 + Mathf.RoundToInt(player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
					.m_level * 0.2f * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f)));
				sE_Monk3.hitCount += 2;
				sE_Monk3.hitCount = Mathf.Clamp(sE_Monk3.hitCount, 0, sE_Monk3.maxHitCount);
				sE_Monk3.refreshed = true;
			}
			altitude = 0f;
		}
	}

	public static void Process_Input(Player player, ref Rigidbody playerBody, ref float altitude, ref Animator anim)
	{
		SE_Monk sE_Monk = (SE_Monk)player.GetSEMan().GetStatusEffect("SE_VL_Monk".GetStableHashCode());
		if (VL_Utility.Ability3_Input_Down)
		{
			if (PlayerIsUnarmed)
			{
				if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
				{
					if (sE_Monk.hitCount >= 1)
					{
						StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
						statusEffect.m_ttl = VL_Utility.GetPsiBoltCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect);
						if (player.IsBlocking())
						{
							player.StartEmote("challenge");
							QueuedAttack = MonkAttackType.Surge;
							ValheimLegends.isChanneling = true;
							ValheimLegends.isChargingDash = true;
							ValheimLegends.dashCounter = 0;
						}
						else
						{
							VL_Utility.RotatePlayerToTarget(player);
							float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
								.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
							((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_axe2");
							QueuedAttack = MonkAttackType.Psibolt;
							ValheimLegends.isChargingDash = true;
							ValheimLegends.dashCounter = 0;
						}
						player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetPsiBoltSkillGain * (float)sE_Monk.hitCount);
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough energy for Chi Blast : (" + sE_Monk.hitCount.ToString("#") + "/" + VL_Utility.GetPsiBoltCost + ")");
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Must be unarmed to use this ability");
			}
		}
		else if (VL_Utility.Ability2_Input_Down)
		{
			if (PlayerIsUnarmed)
			{
				if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
				{
					if (player.GetStamina() >= VL_Utility.GetFlyingKickCost)
					{
						StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
						statusEffect2.m_ttl = VL_Utility.GetFlyingKickCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect2);
						player.UseStamina(VL_Utility.GetFlyingKickCost);
						float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("unarmed_kick");
						QueuedAttack = MonkAttackType.FlyingKickStart;
						ValheimLegends.isChargingDash = true;
						ValheimLegends.dashCounter = 0;
						kickDir = new UnityEngine.Vector3(player.GetLookDir().x, 0f, player.GetLookDir().z);
						fkickCount = 0;
						fkickCountMax = 18;
						kicklist = new List<int>();
						kicklist.Clear();
						player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetFlyingKickSkillGain);
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Flying Kick: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetFlyingKickCost + ")");
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Must be unarmed to use this ability");
			}
		}
		else if (VL_Utility.Ability1_Input_Down)
		{
			if (PlayerIsUnarmed)
			{
				if (player.IsBlocking())
                {
					if (player.GetStaminaPercentage() > 0.8f && player.GetStamina() >= 50f)
					{
						float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef).m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
						if (sE_Monk.hitCount < Mathf.FloorToInt(Mathf.Sqrt(level3)))
						{
							Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ChiPulse"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
							Object.Instantiate(ZNetScene.instance.GetPrefab("fx_Potion_frostresist"), player.transform.position, UnityEngine.Quaternion.identity);
							player.UseStamina(Mathf.Max(50f, player.GetMaxStamina() * 0.8f));
							sE_Monk.maxHitCount = 8 + Mathf.RoundToInt(Mathf.Sqrt(Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
								.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f))));
							sE_Monk.hitCount++;
							sE_Monk.hitCount = Mathf.Clamp(sE_Monk.hitCount, 0, sE_Monk.maxHitCount);
							sE_Monk.refreshed = true;

							player.Message(MessageHud.MessageType.TopLeft, "Power-Up Chi Charges: (" + sE_Monk.hitCount.ToString("#") + "/" + Mathf.FloorToInt(Mathf.Sqrt(level3)) + ")");
							if (sE_Monk.hitCount >= Mathf.FloorToInt(Mathf.Sqrt(level3))) 
							{
								StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
								statusEffect3.m_ttl = VL_Utility.GetMeteorPunchCooldownTime;
								player.GetSEMan().AddStatusEffect(statusEffect3);
							}
						}
						else
						{
							player.Message(MessageHud.MessageType.TopLeft, "Power-Up Chi Charges maxed! (" + sE_Monk.hitCount.ToString("#") + "/" + Mathf.FloorToInt(Mathf.Sqrt(level3)) + ")");
						}
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Power-Up: (" + player.GetStamina().ToString("#.#") + "/" + Mathf.Max(50f, player.GetMaxStamina() * 0.8f) + ")");
					}
				}
				else if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
				{
					if ((float)sE_Monk.hitCount >= VL_Utility.GetMeteorPunchCost)
					{
						VL_Utility.RotatePlayerToTarget(player);
						float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
						StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
						statusEffect3.m_ttl = VL_Utility.GetMeteorPunchCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect3);
						player.AddStamina(10f + 0.3f * level3);
						sE_Monk.hitCount -= Mathf.RoundToInt(VL_Utility.GetMeteorPunchCost);
						sE_Monk.refreshed = true;
						if (player.IsOnGround() || player.transform.position.y - ZoneSystem.instance.GetSolidHeight(player.transform.position) < 2f)
						{
							((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("unarmed_attack1");
							QueuedAttack = MonkAttackType.MeteorPunch;
							ValheimLegends.isChargingDash = true;
							ValheimLegends.dashCounter = 0;
						}
						else
						{
							((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("battleaxe_attack2");
							QueuedAttack = MonkAttackType.MeteorSlam;
							ValheimLegends.isChargingDash = true;
							ValheimLegends.dashCounter = 0;
							ValheimLegends.shouldValkyrieImpact = true;
						}
						player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetMeteorPunchSkillGain);
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough energy for Chi Strike: (" + sE_Monk.hitCount.ToString("#") + "/" + VL_Utility.GetMeteorPunchCost + ")");
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Must be unarmed to use this ability");
			}
		}
		else
		{
			ValheimLegends.isChanneling = false;
            ValheimLegends.channelingBlocksMovement = true;
        }
	}
}
