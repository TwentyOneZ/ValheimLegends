using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Enchanter
{
	public enum EnchanterAttackType
	{
		Charm = 10,
		Shock = 8,
		None = 0
	}

	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

	private static int ScriptChar_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "piece", "terrain", "vehicle", "viewblock", "character", "character_noenv", "character_trigger", "character_net", "character_ghost", "Water");

	private static bool zonechargeCharging = false;

	private static float zonechargeCount;

	private static int zonechargeChargeAmount;

	private static int zonechargeChargeAmountMax;

	private static float zonechargeSkillGain = 0f;

	public static EnchanterAttackType QueuedAttack;

	public static StatusEffect HasZoneBuffTime(Player p)
	{
		foreach (StatusEffect statusEffect in p.GetSEMan().GetStatusEffects())
		{
			if (statusEffect.m_name.StartsWith("Biome: "))
			{
				return statusEffect;
			}
		}
		return null;
	}

	public static StatusEffect HasEnchantBuff(Character p)
	{
		foreach (StatusEffect statusEffect in p.GetSEMan().GetStatusEffects())
		{
			if (statusEffect.m_name.StartsWith("Enchant "))
			{
				return statusEffect;
			}
		}
		return null;
	}

	public static StatusEffect HasEnchantWeapon(Character p)
	{
		foreach (StatusEffect statusEffect in p.GetSEMan().GetStatusEffects())
		{
			if (statusEffect.m_name.EndsWith(" Weapon"))
			{
				return statusEffect;
			}
		}
		return null;
	}

	public static StatusEffect HasEnchantArmor(Character p)
	{
		foreach (StatusEffect statusEffect in p.GetSEMan().GetStatusEffects())
		{
			if (statusEffect.m_name.EndsWith(" Armor"))
			{
				return statusEffect;
			}
		}
		return null;
	}

	public static void Execute_Attack(Player player, ref Rigidbody playerBody, ref float altitude)
	{
		if (QueuedAttack == EnchanterAttackType.Charm)
		{
			float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.IllusionSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			UnityEngine.Vector3 vector = player.GetEyePoint() + player.transform.up * 0.1f + player.GetLookDir() * 0.5f + player.transform.right * 0.25f;
			GameObject prefab = ZNetScene.instance.GetPrefab("VL_Charm");
			GameObject gameObject = Object.Instantiate(prefab, vector, UnityEngine.Quaternion.identity);
			Projectile component = gameObject.GetComponent<Projectile>();
			component.name = "VL_Charm";
			component.m_respawnItemOnHit = false;
			component.m_spawnOnHit = null;
			component.m_ttl = 30f * VL_GlobalConfigs.c_enchanterCharm;
			component.m_gravity = 0f;
			component.m_rayRadius = 0.01f;
			component.m_aoe = 1f + (EpicMMOSystem.LevelSystem.Instance.getLevel() * (1f + Mathf.Sqrt(level)) / 600f);
			//Debug.Log("mAoE:" + component.m_aoe.ToString("#.#"));
			component.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector));
			gameObject.transform.localScale = UnityEngine.Vector3.zero;
			RaycastHit hitInfo = default(RaycastHit);
			UnityEngine.Vector3 position = player.transform.position;
			UnityEngine.Vector3 target = ((!Physics.Raycast(vector, player.GetLookDir(), out hitInfo, 1000f, ScriptChar_Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
			HitData hitData = new HitData();
			hitData.m_skill = ValheimLegends.IllusionSkill;
			UnityEngine.Vector3 vector2 = UnityEngine.Vector3.MoveTowards(gameObject.transform.position, target, 1f);
			component.Setup(player, (vector2 - gameObject.transform.position) * 40f, -1f, hitData, null, null);
			Traverse.Create(component).Field("m_skill").SetValue(ValheimLegends.IllusionSkill);
		}
		else
		{
			if (QueuedAttack != EnchanterAttackType.Shock)
			{
				return;
			}
			StatusEffect statusEffect = HasZoneBuffTime(player);
			if (!(statusEffect != null))
			{
				return;
			}
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Shock"), player.GetEyePoint() + player.GetLookDir() * 2.5f + player.transform.right * 0.25f, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
			float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			UnityEngine.Vector3 vector3 = player.GetEyePoint() + player.GetLookDir() * 4f;
			List<Character> list = new List<Character>();
			list.Clear();
			Character.GetCharactersInRange(vector3, 4f, list);
			foreach (Character item in list)
			{
				if (BaseAI.IsEnemy(player, item) && VL_Utility.LOS_IsValid(item, vector3, vector3))
				{
					UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
					HitData hitData2 = new HitData();
					hitData2.m_damage.m_lightning = 15f + level2 + statusEffect.m_ttl * Random.Range(0.03f, 0.06f) * (1f + 0.1f * level2) * VL_GlobalConfigs.c_enchanterBiomeShock;
					hitData2.m_pushForce = 0f;
					hitData2.m_point = item.GetEyePoint();
					hitData2.m_dir = dir;
					hitData2.m_skill = ValheimLegends.AlterationSkill;
					item.Damage(hitData2);
					item.Stagger(hitData2.m_dir);
				}
			}
			if (statusEffect.name == "SE_VL_BiomeAsh")
			{
				SE_BiomeAsh sE_BiomeAsh = statusEffect as SE_BiomeAsh;
				if (sE_BiomeAsh.biomeLight != null)
				{
					Object.Destroy(sE_BiomeAsh.biomeLight);
				}
			}
			player.GetSEMan().RemoveStatusEffect(statusEffect);
		}
	}

	public static void Process_Input(Player player, ref float altitude)
	{
		if (VL_Utility.Ability3_Input_Down && !zonechargeCharging)
		{
			if (player.IsBlocking())
			{
				StatusEffect statusEffect = HasZoneBuffTime(player);
				if (statusEffect != null && statusEffect.m_ttl > 0f && QueuedAttack != EnchanterAttackType.Shock)
				{
					QueuedAttack = EnchanterAttackType.Shock;
					ValheimLegends.isChargingDash = true;
					ValheimLegends.dashCounter = 0;
					VL_Utility.RotatePlayerToTarget(player);
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("unarmed_attack0");
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Need an active Zone Charge effect.");
				}
			}
			else if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()) && !zonechargeCharging)
			{
				if (player.GetStamina() >= VL_Utility.GetZoneChargeCost)
				{
					float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
					player.StartEmote("challenge");
					ValheimLegends.isChanneling = true;
					ValheimLegends.shouldUseGuardianPower = false;
					StatusEffect statusEffect2 = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
					statusEffect2.m_ttl = VL_Utility.GetZoneChargeCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect2);
					player.UseStamina(VL_Utility.GetZoneChargeCost);
					Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleTailField"), player.transform.position, UnityEngine.Quaternion.identity);
					zonechargeCharging = true;
					zonechargeChargeAmount = 0;
					zonechargeChargeAmountMax = Mathf.RoundToInt(15f * (1f - level * 0.005f));
					zonechargeCount = 10f;
					zonechargeSkillGain += VL_Utility.GetZoneChargeSkillGain;
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to begin Zone Charge : (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetZoneChargeCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability3_Input_Pressed && player.GetStamina() > 1f && player.GetStamina() > VL_Utility.GetZoneChargeCostPerUpdate && Mathf.Max(0f, altitude - player.transform.position.y) <= 1f && zonechargeCharging && ValheimLegends.isChanneling)
		{
			VL_Utility.SetTimer();
			ValheimLegends.isChanneling = true;
			zonechargeChargeAmount++;
			player.UseStamina(VL_Utility.GetZoneChargeCostPerUpdate);
			if (zonechargeChargeAmount >= zonechargeChargeAmountMax)
			{
				zonechargeCount += 2f;
				zonechargeChargeAmount = 0;
				Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleTailField"), player.transform.position, UnityEngine.Quaternion.identity);
				zonechargeSkillGain += 0.2f;
			}
		}
		else if ((VL_Utility.Ability3_Input_Up || player.GetStamina() <= 1f || player.GetStamina() <= VL_Utility.GetZoneChargeCostPerUpdate || Mathf.Max(0f, altitude - player.transform.position.y) >= 1f) && zonechargeCharging && ValheimLegends.isChanneling)
		{
			float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
			Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleFieldBurst"), player.transform.position, UnityEngine.Quaternion.identity);
			List<Character> list = new List<Character>();
			list.Clear();
			Character.GetCharactersInRange(player.transform.position, 30f + 0.2f * level2, list);
			float num = (3f * (EpicMMOSystem.LevelSystem.Instance.getLevel() * 10f / 6f * (1f + level2 / 300f)) + 2f * zonechargeCount) * VL_GlobalConfigs.c_enchanterBiome;
			if (player.GetCurrentBiome() == Heightmap.Biome.Meadows)
			{
				GameObject prefab = ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium");
				foreach (Character item in list)
				{
					SE_BiomeMeadows sE_BiomeMeadows = (SE_BiomeMeadows)ScriptableObject.CreateInstance(typeof(SE_BiomeMeadows));
					sE_BiomeMeadows.casterPower = level2;
					sE_BiomeMeadows.casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
					sE_BiomeMeadows.m_ttl = SE_BiomeMeadows.m_baseTTL + num;
					sE_BiomeMeadows.caster = player;
					if (!BaseAI.IsEnemy(item, player))
					{
						if (item == Player.m_localPlayer)
						{
							item.GetSEMan().AddStatusEffect(sE_BiomeMeadows, resetTime: true);
						}
						else if (item.IsPlayer())
						{
							item.GetSEMan().AddStatusEffect(sE_BiomeMeadows.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item.GetSEMan().AddStatusEffect(sE_BiomeMeadows, resetTime: true);
						}
						Object.Instantiate(prefab, item.GetCenterPoint(), UnityEngine.Quaternion.identity);
					}
				}
			}
			else if (player.GetCurrentBiome() == Heightmap.Biome.BlackForest)
			{
				GameObject prefab2 = ZNetScene.instance.GetPrefab("fx_Potion_frostresist");
				foreach (Character item2 in list)
				{
					SE_BiomeBlackForest sE_BiomeBlackForest = (SE_BiomeBlackForest)ScriptableObject.CreateInstance(typeof(SE_BiomeBlackForest));
					sE_BiomeBlackForest.casterPower = level2;
					sE_BiomeBlackForest.casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
					sE_BiomeBlackForest.m_ttl = SE_BiomeBlackForest.m_baseTTL + num;
					sE_BiomeBlackForest.caster = player;
					if (!BaseAI.IsEnemy(item2, player))
					{
						if (item2 == Player.m_localPlayer)
						{
							item2.GetSEMan().AddStatusEffect(sE_BiomeBlackForest, resetTime: true);
						}
						else if (item2.IsPlayer())
						{
							item2.GetSEMan().AddStatusEffect(sE_BiomeBlackForest.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item2.GetSEMan().AddStatusEffect(sE_BiomeBlackForest, resetTime: true);
						}
						Object.Instantiate(prefab2, item2.GetCenterPoint(), UnityEngine.Quaternion.identity);
					}
				}
			}
			else if (player.GetCurrentBiome() == Heightmap.Biome.Swamp)
			{
				GameObject prefab3 = ZNetScene.instance.GetPrefab("vfx_Potion_health_medium");
				foreach (Character item3 in list)
				{
					SE_BiomeSwamp sE_BiomeSwamp = (SE_BiomeSwamp)ScriptableObject.CreateInstance(typeof(SE_BiomeSwamp));
					sE_BiomeSwamp.casterPower = level2;
					sE_BiomeSwamp.casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
					sE_BiomeSwamp.m_ttl = SE_BiomeSwamp.m_baseTTL + num;
					sE_BiomeSwamp.caster = player;
					if (!BaseAI.IsEnemy(item3, player))
					{
						if (item3 == Player.m_localPlayer)
						{
							item3.GetSEMan().AddStatusEffect(sE_BiomeSwamp, resetTime: true);
						}
						else if (item3.IsPlayer())
						{
							item3.GetSEMan().AddStatusEffect(sE_BiomeSwamp.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item3.GetSEMan().AddStatusEffect(sE_BiomeSwamp, resetTime: true);
						}
						Object.Instantiate(prefab3, item3.GetCenterPoint(), UnityEngine.Quaternion.identity);
					}
				}
			}
			else if (player.GetCurrentBiome() == Heightmap.Biome.Mountain)
			{
				GameObject prefab4 = ZNetScene.instance.GetPrefab("fx_Potion_frostresist");
				foreach (Character item4 in list)
				{
					SE_BiomeMountain sE_BiomeMountain = (SE_BiomeMountain)ScriptableObject.CreateInstance(typeof(SE_BiomeMountain));
					sE_BiomeMountain.casterPower = level2;
					sE_BiomeMountain.casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
					sE_BiomeMountain.m_ttl = SE_BiomeMountain.m_baseTTL + num;
					sE_BiomeMountain.caster = player;
					if (!BaseAI.IsEnemy(item4, player))
					{
						if (item4 == Player.m_localPlayer)
						{
							item4.GetSEMan().AddStatusEffect(sE_BiomeMountain, resetTime: true);
						}
						else if (item4.IsPlayer())
						{
							item4.GetSEMan().AddStatusEffect(sE_BiomeMountain.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item4.GetSEMan().AddStatusEffect(sE_BiomeMountain, resetTime: true);
						}
						Object.Instantiate(prefab4, item4.GetCenterPoint(), UnityEngine.Quaternion.identity);
					}
				}
			}
			else if (player.GetCurrentBiome() == Heightmap.Biome.Plains)
			{
				GameObject prefab5 = ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium");
				foreach (Character item5 in list)
				{
					SE_BiomePlains sE_BiomePlains = (SE_BiomePlains)ScriptableObject.CreateInstance(typeof(SE_BiomePlains));
					sE_BiomePlains.casterPower = level2;
					sE_BiomePlains.casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
					sE_BiomePlains.m_ttl = SE_BiomePlains.m_baseTTL + num;
					sE_BiomePlains.caster = player;
					if (!BaseAI.IsEnemy(item5, player))
					{
						if (item5 == Player.m_localPlayer)
						{
							item5.GetSEMan().AddStatusEffect(sE_BiomePlains, resetTime: true);
						}
						else if (item5.IsPlayer())
						{
							item5.GetSEMan().AddStatusEffect(sE_BiomePlains.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item5.GetSEMan().AddStatusEffect(sE_BiomePlains, resetTime: true);
						}
						Object.Instantiate(prefab5, item5.GetCenterPoint(), UnityEngine.Quaternion.identity);
					}
				}
			}
			else if (player.GetCurrentBiome() == Heightmap.Biome.Ocean)
			{
				GameObject prefab6 = ZNetScene.instance.GetPrefab("fx_Potion_frostresist");
				foreach (Character item6 in list)
				{
					SE_BiomeOcean sE_BiomeOcean = (SE_BiomeOcean)ScriptableObject.CreateInstance(typeof(SE_BiomeOcean));
					sE_BiomeOcean.casterPower = level2;
					sE_BiomeOcean.casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
					sE_BiomeOcean.m_ttl = SE_BiomeOcean.m_baseTTL + num;
					sE_BiomeOcean.caster = player;
					if (!BaseAI.IsEnemy(item6, player))
					{
						if (item6 == Player.m_localPlayer)
						{
							item6.GetSEMan().AddStatusEffect(sE_BiomeOcean, resetTime: true);
						}
						else if (item6.IsPlayer())
						{
							item6.GetSEMan().AddStatusEffect(sE_BiomeOcean.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item6.GetSEMan().AddStatusEffect(sE_BiomeOcean, resetTime: true);
						}
						Object.Instantiate(prefab6, item6.GetCenterPoint(), UnityEngine.Quaternion.identity);
					}
				}
			}
			else if (player.GetCurrentBiome() == Heightmap.Biome.Mistlands)
			{
				GameObject prefab7 = ZNetScene.instance.GetPrefab("fx_Potion_frostresist");
				foreach (Character item7 in list)
				{
					SE_BiomeMist sE_BiomeMist = (SE_BiomeMist)ScriptableObject.CreateInstance(typeof(SE_BiomeMist));
					sE_BiomeMist.casterPower = level2;
					sE_BiomeMist.casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
					sE_BiomeMist.m_ttl = SE_BiomeMist.m_baseTTL + num;
					sE_BiomeMist.caster = player;
					if (!BaseAI.IsEnemy(item7, player))
					{
						if (item7 == Player.m_localPlayer)
						{
							item7.GetSEMan().AddStatusEffect(sE_BiomeMist, resetTime: true);
						}
						else if (item7.IsPlayer())
						{
							item7.GetSEMan().AddStatusEffect(sE_BiomeMist.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item7.GetSEMan().AddStatusEffect(sE_BiomeMist, resetTime: true);
						}
						Object.Instantiate(prefab7, item7.GetCenterPoint(), UnityEngine.Quaternion.identity);
					}
				}
			}
			else if (player.GetCurrentBiome() == Heightmap.Biome.AshLands)
			{
				GameObject prefab8 = ZNetScene.instance.GetPrefab("vfx_Potion_health_medium");
				foreach (Character item8 in list)
				{
					SE_BiomeAsh sE_BiomeAsh = (SE_BiomeAsh)ScriptableObject.CreateInstance(typeof(SE_BiomeAsh));
					sE_BiomeAsh.casterPower = level2;
					sE_BiomeAsh.casterLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();
					sE_BiomeAsh.m_ttl = SE_BiomeAsh.m_baseTTL + num;
					sE_BiomeAsh.caster = player;
					if (!BaseAI.IsEnemy(item8, player))
					{
						if (item8 == Player.m_localPlayer)
						{
							item8.GetSEMan().AddStatusEffect(sE_BiomeAsh, resetTime: true);
						}
						else if (item8.IsPlayer())
						{
							item8.GetSEMan().AddStatusEffect(sE_BiomeAsh.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item8.GetSEMan().AddStatusEffect(sE_BiomeAsh, resetTime: true);
						}
						Object.Instantiate(prefab8, item8.GetCenterPoint(), UnityEngine.Quaternion.identity);
					}
				}
			}
			else
			{
				ZLog.Log("Biome invalid.");
			}
			zonechargeCharging = false;
			zonechargeCount = 0f;
			zonechargeChargeAmount = 0;
			ValheimLegends.isChanneling = false;
			QueuedAttack = EnchanterAttackType.None;
			player.RaiseSkill(ValheimLegends.AbjurationSkill, zonechargeSkillGain);
			zonechargeSkillGain = 0f;
		}
		else if (VL_Utility.Ability2_Input_Down)
		{
			if (player.IsBlocking() && !player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.GetSEMan().HaveStatusEffect("SE_VL_FlameWeapon".GetStableHashCode())) 
				{
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = 0.1f;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_FlameWeapon".GetStableHashCode());
					player.GetSEMan().RemoveStatusEffect(statusEffect, quiet: true);
					StatusEffect statusEffect2 = (SE_IceWeapon)ScriptableObject.CreateInstance(typeof(SE_IceWeapon));
					Object.Instantiate(ZNetScene.instance.GetPrefab("fx_Potion_frostresist"), player.GetCenterPoint(), UnityEngine.Quaternion.identity); 
					player.GetSEMan().AddStatusEffect(statusEffect2);
				}
				else if (player.GetSEMan().HaveStatusEffect("SE_VL_IceWeapon".GetStableHashCode()))
				{
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = 0.1f;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_IceWeapon".GetStableHashCode());
					player.GetSEMan().RemoveStatusEffect(statusEffect, quiet: true);
					StatusEffect statusEffect2 = (SE_ThunderWeapon)ScriptableObject.CreateInstance(typeof(SE_ThunderWeapon));
					Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					player.GetSEMan().AddStatusEffect(statusEffect2);
				}
				else if (player.GetSEMan().HaveStatusEffect("SE_VL_ThunderWeapon".GetStableHashCode()))
				{
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = 0.1f;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_ThunderWeapon".GetStableHashCode());
					player.GetSEMan().RemoveStatusEffect(statusEffect, quiet: true);
				}
				else 
				{
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = 0.1f;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					StatusEffect statusEffect2 = (SE_FlameWeapon)ScriptableObject.CreateInstance(typeof(SE_FlameWeapon));
					Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_health_medium"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					player.GetSEMan().AddStatusEffect(statusEffect2);
				}
			}
			else if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetCharmCost)
				{
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = VL_Utility.GetCharmCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					player.UseStamina(VL_Utility.GetCharmCost);
					StatusEffect statusEffect2 = (SE_Charmcontrol)ScriptableObject.CreateInstance(typeof(SE_Charmcontrol));
					statusEffect2.m_ttl = 30f;
					player.GetSEMan().AddStatusEffect(statusEffect2, true);
					ValheimLegends.isChargingDash = true;
					ValheimLegends.dashCounter = 0;
					QueuedAttack = EnchanterAttackType.Charm;
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("knife_stab0");
					VL_Utility.RotatePlayerToTarget(player);
					player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetCharmSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Charm: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetCharmCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability1_Input_Down)
		{
			if (player.IsBlocking() && !player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
			{
				if (player.GetSEMan().HaveStatusEffect("SE_VL_FlameArmor".GetStableHashCode()))
				{
					StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect3.m_ttl = 0.1f;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_FlameArmor".GetStableHashCode());
					player.GetSEMan().RemoveStatusEffect(statusEffect, quiet: true);
					StatusEffect statusEffect2 = (SE_IceArmor)ScriptableObject.CreateInstance(typeof(SE_IceArmor));
					ValheimLegends.shouldUseGuardianPower = false;
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_guardstone_activate"), player.transform.position, UnityEngine.Quaternion.identity);
					player.GetSEMan().AddStatusEffect(statusEffect2);
				}
				else if (player.GetSEMan().HaveStatusEffect("SE_VL_IceArmor".GetStableHashCode()))
				{
					StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect3.m_ttl = 0.1f;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_IceArmor".GetStableHashCode());
					player.GetSEMan().RemoveStatusEffect(statusEffect, quiet: true);
					StatusEffect statusEffect2 = (SE_ThunderArmor)ScriptableObject.CreateInstance(typeof(SE_ThunderArmor));
					ValheimLegends.shouldUseGuardianPower = false;
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightburst"), player.GetEyePoint(), UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Shock"), player.GetEyePoint() + player.GetLookDir() * 2.5f + player.transform.right * 0.25f, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
					player.GetSEMan().AddStatusEffect(statusEffect2);
				}
				else if (player.GetSEMan().HaveStatusEffect("SE_VL_ThunderArmor".GetStableHashCode()))
				{
					StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect3.m_ttl = 0.1f;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_ThunderArmor".GetStableHashCode());
					player.GetSEMan().RemoveStatusEffect(statusEffect, quiet: true);
				}
				else
				{
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = 0.1f;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					StatusEffect statusEffect2 = (SE_FlameArmor)ScriptableObject.CreateInstance(typeof(SE_FlameArmor));
					ValheimLegends.shouldUseGuardianPower = false;
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Flames"), player.transform.position, UnityEngine.Quaternion.identity);
					player.GetSEMan().AddStatusEffect(statusEffect2);
				}
			}
			else if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()) && player.GetStamina() >= VL_Utility.GetWeakenCost)
			{
				ValheimLegends.shouldUseGuardianPower = false;
				float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
					.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
				StatusEffect statusEffect4 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
				statusEffect4.m_ttl = VL_Utility.GetWeakenCooldownTime;
				player.GetSEMan().AddStatusEffect(statusEffect4);
				player.UseStamina(VL_Utility.GetWeakenCost + 0.5f * level3);
				((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
				RaycastHit hitInfo = default(RaycastHit);
				UnityEngine.Vector3 position = player.transform.position;
				UnityEngine.Vector3 vector = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
				Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Weaken"), vector, UnityEngine.Quaternion.identity);
				for (int i = 0; i < 4; i++)
				{
					Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_WeakenStatus"), player.transform.position + player.transform.up * Random.Range(0.4f, 1.1f), UnityEngine.Quaternion.identity);
				}
				SE_Weaken sE_Weaken = (SE_Weaken)ScriptableObject.CreateInstance(typeof(SE_Weaken));
				sE_Weaken.m_ttl = SE_Weaken.m_baseTTL;
				sE_Weaken.damageReduction = (0.15f + 0.0015f * level3 * (1f + (EpicMMOSystem.LevelSystem.Instance.getLevel() / 60f))) * VL_GlobalConfigs.c_enchanterWeaken;
				sE_Weaken.speedReduction = 0.8f - 0.001f * level3 * (1f + (EpicMMOSystem.LevelSystem.Instance.getLevel() / 40f));
				sE_Weaken.staminaDrain = (0.1f + 0.001f * level3) * VL_GlobalConfigs.c_enchanterWeaken * (1f + (EpicMMOSystem.LevelSystem.Instance.getLevel() / 40f));
				List<Character> allCharacters = Character.GetAllCharacters();
				foreach (Character item9 in allCharacters)
				{
					if (BaseAI.IsEnemy(player, item9) && (item9.transform.position - vector).magnitude <= 5f + 0.01f * level3)
					{
						if (item9.IsPlayer())
						{
							item9.GetSEMan().AddStatusEffect(sE_Weaken.name.GetStableHashCode(), resetTime: true);
						}
						else
						{
							item9.GetSEMan().AddStatusEffect(sE_Weaken, resetTime: true);
						}
					}
				}
				player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetWeakenSkillGain);
			}
			else if (player.GetStamina() < VL_Utility.GetWeakenCost)
			{
				player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Weaken: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetWeakenCost + ")");
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
