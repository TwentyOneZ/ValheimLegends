using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Ranger
{
	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

	private static GameObject GO_CastFX;

	public static GameObject GO_Wolf;

	public static void Process_Input(Player player)
	{
		System.Random random = new System.Random();
		UnityEngine.Vector3 vector = default(Vector3);
		if (VL_Utility.Ability3_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				if (player.IsBlocking())
                {
					if (player.GetInventory() != null)
					{
						ItemDrop.ItemData item = null;
						ItemDrop.ItemData arrowMaterialItem = null;
						ItemDrop arrowFinalItem = null;

						foreach (ItemDrop go in Resources.FindObjectsOfTypeAll(typeof(ItemDrop)) as ItemDrop[])
						{
							//Debug.Log("Item Scanning: " + go.m_itemData.m_shared.m_name.ToLower());
							if (go.m_itemData.m_shared.m_name.ToLower().Contains("$item_arrow_wood"))
							{
								arrowFinalItem = go;
								break;
							}
						}
						//Debug.Log("Item Found: " + arrowFinalItem.m_itemData.m_shared.m_name);
						// Go through all items.
						for (int j = 0; j < player.GetInventory().GetHeight(); j++)
						{
							if (arrowMaterialItem != null)
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

								if (item.m_shared.m_name == "$item_wood")
								{
									//Debug.Log("Stack of Wood: " + player.GetInventory().GetItemAt(i, j).m_stack); 
									if (player.GetInventory().GetItemAt(i, j).m_stack >= 8)
										arrowMaterialItem = item;
								}
								if (arrowMaterialItem != null) break;
							}
						}
						if (arrowMaterialItem == null)
						{
							player.Message(MessageHud.MessageType.TopLeft, "You need 8 Wood to create 20 Wooden Arrows.");
						}
						else
						{
							StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
							statusEffect.m_ttl = 0.5f;
							player.GetSEMan().AddStatusEffect(statusEffect);
							for (int i = 0; i < 8; i++)
							{
								Player.m_localPlayer?.GetInventory().RemoveOneItem(arrowMaterialItem);
							}
							ItemDrop.DropItem(arrowFinalItem.m_itemData, 20, player.transform.position, UnityEngine.Quaternion.identity);
							player.StartEmote("cheer");
							player.Message(MessageHud.MessageType.TopLeft, "Consumed 8 " + arrowMaterialItem.m_shared.m_name + " to create 20 " + arrowFinalItem.m_itemData.m_shared.m_name + ".");
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), player.transform.position, UnityEngine.Quaternion.identity);
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), player.transform.position, UnityEngine.Quaternion.identity);
						}
					}
				}	
				else
                {
					if (player.GetStamina() >= VL_Utility.GetPowerShotCost(player))
					{
						StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
						statusEffect.m_ttl = VL_Utility.GetPowerShotCooldown(player);
						player.GetSEMan().AddStatusEffect(statusEffect);
						player.UseStamina(VL_Utility.GetPowerShotCost(player));
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_crit"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
						SE_PowerShot sE_PowerShot = (SE_PowerShot)ScriptableObject.CreateInstance(typeof(SE_PowerShot));
						sE_PowerShot.m_ttl = SE_PowerShot.m_baseTTL + (float)Mathf.RoundToInt(0.05f * player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f)));
						sE_PowerShot.hitCount = Mathf.RoundToInt(3f + 0.05f * player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f)));
						if (player.GetSEMan().HaveStatusEffect("SE_VL_PowerShot".GetStableHashCode()))
						{
							StatusEffect statusEffect2 = player.GetSEMan().GetStatusEffect("SE_VL_PowerShot".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(statusEffect2);
						}
						player.GetSEMan().AddStatusEffect(sE_PowerShot);
						player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetPowerShotSkillGain(player));
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Power Shot: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetPowerShotCost(player) + ")");
					}
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
				if (player.GetStamina() >= VL_Utility.GetSummonWolfCost(player))
				{
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = VL_Utility.GetSummonWolfCooldown(player);
					player.GetSEMan().AddStatusEffect(statusEffect3);
					float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.ConjurationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					player.UseStamina(VL_Utility.GetSummonWolfCost(player));
					player.StartEmote("cheer");
					vector = player.transform.position + player.transform.forward * 4f;
					GameObject prefab = ZNetScene.instance.GetPrefab("VL_ShadowWolf");
					prefab.GetComponent<CharacterTimedDestruction>().m_timeoutMin = 600f;
					prefab.GetComponent<CharacterTimedDestruction>().m_timeoutMin = 600f;
					GO_Wolf = UnityEngine.Object.Instantiate(prefab, vector, UnityEngine.Quaternion.identity);
					Character component = GO_Wolf.GetComponent<Character>();
					component.m_name = "Shadow Wolf";
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), component.transform.position, UnityEngine.Quaternion.identity);
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), component.transform.position, UnityEngine.Quaternion.identity);
					if (component != null)
					{
						component.m_faction = Character.Faction.Players;
						component.SetTamed(tamed: true);
						component.SetMaxHealth(80f + 9f * level);
						component.transform.localScale = (0.5f + 0.015f * level) * UnityEngine.Vector3.one;
						component.m_swimSpeed *= 2f;
						CharacterTimedDestruction component2 = GO_Wolf.GetComponent<CharacterTimedDestruction>();
						if (component2 != null)
						{
							component2.m_timeoutMin = 600f;
							component2.m_timeoutMax = component2.m_timeoutMin;
							component2.Trigger();
						}
						SE_Companion sE_Companion = (SE_Companion)ScriptableObject.CreateInstance(typeof(SE_Companion));
						sE_Companion.m_ttl = SE_Companion.m_baseTTL;
						sE_Companion.damageModifier = (1f + EpicMMOSystem.LevelSystem.Instance.getLevel() / 30f) * (0.05f + 0.01f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_rangerShadowWolf;
						sE_Companion.healthRegen = 1f + 0.1f * level;
						sE_Companion.speedModifier = 1.2f;
						sE_Companion.summoner = player;
						MonsterAI monsterAI = component.GetBaseAI() as MonsterAI;
						monsterAI.SetFollowTarget(player.gameObject);
						component.GetSEMan().AddStatusEffect(sE_Companion);
					}
					player.RaiseSkill(ValheimLegends.ConjurationSkill, VL_Utility.GetSummonWolfSkillGain(player));
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to Summon Wolf: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetSummonWolfCost(player) + ")");
				}
			}
			else
			{
				if (player.IsBlocking())
				{
					string companionName = "vl_shadowwolf";
					bool noOneToDismiss = true;

					// Check for wolves nearby.
					List<Character> list = new List<Character>();
					list.Clear();
					Character.GetCharactersInRange(player.transform.position, 25f, list);
					foreach (Character characterInArea in list)
					{
						if (characterInArea.gameObject.name.ToLower().Contains(companionName))
						{
							if (characterInArea.GetSEMan().HaveStatusEffect("SE_VL_Companion".GetStableHashCode()))
							{
								SE_Ability2_CD sE_Ability2_CD = Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()) as SE_Ability2_CD;
								float new_mTTL = Mathf.Min(sE_Ability2_CD.m_ttl, Mathf.Sqrt(sE_Ability2_CD.m_ttl / characterInArea.GetHealthPercentage()));
								player.GetSEMan().RemoveStatusEffect(sE_Ability2_CD);
								StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
								statusEffect3.m_ttl = new_mTTL;
								player.GetSEMan().AddStatusEffect(statusEffect3);
								SE_Companion sE_Companion = characterInArea.GetSEMan().GetStatusEffect("SE_VL_Companion".GetStableHashCode()) as SE_Companion;
								if (sE_Companion.summoner == Player.m_localPlayer)
								{
									MonsterAI component = characterInArea.GetComponent<MonsterAI>();
									if (component != null)
									{
										component.SetFollowTarget(null);
									}
									characterInArea.m_faction = Character.Faction.MountainMonsters;
									HitData hitData = new HitData();
									hitData.m_damage.m_slash = 9999f;
									characterInArea.Damage(hitData);
								}
								break;
							}
						}
					}
					if (noOneToDismiss == true)
					{
						player.Message(MessageHud.MessageType.TopLeft, "No Shadow Wolf to dismiss.");
					}

				}
				else
				{
					if (player.GetInventory() != null)
					{
						ItemDrop.ItemData item = null;
						ItemDrop.ItemData wolfFoodItem = null;
						float healingPower = new();
						string companionName = "vl_shadowwolf";
						bool noOneToHeal = true;

						// Go through all items.
						for (int j = 0; j < player.GetInventory().GetHeight(); j++)
						{
							if (wolfFoodItem != null)
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

								switch (item.m_shared.m_name)
								{
									case "$item_necktail":
										wolfFoodItem = item;
										healingPower = 0.10f;
										break;
									case "$item_boar_meat":
										wolfFoodItem = item;
										healingPower = 0.15f;
										break;
									case "$item_deer_meat":
										wolfFoodItem = item;
										healingPower = 0.20f;
										break;
									case "$item_fish_raw":
										wolfFoodItem = item;
										healingPower = 0.20f;
										break;
									case "$item_sausages":
										wolfFoodItem = item;
										healingPower = 0.25f;
										break;
									case "$item_loxmeat":
										wolfFoodItem = item;
										healingPower = 0.25f;
										break;
								}
								if (wolfFoodItem != null) break;
							}
						}
						if (wolfFoodItem == null)
						{
							player.Message(MessageHud.MessageType.TopLeft, "Not enough wolf food in inventory to Heal Wolf.");
						}
						else
						{
							// Food defined and assured it is available in inventory. Time to check for wolves to heal.
							List<Character> list = new List<Character>();
							list.Clear();
							Character.GetCharactersInRange(player.transform.position, 25f, list);
							foreach (Character characterInArea in list)
							{
								if (characterInArea.gameObject.name.ToLower().Contains(companionName))
								{
									if (characterInArea.GetHealthPercentage().Equals(1f))
									{
										continue;
									}
									// Found a wolf to heal! Time to check if trainer has stamina.
									noOneToHeal = false;
									if (player.GetStamina() >= (VL_Utility.GetSummonWolfCost(player) * (2 * healingPower)))
									{
										player.UseStamina(VL_Utility.GetSummonWolfCost(player) * (2 * healingPower));
										Player.m_localPlayer?.GetInventory().RemoveOneItem(wolfFoodItem);
										player.StartEmote("cheer");
										player.Message(MessageHud.MessageType.TopLeft, "Consumed 1 " + wolfFoodItem.m_shared.m_name + " to heal your companion by " + (characterInArea.GetMaxHealth() * healingPower).ToString("#") + " health.");
										characterInArea.Heal(Mathf.Max(characterInArea.GetMaxHealth() * healingPower, healingPower * 10f));
										float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.ConjurationSkillDef)
											.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
										SE_Regeneration sE_Regeneration = (SE_Regeneration)ScriptableObject.CreateInstance(typeof(SE_Regeneration));
										sE_Regeneration.m_ttl = SE_Regeneration.m_baseTTL * (1f + level / 300f);
										sE_Regeneration.m_HealAmount = 0.5f + level * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_druidRegen;
										sE_Regeneration.doOnce = false;
										characterInArea.GetSEMan().AddStatusEffect(sE_Regeneration, resetTime: true);
										UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), characterInArea.transform.position, UnityEngine.Quaternion.identity);
										UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), characterInArea.transform.position, UnityEngine.Quaternion.identity);
										player.RaiseSkill(global::ValheimLegends.ValheimLegends.ConjurationSkill, VL_Utility.GetSummonWolfSkillGain(player) * (healingPower));
									}
									else
									{
										player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to Heal Wolf: (" + player.GetStamina().ToString("#.#") + "/" + (VL_Utility.GetSummonWolfCost(player) * (4 * healingPower)) + ")");
									}
								}
							}
							if (noOneToHeal == true)
							{
								player.Message(MessageHud.MessageType.TopLeft, "No wounded Shadow Wolf nearby.");
							}
						}
						//player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
					}
				}
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
				if (player.GetStamina() >= VL_Utility.GetShadowStalkCost(player))
				{
					float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
					StatusEffect statusEffect4 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect4.m_ttl = VL_Utility.GetShadowStalkCooldown(player);
					player.GetSEMan().AddStatusEffect(statusEffect4);
					player.UseStamina(VL_Utility.GetShadowStalkCost(player));
					GameObject prefab2 = ZNetScene.instance.GetPrefab("vfx_odin_despawn");
					UnityEngine.Object.Instantiate(prefab2, player.transform.position, UnityEngine.Quaternion.identity);
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_wraith_death"), player.transform.position, UnityEngine.Quaternion.identity);
					SE_ShadowStalk sE_ShadowStalk = (SE_ShadowStalk)ScriptableObject.CreateInstance(typeof(SE_ShadowStalk));
					sE_ShadowStalk.m_ttl = SE_ShadowStalk.m_baseTTL * (1f + 0.02f * level2);
					sE_ShadowStalk.speedAmount = 1.5f + 0.01f * level2 * VL_GlobalConfigs.c_rangerShadowStalk;
					sE_ShadowStalk.speedDuration = 3f + 0.03f * level2;
					player.GetSEMan().AddStatusEffect(sE_ShadowStalk);
					List<Character> list = new List<Character>();
					list.Clear();
					Character.GetCharactersInRange(player.GetCenterPoint(), 500f, list);
					foreach (Character item in list)
					{
						if (item.GetBaseAI() != null && item.GetBaseAI() is MonsterAI && item.GetBaseAI().IsEnemy(player))
						{
							MonsterAI monsterAI2 = item.GetBaseAI() as MonsterAI;
							if (monsterAI2 != null && monsterAI2.GetTargetCreature() == player)
							{
								Traverse.Create(monsterAI2).Field("m_alerted").SetValue(false);
								Traverse.Create(monsterAI2).Field("m_targetCreature").SetValue(null);
							}
						}
					}
					player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetShadowStalkSkillGain(player));
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Shadow Stalk: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetShadowStalkCost(player) + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
	}
}
