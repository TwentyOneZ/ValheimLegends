using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Metavoker
{
	public enum MetavokerAttackType
	{
		ForceWave = 0x10
	}

	private static int Warp_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "Water", "character", "character_net", "character_ghost");

	private static int Light_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost");

	private static int SafeFall_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "Water");

	private static GameObject GO_CastFX;

	private static GameObject GO_Light;

	private static Projectile P_Light;

	private static StatusEffect SE_Root;

	private static GameObject GO_RootDefender;

	public static bool isReactivearmored = false;

	private static float warpCount;

	private static float warpDistance;

	private static int warpGrowthTrigger;

	public static MetavokerAttackType QueuedAttack;

	public static void Execute_Attack(Player player, ref Rigidbody playerBody, ref float altitude)
	{
		if (QueuedAttack != MetavokerAttackType.ForceWave)
		{
			return;
		}
		Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ForceWall"), player.GetEyePoint(), UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
		List<Character> list = new List<Character>();
		list.Clear();
		UnityEngine.Vector3 vector = player.GetCenterPoint() + player.transform.forward * 6f;
		Character.GetCharactersInRange(vector, 6f, list);
		float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
			.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
		List<Projectile> list2 = Object.FindObjectsByType<Projectile>(FindObjectsSortMode.None).ToList();
		if (list2 != null && list2.Count > 0)
		{
			foreach (Projectile item in list2)
			{
				if (Vector3.Distance(item.transform.position, vector) <= 6f)
				{
					item.m_ttl = 0.05f;
					string name = item.name.Substring(0, item.name.IndexOf('('));
					GameObject prefab = ZNetScene.instance.GetPrefab(name);
					if (prefab != null)
					{
						GameObject gameObject = Object.Instantiate(prefab, item.transform.position, UnityEngine.Quaternion.identity);
						Projectile component = gameObject.GetComponent<Projectile>();
						component.name = "DupeProj";
						component.m_respawnItemOnHit = false;
						component.m_spawnOnHit = null;
						component.m_ttl = 4f;
						component.transform.localRotation = UnityEngine.Quaternion.LookRotation(item.transform.forward * -1f);
						gameObject.transform.localScale = item.transform.localScale;
						HitData hitData = new HitData();
						hitData.m_damage = item.m_damage;
						hitData.SetAttacker(player);
						hitData.m_skill = ValheimLegends.EvocationSkill;
						component.Setup(player, item.GetVelocity() * -1f, -1f, hitData, null, null);
						Traverse.Create(component).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
						gameObject = null;
					}
				}
			}
		}
		foreach (Character item2 in list)
		{
			if (!BaseAI.IsEnemy(player, item2) || !VL_Utility.LOS_IsValid(item2, player.GetCenterPoint(), player.transform.position))
			{
				continue;
			}
			UnityEngine.Vector3 vector2 = item2.transform.position - player.transform.position;
			float magnitude = vector2.magnitude;
			Rigidbody value = Traverse.Create(item2).Field("m_body").GetValue<Rigidbody>();
			if (value != null)
			{
				float mass = value.mass;
				if (Random.value * (1f - mass / 100f) > 0.5f)
				{
					item2.Stagger(vector2);
				}
				mass *= 0.02f;
				UnityEngine.Vector3 vector3 = vector2 * ((15f - magnitude) / mass) + new UnityEngine.Vector3(0f, Mathf.Clamp(3f / mass, 1f, 5f), 0f);
                vector3 *= VL_GlobalConfigs.c_metavokerBonusForceWave;
				Traverse.Create(item2).Field("m_pushForce").SetValue(vector3);
				HitData hitData2 = new HitData();
				hitData2.m_damage.m_damage = magnitude * Random.Range(0.75f, 1.25f) * (1f + 0.02f * level) * VL_GlobalConfigs.c_metavokerBonusForceWave;
				hitData2.m_point = item2.GetEyePoint();
				hitData2.m_dir = vector2;
				hitData2.m_skill = ValheimLegends.EvocationSkill;
				item2.Damage(hitData2);
			}
		}
	}

	public static void Process_Input(Player player, ref float altitude, ref Rigidbody playerBody)
	{
		if (player.IsBlocking() && ZInput.GetButtonDown("Attack") && !player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()) && player.GetStamina() >= VL_Utility.GetForceWaveCost)
		{
			StatusEffect statusEffect = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
			statusEffect.m_ttl = VL_Utility.GetForceWaveCooldown;
			player.GetSEMan().AddStatusEffect(statusEffect);
			player.UseStamina(VL_Utility.GetForceWaveCost);
			VL_Utility.RotatePlayerToTarget(player);
			((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).StopAllCoroutines();
			((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("battleaxe_attack2");
			ValheimLegends.isChargingDash = true;
			ValheimLegends.dashCounter = 0;
			QueuedAttack = MetavokerAttackType.ForceWave;
			player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetForceWaveSkillGain);
		}
		if (P_Light != null)
		{
			P_Light.transform.position = player.GetEyePoint() + player.transform.up * 0.4f + player.transform.right * -0.8f;
		}
		if (ZInput.GetButton("Jump") && !player.IsOnGround() && !player.IsDead() && !player.InAttack() && !player.IsEncumbered() && !player.InDodge() && !player.IsKnockedBack())
		{
			UnityEngine.Vector3 velocity = playerBody.linearVelocity;
			if (velocity.y < 0f)
			{
				bool flag = true;
				if (!player.HaveStamina(1f))
				{
					if (player.IsPlayer())
					{
						Hud.instance.StaminaBarEmptyFlash();
					}
					flag = false;
				}
				if (flag)
				{
					player.UseStamina(0.6f * VL_GlobalConfigs.c_metavokerBonusSafeFallCost);
					RaycastHit hitInfo = default(RaycastHit);
					UnityEngine.Vector3 position = player.transform.position;
					UnityEngine.Vector3 vector = ((!Physics.Raycast(position, new UnityEngine.Vector3(0f, -1f, 0f), out hitInfo, float.PositiveInfinity, SafeFall_Layermask) || !hitInfo.collider) ? (position + player.transform.up * -1000f) : hitInfo.point);
					float y = hitInfo.point.y;
					float num = altitude - y;
					float num2 = Mathf.Clamp(-0.15f * velocity.y, 0f, 1.5f);
					float num3 = num2 / (0f - velocity.y);
					float num4 = num * num3;
					playerBody.linearVelocity = velocity + new UnityEngine.Vector3(0f, num2, 0f);
					altitude -= num4;
					Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ReverseLightburst"), player.transform.position, UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(0f, 1f, 0f)));
				}
			}
		}
		if (VL_Utility.Ability3_Input_Down)
		{
			if (player.IsBlocking())
			{
				ValheimLegends.shouldUseGuardianPower = false;
				SE_Reactivearmor SE_Reactivearmor = (SE_Reactivearmor)ScriptableObject.CreateInstance(typeof(SE_Reactivearmor));
				if (player.GetSEMan().HaveStatusEffect("SE_VL_Reactivearmor".GetStableHashCode()))
				{
					StatusEffect statusEffect2 = player.GetSEMan().GetStatusEffect("SE_VL_Reactivearmor".GetStableHashCode());
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightSuction"), player.transform.position, UnityEngine.Quaternion.identity);
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), player.GetEyePoint(), UnityEngine.Quaternion.identity);
					player.GetSEMan().RemoveStatusEffect(SE_Reactivearmor, quiet: true);
					List<Character> allCharacters = Character.GetAllCharacters();
					float chargesLeft = statusEffect2.m_ttl;
					foreach (Character item in allCharacters)
					{
						if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 6f && VL_Utility.LOS_IsValid(item, player.transform.position, player.GetCenterPoint()) && chargesLeft > 0)
						{
							UnityEngine.Vector3 forceDirection = item.transform.position - player.transform.position;
							item.Stagger(forceDirection);
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ForwardLightningShock"), item.transform.position, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightSuction"), item.GetEyePoint(), UnityEngine.Quaternion.identity);
							chargesLeft--;
						}
					}
					StatusEffect statusEffect4 = player.GetSEMan().GetStatusEffect("SE_VL_CDReactivearmor".GetStableHashCode());
					float level = Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
					float maxCharges = 3 + Mathf.RoundToInt(Mathf.Sqrt(level*2));
					float cooldownRestored = (VL_Utility.GetLightCooldownTime * 3) * (chargesLeft / maxCharges);
					if (player.GetSEMan().HaveStatusEffect("SE_VL_CDReactivearmor".GetStableHashCode()))
					{
						statusEffect4.m_ttl = Mathf.Max(statusEffect4.m_ttl - cooldownRestored, (VL_Utility.GetLightCooldownTime * 3));
					}
					else
					{
						statusEffect4.m_ttl = VL_Utility.GetLightCooldownTime * 3;
						player.GetSEMan().AddStatusEffect(statusEffect4);
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Reactive Armor is not up.");
				}
			}
			if (!player.IsBlocking() && !player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				
				ValheimLegends.shouldUseGuardianPower = false;
				if (player.GetStamina() >= VL_Utility.GetWarpCost && !ValheimLegends.isChanneling)
				{
					StatusEffect statusEffect2 = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
					statusEffect2.m_ttl = VL_Utility.GetWarpCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect2);
					player.UseStamina(VL_Utility.GetWarpCost);
					VL_Utility.RotatePlayerToTarget(player);
					player.StartEmote("point");
					ValheimLegends.isChanneling = true;
					warpDistance = 15f;
					warpGrowthTrigger = 10;
					player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetWarpSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to initiate Warp: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetWarpCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability3_Input_Pressed && player.GetStamina() > VL_Utility.GetWarpCostPerUpdate && ValheimLegends.isChanneling && Mathf.Max(0f, altitude - player.transform.position.y) <= 2f)
		{
			VL_Utility.SetTimer();
			warpCount += 1f;
			player.UseStamina(VL_Utility.GetWarpCostPerUpdate);
			ValheimLegends.isChanneling = true;
			if (warpCount >= (float)warpGrowthTrigger)
			{
				warpCount = 0f;
				Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightSuction"), player.transform.position, UnityEngine.Quaternion.identity);
				GameObject prefab = ZNetScene.instance.GetPrefab("fx_guardstone_permitted_add");
				player.RaiseSkill(ValheimLegends.EvocationSkill, 0.06f);
				warpDistance += 5f;
			}
		}
		else if ((VL_Utility.Ability3_Input_Up || player.GetStamina() <= VL_Utility.GetWarpCostPerUpdate || player.GetStamina() <= 2f) && ValheimLegends.isChanneling)
		{
			float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			warpDistance = warpDistance * (1f + 0.01f * level) * VL_GlobalConfigs.c_metavokerWarpDistance;
			ValheimLegends.isChanneling = false;
            ValheimLegends.channelingBlocksMovement = true;
            RaycastHit hitInfo2 = default(RaycastHit);
			UnityEngine.Vector3 eyePoint = player.GetEyePoint();
			UnityEngine.Vector3 target = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo2, float.PositiveInfinity, Warp_Layermask) || !hitInfo2.collider) ? (eyePoint + player.GetLookDir() * 1000f) : hitInfo2.point);
			UnityEngine.Vector3 vector2 = UnityEngine.Vector3.MoveTowards(eyePoint, target, 1f);
			float magnitude = (hitInfo2.point - eyePoint).magnitude;
			float num5 = (warpDistance * player.GetLookDir()).magnitude;
			float num6 = 0f;
			if (num5 > magnitude)
			{
				num6 = num5 - magnitude;
				num5 = magnitude;
			}
			bool flag2 = num5 >= 140f;
			UnityEngine.Vector3 vector3 = UnityEngine.Vector3.MoveTowards(player.transform.position, target, num5);
			UnityEngine.Vector3 position2 = vector3 + player.GetLookDir() * -10f;
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightburst"), player.GetEyePoint(), UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
			if (num5 > 0f)
			{
				Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ForwardLightningShock"), position2, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
				List<Character> list = new List<Character>();
				list.Clear();
				Character.GetCharactersInRange(vector3, 8f + 0.02f * level, list);
				bool flag3 = false;
				foreach (Character item in list)
				{
					if (BaseAI.IsEnemy(player, item) && VL_Utility.LOS_IsValid(item, player.transform.position))
					{
						UnityEngine.Vector3 vector4 = item.transform.position - player.transform.position;
						HitData hitData = new HitData();
						hitData.m_damage.m_lightning = Random.Range(num6 * (level / 15f), num6 * (level / 10f)) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_metavokerWarpDamage;
						hitData.m_pushForce = (num6 + level) * 0.1f;
						hitData.m_point = item.GetEyePoint();
						hitData.m_dir = item.transform.position - player.transform.position;
						hitData.m_skill = ValheimLegends.EvocationSkill;
						item.Damage(hitData);
						flag3 = true;
					}
				}
				if (!flag3 && !flag2)
				{
					float v = num6 * 1.5f;
					player.AddStamina(v);
				}
			}
			if (flag2)
			{
				if (num5 >= 200f)
				{
					player.TeleportTo(vector3, player.transform.rotation, distantTeleport: true);
				}
				else
				{
					player.TeleportTo(vector3, player.transform.rotation, distantTeleport: false);
				}
			}
			else
			{
				playerBody.position = vector3;
			}
			altitude = 0f;
		}
		else if (VL_Utility.Ability2_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetReplicaCost)
				{
					UnityEngine.Vector3 lookDir = player.GetLookDir();
					lookDir.y = 0f;
					player.transform.rotation = UnityEngine.Quaternion.LookRotation(lookDir);
					ValheimLegends.shouldUseGuardianPower = false;
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = VL_Utility.GetReplicaCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					player.UseStamina(VL_Utility.GetReplicaCost);
					float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.IllusionSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("gpower");
					Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Replica"), player.transform.position + player.transform.up * 0.6f, UnityEngine.Quaternion.identity);
					List<Character> list2 = new List<Character>();
					foreach (Character allCharacter in Character.GetAllCharacters())
					{
						if (!allCharacter.IsBoss())
						{
							list2.Add(allCharacter);
						}
					}
					for (int i = 0; i < list2.Count; i++)
					{
						Character character = list2[i];
						if (!BaseAI.IsEnemy(player, character) || !((character.transform.position - player.transform.position).magnitude <= 18f + 0.05f * level2))
						{
							continue;
						}
						string name = character.name.Substring(0, character.name.IndexOf('('));
						GameObject prefab = ZNetScene.instance.GetPrefab(name);
						if (prefab != null)
						{
							if (prefab.GetComponent<CharacterTimedDestruction>() == null)
							{
								prefab.AddComponent<CharacterTimedDestruction>();
							}
							prefab.GetComponent<CharacterTimedDestruction>().m_timeoutMin = 8f + 0.2f * level2;
							prefab.GetComponent<CharacterTimedDestruction>().m_timeoutMax = 8f + 0.2f * level2;
							UnityEngine.Vector3 position3 = character.transform.position;
							position3.x += 5f * Random.Range(-1f, 1f);
							GameObject gameObject = Object.Instantiate(prefab, position3, UnityEngine.Quaternion.Inverse(character.transform.rotation));
							CharacterTimedDestruction component = gameObject.GetComponent<CharacterTimedDestruction>();
							if (component != null)
							{
								component.m_timeoutMin = 8f + 0.2f * level2;
								component.m_timeoutMax = component.m_timeoutMin;
								component.Trigger();
							}
							Character component2 = gameObject.GetComponent<Character>();
							component2.SetMaxHealth(1f + level2);
							component2.transform.localScale = 0.8f * UnityEngine.Vector3.one;
							SE_Companion sE_Companion = (SE_Companion)ScriptableObject.CreateInstance(typeof(SE_Companion));
							sE_Companion.m_ttl = 8f + 0.2f * level2;
							sE_Companion.damageModifier = 0.05f + 0.0075f * level2 * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_metavokerReplica;
							sE_Companion.summoner = player;
							component2.GetSEMan().AddStatusEffect(sE_Companion);
							component2.m_faction = Character.Faction.Players;
							component2.SetTamed(tamed: true);
							CharacterDrop component3 = component2.GetComponent<CharacterDrop>();
							if (component3 != null)
							{
								component3.m_drops.Clear();
							}
							component2.name = "VL_" + component2.name;
							component2.m_name = "(" + component2.m_name + ")";
							Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ReplicaCreate"), component2.transform.position + component2.transform.up * 0.2f, UnityEngine.Quaternion.identity);
						}
					}
					player.RaiseSkill(ValheimLegends.IllusionSkill, VL_Utility.GetReplicaSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to create illusions: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetReplicaCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability1_Input_Down)
		{
			if (player.IsBlocking() && !player.GetSEMan().HaveStatusEffect("SE_VL_CDReactivearmor".GetStableHashCode()))
			{
				ValheimLegends.shouldUseGuardianPower = false;
				if (player.GetStamina() >= VL_Utility.GetLightCost)
				{
					StatusEffect statusEffect4 = (SE_CDReactivearmor)ScriptableObject.CreateInstance(typeof(SE_CDReactivearmor));
					statusEffect4.m_ttl = VL_Utility.GetLightCooldownTime * 6;
					player.GetSEMan().AddStatusEffect(statusEffect4);
					player.UseStamina(VL_Utility.GetLightCost);
					SE_Reactivearmor SE_Reactivearmor = (SE_Reactivearmor)ScriptableObject.CreateInstance(typeof(SE_Reactivearmor));
					if (player.GetSEMan().HaveStatusEffect("SE_VL_Reactivearmor".GetStableHashCode()))
					{
						StatusEffect statusEffect2 = player.GetSEMan().GetStatusEffect("SE_VL_Reactivearmor".GetStableHashCode());
						player.GetSEMan().RemoveStatusEffect(statusEffect2);
					}
					player.GetSEMan().AddStatusEffect(SE_Reactivearmor);
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightburst"), player.GetEyePoint(), UnityEngine.Quaternion.LookRotation(Player.m_localPlayer.GetLookDir()));
					player.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetLightSkillGain * 3);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to cast Reactive Armor: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetWarpCost + ")");
				}
			}
			if (!player.IsBlocking() && P_Light != null && (P_Light.transform.position - player.GetEyePoint()).magnitude < 2f)
			{
				float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.IllusionSkillDef)
					.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
				P_Light.m_ttl = 0.05f;
				HitData hitData2 = new HitData();
				hitData2.m_skill = ValheimLegends.EvocationSkill;
				UnityEngine.Vector3 vector5 = player.GetEyePoint() + player.transform.up * 0.5f + player.transform.right * -1f;
				GameObject prefab2 = ZNetScene.instance.GetPrefab("VL_Light");
				GameObject gameObject2 = Object.Instantiate(prefab2, vector5, UnityEngine.Quaternion.identity);
				Projectile component4 = gameObject2.GetComponent<Projectile>();
				component4.m_respawnItemOnHit = false;
				component4.m_spawnOnHit = null;
				component4.m_ttl = 5f;
				component4.m_gravity = 0.25f;
				component4.m_rayRadius = 0.1f;
				component4.m_aoe = 4f + 0.04f * level3;
				component4.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector5));
				gameObject2.transform.localScale = UnityEngine.Vector3.zero;
				RaycastHit hitInfo3 = default(RaycastHit);
				UnityEngine.Vector3 position4 = player.transform.position;
				UnityEngine.Vector3 target2 = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo3, float.PositiveInfinity, Light_Layermask) || !hitInfo3.collider) ? (position4 + player.GetLookDir() * 1000f) : hitInfo3.point);
				hitData2.m_damage.m_lightning = Random.Range(5f + 0.3f * level3, 10f + 0.6f * level3) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_meteavokerLight;
				hitData2.m_damage.m_pierce = Random.Range(5f + 0.3f * level3, 10f + 0.6f * level3) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_meteavokerLight;
				hitData2.m_pushForce = (100f + 2f * level3) * VL_GlobalConfigs.c_meteavokerLight;
				hitData2.SetAttacker(player);
				UnityEngine.Vector3 vector6 = UnityEngine.Vector3.MoveTowards(gameObject2.transform.position, target2, 1f);
				component4.Setup(player, (vector6 - gameObject2.transform.position) * 80f, -1f, hitData2, null, null);
				Traverse.Create(component4).Field("m_skill").SetValue(ValheimLegends.IllusionSkill);
				gameObject2 = null;
				GO_Light = null;
			}
			else if (!player.IsBlocking() && !player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetLightCost)
				{
					StatusEffect statusEffect4 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect4.m_ttl = VL_Utility.GetLightCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect4);
					player.UseStamina(VL_Utility.GetLightCost);
					float level4 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.IllusionSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					player.StartEmote("cheer");
					VL_Utility.SetTimer();
					UnityEngine.Vector3 vector7 = player.GetEyePoint() + player.transform.up * 0.4f + player.transform.right * -0.8f;
					Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), vector7, UnityEngine.Quaternion.identity);
					GameObject prefab3 = ZNetScene.instance.GetPrefab("VL_Light");
					GO_Light = Object.Instantiate(prefab3, vector7, UnityEngine.Quaternion.identity);
					P_Light = GO_Light.GetComponent<Projectile>();
					P_Light.m_respawnItemOnHit = false;
					P_Light.m_spawnOnHit = null;
					P_Light.m_ttl = 300f;
					P_Light.m_gravity = 0f;
					P_Light.m_rayRadius = 0.1f;
					P_Light.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector7));
					GO_Light.transform.localScale = UnityEngine.Vector3.zero;
					RaycastHit hitInfo4 = default(RaycastHit);
					UnityEngine.Vector3 position5 = player.transform.position;
					UnityEngine.Vector3 target3 = ((!Physics.Raycast(vector7, player.GetLookDir(), out hitInfo4, float.PositiveInfinity, Light_Layermask) || !hitInfo4.collider) ? (position5 + player.GetLookDir() * 1000f) : hitInfo4.point);
					HitData hitData3 = new HitData();
					hitData3.m_skill = ValheimLegends.EvocationSkill;
					UnityEngine.Vector3 vector8 = UnityEngine.Vector3.MoveTowards(GO_Light.transform.position, target3, 1f);
					P_Light.Setup(player, UnityEngine.Vector3.zero, -1f, hitData3, null, null);
					Traverse.Create(P_Light).Field("m_skill").SetValue(ValheimLegends.IllusionSkill);
					player.RaiseSkill(ValheimLegends.IllusionSkill, VL_Utility.GetLightSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to for Light: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetLightCost + ")");
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
