using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Druid
{
	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost");

	private static GameObject GO_CastFX;

	private static GameObject GO_Root;

	private static Projectile P_Root;

	private static StatusEffect SE_Root;

	private static GameObject GO_RootDefender;

	private static int rootCount;

	private static int rootTotal;

	private static int rootCountTrigger;

	public static void Process_Input(Player player, float altitude)
	{
		System.Random random = new System.Random();
		UnityEngine.Vector3 vector = default(Vector3);
		if (VL_Utility.Ability3_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				ValheimLegends.shouldUseGuardianPower = false;
				if (player.GetStamina() >= VL_Utility.GetRootCost && !ValheimLegends.isChanneling)
				{
					ValheimLegends.isChanneling = true;
					StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
					statusEffect.m_ttl = VL_Utility.GetRootCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect);
					player.UseStamina(VL_Utility.GetRootCost);
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("gpower");
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetSpeed(0.3f);
					float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.ConjurationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					rootCount = 0;
					rootCountTrigger = 32 - Mathf.RoundToInt(0.12f * level);
					rootTotal = 0;
					UnityEngine.Vector3 vector2 = player.transform.right * 2.5f;
					if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
					{
						vector2 *= -1f;
					}
					vector = player.transform.position + player.transform.up * 3f + player.GetLookDir() * 2f + vector2;
					GameObject prefab = ZNetScene.instance.GetPrefab("gdking_root_projectile");
					GO_Root = UnityEngine.Object.Instantiate(prefab, new UnityEngine.Vector3(vector.x, vector.y, vector.z), UnityEngine.Quaternion.identity);
					P_Root = GO_Root.GetComponent<Projectile>();
					P_Root.name = "Root";
					P_Root.m_respawnItemOnHit = false;
					P_Root.m_spawnOnHit = null;
					P_Root.m_ttl = 35f;
					P_Root.m_gravity = 0f;
					P_Root.m_rayRadius = 0.1f;
					Traverse.Create(P_Root).Field("m_skill").SetValue(ValheimLegends.ConjurationSkill);
					P_Root.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetLookDir());
					GO_Root.transform.localScale = UnityEngine.Vector3.one * 1.5f;
					player.RaiseSkill(ValheimLegends.ConjurationSkill, VL_Utility.GetRootSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to channel Root: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetRootCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability3_Input_Pressed && player.GetStamina() > VL_Utility.GetRootCostPerUpdate && ValheimLegends.isChanneling && Mathf.Max(0f, altitude - player.transform.position.y) <= 2f)
		{
			rootCount++;
			VL_Utility.SetTimer();
			player.UseStamina(VL_Utility.GetRootCostPerUpdate * rootTotal);
			ValheimLegends.isChanneling = true;
			if (rootCount < rootCountTrigger)
			{
				return;
			}
			player.RaiseSkill(ValheimLegends.ConjurationSkill, 0.06f);
			float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.ConjurationSkillDef)
				.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
			rootCount = 0;
			if (GO_Root != null && GO_Root.transform != null)
			{
				RaycastHit hitInfo = default(RaycastHit);
				UnityEngine.Vector3 position = player.transform.position;
				UnityEngine.Vector3 target = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, float.PositiveInfinity, Script_Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
				HitData hitData = new HitData();
				hitData.m_damage.m_pierce = UnityEngine.Random.Range(10f + 0.6f * level2, 15f + 1.2f * level2) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_druidVines;
				hitData.m_pushForce = 2f;
				rootTotal++;
				UnityEngine.Vector3 vector3 = UnityEngine.Vector3.MoveTowards(GO_Root.transform.position, target, 1f);
				if (P_Root != null && P_Root.name == "Root")
				{
					P_Root.Setup(player, (vector3 - GO_Root.transform.position) * 75f, -1f, hitData, null, null);
					Traverse.Create(P_Root).Field("m_skill").SetValue(ValheimLegends.ConjurationSkill);
				}
			}
			GO_Root = null;
			UnityEngine.Vector3 vector4 = player.transform.right * 2.5f;
			if (UnityEngine.Random.Range(0f, 1f) < 0.5f)
			{
				vector4 *= -1f;
			}
			vector = player.transform.position + player.transform.up * 3f + player.GetLookDir() * 2f + vector4;
			GameObject prefab2 = ZNetScene.instance.GetPrefab("gdking_root_projectile");
			GO_Root = UnityEngine.Object.Instantiate(prefab2, new UnityEngine.Vector3(vector.x, vector.y, vector.z), UnityEngine.Quaternion.identity);
			P_Root = GO_Root.GetComponent<Projectile>();
			P_Root.name = "Root";
			P_Root.m_respawnItemOnHit = false;
			P_Root.m_spawnOnHit = null;
			P_Root.m_ttl = rootCountTrigger + 1;
			P_Root.m_gravity = 0f;
			P_Root.m_rayRadius = 0.1f;
			Traverse.Create(P_Root).Field("m_skill").SetValue(ValheimLegends.ConjurationSkill);
			P_Root.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetLookDir());
			GO_Root.transform.localScale = UnityEngine.Vector3.one * 1.5f;
		}
		else if (((VL_Utility.Ability3_Input_Up || player.GetStamina() <= VL_Utility.GetRootCostPerUpdate) && ValheimLegends.isChanneling) || Mathf.Max(0f, altitude - player.transform.position.y) > 2f)
		{
			if (GO_Root != null && GO_Root.transform != null)
			{
				RaycastHit hitInfo2 = default(RaycastHit);
				UnityEngine.Vector3 position2 = player.transform.position;
				UnityEngine.Vector3 target2 = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo2, float.PositiveInfinity, Script_Layermask) || !hitInfo2.collider) ? (position2 + player.GetLookDir() * 1000f) : hitInfo2.point);
				HitData hitData2 = new HitData();
				hitData2.m_damage.m_pierce = 10f;
				hitData2.m_pushForce = 10f;
				hitData2.SetAttacker(player);
				UnityEngine.Vector3 vector5 = UnityEngine.Vector3.MoveTowards(GO_Root.transform.position, target2, 1f);
				P_Root.Setup(player, (vector5 - GO_Root.transform.position) * 65f, -1f, hitData2, null, null);
				Traverse.Create(P_Root).Field("m_skill").SetValue(ValheimLegends.ConjurationSkill);
			}
			rootTotal = 0;
			GO_Root = null;
			ValheimLegends.isChanneling = false;
		}
		else if (VL_Utility.Ability2_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetDefenderCost)
				{
					UnityEngine.Vector3 lookDir = player.GetLookDir();
					lookDir.y = 0f;
					player.transform.rotation = UnityEngine.Quaternion.LookRotation(lookDir);
					ValheimLegends.shouldUseGuardianPower = false;
					StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect2.m_ttl = VL_Utility.GetDefenderCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect2);
					player.UseStamina(VL_Utility.GetDefenderCost);
					float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.ConjurationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("gpower");
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), player.transform.position, UnityEngine.Quaternion.identity);
					GameObject prefab3 = ZNetScene.instance.GetPrefab("TentaRoot");
					CharacterTimedDestruction component = prefab3.GetComponent<CharacterTimedDestruction>();
					if (component != null)
					{
						component.m_timeoutMin = 24f + 0.3f * level3;
						component.m_timeoutMax = component.m_timeoutMin;
						component.m_triggerOnAwake = true;
						component.enabled = true;
					}
					List<Vector3> list = new List<Vector3>();
					list.Clear();
					vector = player.transform.position + player.GetLookDir() * 5f + player.transform.right * 5f;
					list.Add(vector);
					vector = player.transform.position + player.GetLookDir() * 5f + player.transform.right * 5f * -1f;
					list.Add(vector);
					vector = player.transform.position + player.GetLookDir() * 5f * -1f;
					list.Add(vector);
					for (int i = 0; i < list.Count; i++)
					{
						GO_RootDefender = UnityEngine.Object.Instantiate(prefab3, list[i], UnityEngine.Quaternion.identity);
						Character component2 = GO_RootDefender.GetComponent<Character>();
						if (component2 != null)
						{
							SE_RootsBuff sE_RootsBuff = (SE_RootsBuff)ScriptableObject.CreateInstance(typeof(SE_RootsBuff));
							sE_RootsBuff.m_ttl = SE_RootsBuff.m_baseTTL;
							sE_RootsBuff.damageModifier = 0.5f + 0.015f * level3 * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_druidDefenders;
							sE_RootsBuff.staminaRegen = 0.5f + 0.05f * level3;
							sE_RootsBuff.summoner = player;
							sE_RootsBuff.centerPoint = player.transform.position;
							component2.GetSEMan().AddStatusEffect(sE_RootsBuff);
							component2.SetMaxHealth(30f + 6f * level3);
							component2.transform.localScale = (0.75f + 0.005f * level3) * UnityEngine.Vector3.one;
							component2.m_faction = Character.Faction.Players;
							component2.SetTamed(tamed: true);
						}
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), component2.transform.position, UnityEngine.Quaternion.identity);
					}
					GameObject prefab4 = ZNetScene.instance.GetPrefab("VL_Deathsquit");
					CharacterTimedDestruction component3 = prefab4.GetComponent<CharacterTimedDestruction>();
					if (component3 != null)
					{
						component3.m_timeoutMin = 24f + 0.3f * level3;
						component3.m_timeoutMax = component3.m_timeoutMin;
						component3.m_triggerOnAwake = true;
						component3.enabled = true;
					}
					int num = 2 + Mathf.RoundToInt(0.05f * level3);
					for (int j = 0; j < num; j++)
					{
						vector = player.transform.position + player.transform.up * 4f + (player.GetLookDir() * UnityEngine.Random.Range(0f - (5f + 0.1f * level3), 5f + 0.1f * level3) + player.transform.right * UnityEngine.Random.Range(0f - (5f + 0.1f * level3), 5f + 0.1f * level3));
						GameObject gameObject = UnityEngine.Object.Instantiate(prefab4, vector, UnityEngine.Quaternion.identity);
						Character component4 = gameObject.GetComponent<Character>();
						component4.m_name = "Drusquito";
						if (component4 != null)
						{
							SE_Companion sE_Companion = (SE_Companion)ScriptableObject.CreateInstance(typeof(SE_Companion));
							sE_Companion.m_ttl = 60f;
							sE_Companion.damageModifier = 0.05f + 0.0075f * level3 * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_druidDefenders;
							sE_Companion.summoner = player;
							component4.GetSEMan().AddStatusEffect(sE_Companion);
							component4.transform.localScale = (0.4f + 0.005f * level3) * UnityEngine.Vector3.one;
							component4.m_faction = Character.Faction.Players;
							component4.SetTamed(tamed: true);
						}
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_float_hitwater"), component4.transform.position, UnityEngine.Quaternion.identity);
					}
					player.RaiseSkill(ValheimLegends.ConjurationSkill, VL_Utility.GetDefenderSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to summon root defenders: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetDefenderCost + ")");
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
				if (player.IsBlocking())
				{
					if (player.GetInventory() != null)
					{
						ItemDrop.ItemData item = null;
						ItemDrop.ItemData resurrectMaterialItem = null;
						ItemDrop resurrectFinalItem = null;

						foreach (ItemDrop go in Resources.FindObjectsOfTypeAll(typeof(ItemDrop)) as ItemDrop[])
						{
							//Debug.Log("Item Scanning: " + go.m_itemData.m_shared.m_name.ToLower());
							if (go.m_itemData.m_shared.m_name.ToLower().Contains("spirit binding vial"))
							{
								resurrectFinalItem = go;
								break;
							}
						}
						//Debug.Log("Item Found: " + resurrectFinalItem.m_itemData.m_shared.m_name);
						// Go through all items.
						for (int j = 0; j < player.GetInventory().GetHeight(); j++)
						{
							if (resurrectMaterialItem != null)
							{
								break;
							}
							for (int i = 0; i < player.GetInventory().GetWidth(); i++)
							{
								item = player.GetInventory().GetItemAt(i, j);
								if (item == null)
								{
									continue;
								}

								if (item.m_shared.m_name == "$item_ancientseed")
								{
									resurrectMaterialItem = item;
								}
								if (resurrectMaterialItem != null) break;
							}
						}
						if (resurrectMaterialItem == null)
						{
							player.Message(MessageHud.MessageType.TopLeft, "You need an Ancient Seed to create a Spirit Binding Vial.");
						}
						else
						{
							if (player.GetStamina() >= (VL_Utility.GetRegenerationCost))
							{
								player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetRegenerationSkillGain);
								StatusEffect statusEffect = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
								float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
									.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
								statusEffect.m_ttl = VL_Utility.GetHealCooldownTime * 20f / (1f + level / 150f);
								player.GetSEMan().AddStatusEffect(statusEffect);
								player.UseStamina(VL_Utility.GetRegenerationCost);
								Player.m_localPlayer?.GetInventory().RemoveOneItem(resurrectMaterialItem);
								ItemDrop.DropItem(resurrectFinalItem.m_itemData, 1, player.transform.position, UnityEngine.Quaternion.identity);
								player.StartEmote("cheer");
								player.Message(MessageHud.MessageType.TopLeft, "Consumed 1 " + resurrectMaterialItem.m_shared.m_name + " to create a " + resurrectFinalItem.m_itemData.m_shared.m_name + ".");
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), player.transform.position, UnityEngine.Quaternion.identity);
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), player.transform.position, UnityEngine.Quaternion.identity);
							}
							else
							{
								player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to Create Spirit Binding Vial: (" + player.GetStamina().ToString("#.#") + "/" + (VL_Utility.GetHealCost).ToString("#.#") + ")");
							}
						}
					}
				}
				else
				{
					if (player.GetStamina() >= VL_Utility.GetRegenerationCost)
					{
						StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
						statusEffect3.m_ttl = VL_Utility.GetRegenerationCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect3);
						player.UseStamina(VL_Utility.GetRegenerationCost);
						float level4 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
						player.StartEmote("cheer");
						GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_guardstone_permitted_add"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
						GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), player.transform.position, UnityEngine.Quaternion.identity);
						SE_Regeneration sE_Regeneration = (SE_Regeneration)ScriptableObject.CreateInstance(typeof(SE_Regeneration));
						sE_Regeneration.m_ttl = SE_Regeneration.m_baseTTL;
						sE_Regeneration.m_icon = ZNetScene.instance.GetPrefab("TrophyGreydwarfShaman").GetComponent<ItemDrop>().m_itemData.GetIcon();
						sE_Regeneration.m_HealAmount = 0.5f + 0.4f * level4 * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_druidRegen;
						sE_Regeneration.doOnce = false;
						List<Character> list2 = new List<Character>();
						list2.Clear();
						Character.GetCharactersInRange(player.GetCenterPoint(), 30f + 0.2f * level4, list2);
						foreach (Character item in list2)
						{
							if (!BaseAI.IsEnemy(player, item))
							{
								if (item == Player.m_localPlayer)
								{
									item.GetSEMan().AddStatusEffect(sE_Regeneration, resetTime: true);
								}
								else if (item.IsPlayer())
								{
									item.GetSEMan().AddStatusEffect(sE_Regeneration.name.GetStableHashCode(), resetTime: true);
								}
								else
								{
									item.GetSEMan().AddStatusEffect(sE_Regeneration, resetTime: true);
								}
							}
						}
						player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetRegenerationSkillGain);
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to for Regeneration: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetRegenerationCost + ")");
					}
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
