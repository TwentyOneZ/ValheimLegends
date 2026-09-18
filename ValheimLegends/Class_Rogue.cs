using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Rogue
{
	private static int ScriptChar_Layermask = LayerMask.GetMask("character", "character_noenv", "character_trigger", "character_net", "character_ghost", "Default", "static_solid", "Default_small", "piece_nonsolid", "piece", "terrain", "vehicle", "viewblock", "Water");

	public static UnityEngine.Vector3 fadePoint;

	public static UnityEngine.Vector3 backstabPoint;

	public static UnityEngine.Vector3 backstabVector;

	public static bool throwDagger = false;

	public static bool canDoubleJump = true;

	public static bool canGainTrick = false;

	public static bool PlayerUsingDaggerOnly
	{
		get
		{
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer.GetCurrentWeapon() != null)
			{
				ItemDrop.ItemData.SharedData shared = localPlayer.GetCurrentWeapon().m_shared;
				ItemDrop.ItemData value = Traverse.Create(localPlayer).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
				if (shared != null && (shared.m_name.ToLower().Contains("knife") || shared.m_name.Contains("dagger")) && value == null)
				{
					return true;
				}
			}
			return false;
		}
	}

	public static void Execute_Throw(Player player)
	{
		if (!throwDagger)
		{
			float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			UnityEngine.Vector3 vector = player.GetEyePoint() + player.GetLookDir() * 0.2f + player.transform.up * 0.3f + player.transform.right * 0.28f;
			GameObject prefab = ZNetScene.instance.GetPrefab("VL_PoisonBomb");
			GameObject gameObject = Object.Instantiate(prefab, vector, UnityEngine.Quaternion.identity);
			GameObject prefab2 = ZNetScene.instance.GetPrefab("VL_PoisonBombExplosion");
			Aoe componentInChildren = prefab2.gameObject.GetComponentInChildren<Aoe>();
			componentInChildren.m_damage.m_poison = (10f + 2f * level) * VL_GlobalConfigs.c_roguePoisonBomb;
			componentInChildren.m_ttl = 4f + 0.1f * level;
			componentInChildren.m_hitInterval = 0.5f;
			Projectile component = gameObject.GetComponent<Projectile>();
			component.name = "Poison Bomb";
			component.m_respawnItemOnHit = false;
			component.m_spawnOnHit = null;
			component.m_ttl = 10f;
			component.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector));
			component.m_spawnOnHit = prefab2;
			gameObject.transform.localScale = UnityEngine.Vector3.one * 0.5f;
			RaycastHit hitInfo = default(RaycastHit);
			UnityEngine.Vector3 position = player.transform.position;
			UnityEngine.Vector3 target = ((!Physics.Raycast(vector, player.GetLookDir(), out hitInfo, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
			HitData hitData = new HitData();
			hitData.m_skill = ValheimLegends.AlterationSkill;
			hitData.SetAttacker(player);
			UnityEngine.Vector3 vector2 = UnityEngine.Vector3.MoveTowards(gameObject.transform.position, target, 1f);
			component.Setup(player, (vector2 - gameObject.transform.position) * 25f, -1f, hitData, null, null);
			Traverse.Create(component).Field("m_skill").SetValue(ValheimLegends.AlterationSkill);
			gameObject = null;
		}
		else
		{
			float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
			UnityEngine.Vector3 vector3 = player.GetEyePoint() + player.GetLookDir() * 0.2f + player.transform.up * 0.3f + player.transform.right * 0.28f;
			GameObject prefab3 = ZNetScene.instance.GetPrefab("VL_ThrowingKnife");
			GameObject gameObject2 = Object.Instantiate(prefab3, vector3, UnityEngine.Quaternion.identity);
			Projectile component2 = gameObject2.GetComponent<Projectile>();
			component2.name = "ThrowingKnife";
			component2.m_respawnItemOnHit = false;
			component2.m_spawnOnHit = null;
			component2.m_ttl = 10f;
			component2.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector3));
			gameObject2.transform.localScale = UnityEngine.Vector3.one * 0.2f;
			RaycastHit hitInfo2 = default(RaycastHit);
			UnityEngine.Vector3 position2 = player.transform.position;
			UnityEngine.Vector3 target2 = ((!Physics.Raycast(vector3, player.GetLookDir(), out hitInfo2, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo2.collider) ? (position2 + player.GetLookDir() * 1000f) : hitInfo2.point);
			HitData hitData2 = new HitData();
			hitData2.m_damage.m_pierce = Random.Range(5f + level2, 10f + 2f * level2) * VL_GlobalConfigs.c_rogueBonusThrowingDagger;
			hitData2.m_skill = ValheimLegends.DisciplineSkill;
			hitData2.SetAttacker(player);
			UnityEngine.Vector3 vector4 = UnityEngine.Vector3.MoveTowards(gameObject2.transform.position, target2, 1f);
			component2.Setup(player, (vector4 - gameObject2.transform.position) * 30f, -1f, hitData2, null, null);
			Traverse.Create(component2).Field("m_skill").SetValue(ValheimLegends.AlterationSkill);
			gameObject2 = null;
		}
	}

	public static void Process_Input(Player player, ref Rigidbody playerBody, ref float altitude)
	{
		if (ZInput.GetButtonDown("Jump") && !player.IsDead() && !player.InAttack() && !player.IsEncumbered() && !player.InDodge() && !player.IsKnockedBack())
		{
			SE_Rogue sE_Rogue = (SE_Rogue)player.GetSEMan().GetStatusEffect("SE_VL_Rogue".GetStableHashCode());
			if (!player.IsOnGround() && canDoubleJump && sE_Rogue != null && sE_Rogue.hitCount > 0)
			{
				UnityEngine.Vector3 velocity = player.GetVelocity();
				velocity.y = 0f;
				playerBody.linearVelocity = velocity * 2f + new UnityEngine.Vector3(0f, 8f, 0f);
				sE_Rogue.hitCount--;
				canDoubleJump = false;
				altitude = 0f;
				((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("jump");
			}
			else if (player.IsOnGround())
			{
				canDoubleJump = true;
			}
		}
		if (player.IsBlocking() && ZInput.GetButtonDown("Attack"))
		{
			SE_Rogue sE_Rogue2 = (SE_Rogue)player.GetSEMan().GetStatusEffect("SE_VL_Rogue".GetStableHashCode());
			if (sE_Rogue2 != null && sE_Rogue2.hitCount > 0)
			{
				sE_Rogue2.hitCount--;
				((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("throw_bomb");
				ValheimLegends.isChargingDash = true;
				ValheimLegends.dashCounter = 0;
				throwDagger = true;
				player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetPoisonBombSkillGain * 0.3f);
			}
		}
		if (VL_Utility.Ability3_Input_Down)
		{
			RaycastHit hitInfo = default(RaycastHit);
			UnityEngine.Vector3 position = player.transform.position;
			Physics.SphereCast(player.GetEyePoint(), 0.2f, player.GetLookDir(), out hitInfo, 150f, ScriptChar_Layermask);
			VL_Utility.SetTimer();
			if (hitInfo.collider != null && hitInfo.collider.gameObject != null)
			{
				Character component = null;
				hitInfo.collider.gameObject.TryGetComponent<Character>(out component);
				bool flag = component != null;
				List<Component> list = new List<Component>();
				list.Clear();
				hitInfo.collider.gameObject.GetComponents(list);
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
				if (flag && !component.IsPlayer())
				{
					if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
					{
						if (player.GetStamina() >= VL_Utility.GetBackstabCost)
						{
							StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
							statusEffect.m_ttl = VL_Utility.GetBackstabCooldownTime;
							player.GetSEMan().AddStatusEffect(statusEffect);
							player.UseStamina(VL_Utility.GetBackstabCost);
							float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
								.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
							Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Smokeburst"), player.GetEyePoint(), UnityEngine.Quaternion.identity);
							backstabVector = (component.transform.position - player.transform.position) / UnityEngine.Vector3.Distance(component.transform.position, player.transform.position);
							float num = -1.5f;
							_ = hitInfo.collider.bounds;
							_ = hitInfo.collider.bounds.size;
							if (true)
							{
								num = (hitInfo.collider.bounds.size.x + hitInfo.collider.bounds.size.z) / 2f;
								num = Mathf.Clamp(num, 0.6f, 2f);
								num *= -1f;
							}
							backstabPoint = component.transform.position + component.transform.forward * num;
							backstabPoint.y += 0.1f;
							playerBody.position = backstabPoint;
							player.transform.rotation = component.transform.rotation;
							((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("knife_stab2");
							if (BaseAI.IsEnemy(player, component))
							{
								UnityEngine.Vector3 dir = component.transform.position - player.transform.position;
								HitData hitData = new HitData();
								hitData.m_damage = player.GetCurrentWeapon().GetDamage();
								hitData.m_damage.Modify(Random.Range(0.6f, 0.8f) * (1f + 0.005f * level) * VL_GlobalConfigs.c_rogueBackstab);
								hitData.m_pushForce = 10f + 0.1f * level;
								hitData.m_point = component.GetEyePoint();
								hitData.m_dir = dir;
								hitData.m_skill = ValheimLegends.DisciplineSkill;
								component.Damage(hitData);
							}
							Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Smokeburst"), backstabPoint, UnityEngine.Quaternion.identity);
							Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Shadowburst"), backstabPoint + player.transform.up * 0.5f, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
							altitude = 0f;
							player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetBackstabSkillGain);
						}
						else
						{
							player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Backstab: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetBackstabCost + ")");
						}
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Invalid target");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "No target");
			}
		}
		else if (VL_Utility.Ability2_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetFadeCost)
				{
					StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect2.m_ttl = VL_Utility.GetFadeCooldownTime * VL_GlobalConfigs.c_rogueFadeCooldown;
					player.GetSEMan().AddStatusEffect(statusEffect2);
					player.UseStamina(VL_Utility.GetFadeCost);
					float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.IllusionSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					GameObject prefab = ZNetScene.instance.GetPrefab("vfx_odin_despawn");
					Object.Instantiate(prefab, player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_wraith_death"), player.transform.position, UnityEngine.Quaternion.identity);
					fadePoint = player.transform.position;
					canGainTrick = true;
					player.RaiseSkill(ValheimLegends.IllusionSkill, VL_Utility.GetFadeSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to set Fade point: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetFadeCost + ")");
				}
			}
			else if (player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if ((fadePoint - player.transform.position).magnitude < 100f)
				{
					GameObject prefab2 = ZNetScene.instance.GetPrefab("vfx_odin_despawn");
					Object.Instantiate(prefab2, player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_wraith_death"), player.transform.position, UnityEngine.Quaternion.identity);
					playerBody.position = fadePoint;
					if (canGainTrick)
					{
						SE_Rogue sE_Rogue3 = (SE_Rogue)player.GetSEMan().GetStatusEffect("SE_VL_Rogue".GetStableHashCode());
						sE_Rogue3.hitCount++;
						canGainTrick = false;
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Cannot fade at this distance.");
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
				if (player.GetStamina() >= VL_Utility.GetPoisonBombCost)
				{
					if (player.IsCrouching())
                    {
						StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
						statusEffect3.m_ttl = VL_Utility.GetPoisonBombCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect3, false, 0, 0f);
						player.UseStamina(VL_Utility.GetFadeCost);
						Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Smokeburst"), player.transform.position, UnityEngine.Quaternion.identity);
						Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Shadowburst"), player.transform.position + player.transform.up * 0.5f, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
						List<Character> list = new List<Character>();
						list.Clear();
						Character.GetCharactersInRange(player.GetCenterPoint(), 500f, list);
						foreach (Character item in list)
						{
							if (item.GetBaseAI() != null && item.GetBaseAI() is MonsterAI && item.GetBaseAI().IsEnemy((Character)player))
							{
								MonsterAI monsterAI2 = item.GetBaseAI() as MonsterAI;
								if (monsterAI2 != null && monsterAI2.GetTargetCreature() == player)
								{
									Traverse.Create(monsterAI2).Field("m_alerted").SetValue(false);
									Traverse.Create(monsterAI2).Field("m_targetCreature").SetValue(null);
								}
							}
						}
						player.Message(MessageHud.MessageType.Center, "Smoke Bomb!");
						player.RaiseSkill(global::ValheimLegends.ValheimLegends.IllusionSkill, VL_Utility.GetFadeSkillGain);
					}
					else
					{ 
						StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
						statusEffect3.m_ttl = VL_Utility.GetPoisonBombCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect3);
						player.UseStamina(VL_Utility.GetPoisonBombCost);
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("throw_bomb");
						ValheimLegends.isChargingDash = true;
						ValheimLegends.dashCounter = 0;
						throwDagger = false;
						player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetPoisonBombSkillGain);
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to throw Poison Bomb: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetPoisonBombCost + ")");
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
