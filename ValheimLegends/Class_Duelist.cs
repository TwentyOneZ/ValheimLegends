using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Duelist
{
	private static int ScriptChar_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost");

	private static GameObject GO_CastFX;

	private static GameObject GO_QuickShot;

	private static Projectile P_QuickShot;

	public static ItemDrop coinsItem;

	public static List<int> challengedDeath = new List<int>();

	public static List<int> challengedMastery = new List<int>();

	public static void Execute_Slash(Player player)
	{
		UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_BlinkStrike"), player.GetCenterPoint() + player.GetLookDir() * 3f, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
		float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
			.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
		float num = (1.25f + (level / 300f)) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_duelistSeismicSlash;
        // Direção horizontal do jogador
        Vector3 forward = player.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        float coneRange = 7f;
        float coneAngle = 90f;
        float halfConeDot = Mathf.Cos(coneAngle * 0.5f * Mathf.Deg2Rad);

        List<Character> targets = new List<Character>();
        Character.GetCharactersInRange(player.transform.position, coneRange, targets);

        foreach (Character item in targets)
        {
            if (!BaseAI.IsEnemy(player, item))
                continue;

            Vector3 toTarget = item.transform.position - player.transform.position;
            toTarget.y = 0f;

            float distance = toTarget.magnitude;
            if (distance > coneRange)
                continue;

            toTarget.Normalize();

            float dot = Vector3.Dot(forward, toTarget);
            if (dot < halfConeDot)
                continue;

            if (!VL_Utility.LOS_IsValid(item, item.GetEyePoint(), player.GetEyePoint()))
                continue;

            // ===== DANO =====
            HitData hitData = new HitData();
            hitData.m_damage = player.GetCurrentWeapon().GetDamage();
            hitData.ApplyModifier(UnityEngine.Random.Range(1.8f, 2.2f) * num);
            hitData.m_pushForce = 25f + 0.1f * level;
            hitData.m_point = item.GetEyePoint();
            hitData.m_dir = forward;
            hitData.m_skill = ValheimLegends.DisciplineSkill;

            item.Damage(hitData);

            // ===== FINALIZA DUEL OF MASTERY =====
            if (Class_Duelist.challengedMastery.Contains(item.GetInstanceID()))
            {
                Class_Duelist.challengedMastery.Remove(item.GetInstanceID());

                int coinsSpoiled = Mathf.CeilToInt(
                    Mathf.Sqrt(item.GetMaxHealth()) +
                    (1f + EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f)
                    * Mathf.Sqrt(item.GetMaxHealth())
                );

                if (player.GetInventory().CanAddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled))
                    player.GetInventory().AddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled);
                else
                    ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, player.transform.position, Quaternion.identity);

                player.Message(
                    MessageHud.MessageType.TopLeft,
                    $"Spoiled {coinsSpoiled:#} coins from {item.GetHoverName()}!"
                );
            }
        }
    }

    public static void Process_Input(Player player, ref Rigidbody playerBody)
	{
		System.Random random = new System.Random();
		if (VL_Utility.Ability3_Input_Down)
		{
			if (challengedDeath == null)
			{
				challengedDeath = new List<int>();
				challengedDeath.Clear();
			}
			if (challengedDeath.Count > 50)
			{
				challengedDeath.Clear();
			}
			if (challengedMastery == null)
			{
				challengedMastery = new List<int>();
				challengedMastery.Clear();
			}
			if (challengedMastery.Count > 50)
			{
				challengedMastery.Clear();
			}
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				if (player.IsBlocking())
				{
					ItemDrop.ItemData hasLeftItem = Traverse.Create(player).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
					if (hasLeftItem == null && player.GetCurrentWeapon() != null && player.GetCurrentWeapon().m_shared.m_itemType != ItemDrop.ItemData.ItemType.TwoHandedWeapon && (player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Swords || player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Knives || player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Axes || player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Spears))
					{
						float maxWeaponRecover = player.GetCurrentWeapon().GetMaxDurability() * (1f - player.GetCurrentWeapon().GetDurabilityPercentage());
						if (maxWeaponRecover > 0f)
						{
							float maxStaminaCost = maxWeaponRecover * 4f * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
							float recoveredWeapon = maxWeaponRecover;
							if (player.GetStamina() < maxStaminaCost)
							{
								recoveredWeapon = player.GetStamina() / (4f * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f)));
							}
							float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
								.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
							StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
							statusEffect.m_ttl = 60f + (recoveredWeapon * 2f) * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f));
							player.GetSEMan().AddStatusEffect(statusEffect);
							player.UseStamina(recoveredWeapon * 10f * (1f - (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f)) * (1f - level/300f));
							player.GetCurrentWeapon().m_durability += recoveredWeapon;
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_BlinkStrike"), player.GetCenterPoint() + player.GetLookDir() * 3f, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
							((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("knife_stab1");
							player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetBlinkStrikeSkillGain);
						}
						else
						{
							player.Message(MessageHud.MessageType.TopLeft, "You don't need to Sharpen you weapon.");
						}
					}
					else
                    {
						player.Message(MessageHud.MessageType.TopLeft, "You can only Sharpen an equipped bladed single one-handed weapon (swords, knives, axes or spears).");
					}
				}
				else
				{ 				
					if (player.GetStamina() >= VL_Utility.GetBlinkStrikeCost)
					{
						StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
						statusEffect.m_ttl = VL_Utility.GetBlinkStrikeCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect);
						player.UseStamina(VL_Utility.GetBlinkStrikeCost);
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("knife_stab1");
						ValheimLegends.isChanneling = true;
						ValheimLegends.isChargingDash = true;
						ValheimLegends.dashCounter = 0;
						player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetBlinkStrikeSkillGain);
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for S. Slash : (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetBlinkStrikeCost + ")");
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
			if (challengedDeath == null)
			{
				challengedDeath = new List<int>();
				challengedDeath.Clear();
			}
			if (challengedDeath.Count > 50)
			{
				challengedDeath.Clear();
			}
			if (challengedMastery == null)
			{
				challengedMastery = new List<int>();
				challengedMastery.Clear();
			}
			if (challengedMastery.Count > 50)
			{
				challengedMastery.Clear();
			}
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetRiposteCost)
				{
					StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect2.m_ttl = VL_Utility.GetRiposteCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect2);
					player.UseStamina(VL_Utility.GetRiposteCost);
					float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
					GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_perfectblock"), player.transform.position, UnityEngine.Quaternion.identity);
					GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_backstab"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					SE_Riposte statusEffect3 = (SE_Riposte)ScriptableObject.CreateInstance(typeof(SE_Riposte));
					statusEffect3.playerBody = playerBody;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetRiposteSkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Riposte: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetRiposteCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability1_Input_Down)
		{
			if (challengedDeath == null)
			{
				challengedDeath = new List<int>();
				challengedDeath.Clear();
			}
			if (challengedDeath.Count > 50)
			{
				challengedDeath.Clear();
			}
			if (challengedMastery == null)
			{
				challengedMastery = new List<int>();
				challengedMastery.Clear();
			}
			if (challengedMastery.Count > 50)
			{
				challengedMastery.Clear();
			}
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
			{
				ValheimLegends.DefineCoins();
				if (ValheimLegends.coinsItem != null)
				{
					if (player.IsBlocking())
                    {
						RaycastHit hitInfo = default(RaycastHit);
						UnityEngine.Vector3 position = player.transform.position;
						Physics.SphereCast(player.GetEyePoint(), 0.2f, player.GetLookDir(), out hitInfo, 200f, ScriptChar_Layermask);
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
                                int targetId = component.GetInstanceID();

                                bool alreadyChallenged =
                                    (Class_Duelist.challengedDeath != null && Class_Duelist.challengedDeath.Contains(targetId)) ||
                                    (Class_Duelist.challengedMastery != null && Class_Duelist.challengedMastery.Contains(targetId));

                                if (alreadyChallenged)
                                {
                                    player.Message(MessageHud.MessageType.TopLeft, component.GetHoverName() + " was already challenged!");
                                    return;
                                }

                                if (Vector3.Distance(player.transform.position, component.transform.position) <= 70f)
								{
									if (player.GetStamina() >= VL_Utility.GetQuickShotCost)
									{
										int coinsSpoiled = Mathf.CeilToInt(Mathf.Sqrt(component.GetMaxHealth()));
										if (player.GetInventory() != null && player.GetInventory().HaveItem(ValheimLegends.coinsItem.m_itemData.m_shared.m_name) && player.GetInventory().CountItems("$item_coins") >= coinsSpoiled)
										{
											ItemDrop.ItemData item = null;
											ItemDrop.ItemData coinsItemData = null;
											for (int j = 0; j < player.GetInventory().GetHeight(); j++)
											{
												if (coinsItemData != null)
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

													if (item.m_shared.m_name == "$item_coins")
													{
														if (player.GetInventory().GetItemAt(i, j).m_stack >= coinsSpoiled)
															coinsItemData = item;
													}
													if (coinsItemData != null) break;
												}
											}
											for (int i = 0; i < coinsSpoiled; i++)
											{
												Player.m_localPlayer?.GetInventory().RemoveOneItem(coinsItemData);
											}
											player.UseStamina(VL_Utility.GetQuickShotCost);
											if (coinsSpoiled < EpicMMOSystem.LevelSystem.Instance.getLevel())
											{
												UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_destroyed"), component.GetCenterPoint(), UnityEngine.Quaternion.identity);
												UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_stack_destroyed"), component.GetCenterPoint(), UnityEngine.Quaternion.identity);
											}
											else
											{
												UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_pile_destroyed"), component.GetCenterPoint(), UnityEngine.Quaternion.identity);
												UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_pile_destroyed"), component.GetCenterPoint(), UnityEngine.Quaternion.identity);
											}
											player.StartEmote("point");
											player.Message(MessageHud.MessageType.TopLeft, "Challenged " + component.GetHoverName() + " to a duel worth of " + coinsSpoiled.ToString("#") + " coins!");
											if (component.IsOwner())
                                            {
												if (UnityEngine.Random.value < 0.33f)
                                                {
													player.Message(MessageHud.MessageType.Center, "Duel of Mastery!", 1, ValheimLegends.RiposteIcon);
													Class_Duelist.challengedMastery.Add(component.GetInstanceID());
												}
												else
                                                {
													player.Message(MessageHud.MessageType.Center, "Duel to the Death!", 1, ZNetScene.instance.GetPrefab("TrophySkeleton").GetComponent<ItemDrop>().m_itemData.GetIcon());
													Class_Duelist.challengedDeath.Add(component.GetInstanceID());
												}
											}
											else
                                            {
												player.Message(MessageHud.MessageType.Center, "Duel of Mastery!", 1, ValheimLegends.RiposteIcon);
												Class_Duelist.challengedMastery.Add(component.GetInstanceID());
											}
											player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetQuickShotSkillGain);
                                            StatusEffect statusEffect4 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
                                            statusEffect4.m_ttl = VL_Utility.GetQuickShotCooldownTime * 3f;
                                            player.GetSEMan().AddStatusEffect(statusEffect4);
                                        }
                                        else
										{
											player.Message(MessageHud.MessageType.TopLeft, "You need " + coinsSpoiled.ToString("#") + " coins to challenge " + component.GetHoverName() + ".");
										}
									}
									else
									{
										player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Challenge: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetBackstabCost + ")");
									}
								}
								else
								{
									player.Message(MessageHud.MessageType.TopLeft, component.GetHoverName() + " is too far away to challenge!");
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
					else
					{
						if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
						{
							if (player.GetStamina() >= VL_Utility.GetQuickShotCost)
							{
								if (player.GetInventory() != null && player.GetInventory().HaveItem(ValheimLegends.coinsItem.m_itemData.m_shared.m_name))
								{
									ItemDrop.ItemData item = null;
									ItemDrop.ItemData coinsItemData = null;
									for (int j = 0; j < player.GetInventory().GetHeight(); j++)
									{
										if (coinsItemData != null)
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

											if (item.m_shared.m_name == "$item_coins")
											{
												//Debug.Log("Stack of Wood: " + player.GetInventory().GetItemAt(i, j).m_stack); 
												if (player.GetInventory().GetItemAt(i, j).m_stack >= 0)
													coinsItemData = item;
											}
											if (coinsItemData != null) break;
										}
									}
									Player.m_localPlayer?.GetInventory().RemoveOneItem(coinsItemData);
									float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
										.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
									StatusEffect statusEffect4 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
									statusEffect4.m_ttl = VL_Utility.GetQuickShotCooldownTime;
									player.GetSEMan().AddStatusEffect(statusEffect4);
									player.UseStamina(VL_Utility.GetQuickShotCost + 0.5f * level2);
									((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("interact");
									GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_smelter_add"), player.transform.position, UnityEngine.Quaternion.identity);
									UnityEngine.Vector3 vector = player.transform.position + player.transform.up * 1.2f + player.GetLookDir() * 0.5f;
									UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_destroyed"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
									GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_QuickShot"), vector, UnityEngine.Quaternion.LookRotation(player.GetLookDir()));
									GameObject prefab = ZNetScene.instance.GetPrefab("Greydwarf_throw_projectile");
									GO_QuickShot = UnityEngine.Object.Instantiate(prefab, vector, UnityEngine.Quaternion.identity);
									P_QuickShot = GO_QuickShot.GetComponent<Projectile>();
									P_QuickShot.name = "QuickShot";
									P_QuickShot.m_respawnItemOnHit = false;
									P_QuickShot.m_spawnOnHit = null;
									P_QuickShot.m_ttl = 10f;
									P_QuickShot.m_gravity = 1.2f;
									P_QuickShot.m_rayRadius = 0.05f;
									P_QuickShot.m_hitNoise = 20f;
									P_QuickShot.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector));
									GO_QuickShot.transform.localScale = new UnityEngine.Vector3(0.6f, 0.6f, 0.6f);
									RaycastHit hitInfo = default(RaycastHit);
									UnityEngine.Vector3 target = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo.collider) ? (player.GetEyePoint() + player.GetLookDir() * 1000f) : hitInfo.point);
									HitData hitData = new HitData();
									hitData.m_damage.m_pierce = UnityEngine.Random.Range(10f + 1f * (level2 + (EpicMMOSystem.LevelSystem.Instance.getLevel())), 30f + 2f * (level2 + (EpicMMOSystem.LevelSystem.Instance.getLevel()))) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_duelistHipShot;
									hitData.m_pushForce = 1f;
									hitData.m_skill = ValheimLegends.DisciplineSkill;
									UnityEngine.Vector3 vector2 = UnityEngine.Vector3.MoveTowards(vector, target, 1f);
									P_QuickShot.Setup(player, (vector2 - GO_QuickShot.transform.position) * 100f, -1f, hitData, null, null);
									Traverse.Create(P_QuickShot).Field("m_skill").SetValue(ValheimLegends.DisciplineSkill);
									GO_QuickShot = null;
									VL_Utility.RotatePlayerToTarget(player);
									player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetQuickShotSkillGain);
								}
								else
								{
									player.Message(MessageHud.MessageType.TopLeft, "You need one coin to shoot.");
								}
							}
                            else
                            {
                                player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to for Coin Shot: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetQuickShotCost + ")");
                            }
                        }
						else
						{
                            player.Message(MessageHud.MessageType.TopLeft, "Ability not ready yet.");
                        }
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
