using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends;

public class Class_Priest
{
	private static int Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost", "Water");

	private static GameObject GO_CastFX;

	private static GameObject GO_Sanctify;

	private static Projectile P_Sanctify;

	private static StatusEffect SE_Fireball;

	private static GameObject GO_Meteor;

	private static Projectile P_Meteor;

	private static StatusEffect SE_Meteor;

	private static bool healCharging = false;

	private static int healCount;

	private static int healChargeAmount;

	private static int healChargeAmountMax;

	private static float healSkillGain = 0f;

	public static void PurgeStatus_NearbyPlayers(Player healer, float radius, List<string> effectNames)
	{
		if (effectNames == null || effectNames.Count <= 0)
		{
			return;
		}
		List<Character> list = new List<Character>();
		list.Clear();
		Character.GetCharactersInRange(healer.transform.position, radius, list);
		foreach (Character item in list)
		{
			if (BaseAI.IsEnemy(item, healer))
			{
				continue;
			}
			foreach (string effectName in effectNames)
			{
				if (item.GetSEMan().HaveStatusEffect(effectName.GetStableHashCode()))
				{
					item.GetSEMan().RemoveStatusEffect(effectName.GetStableHashCode());
					break;
				}
			}
		}
	}

	public static void HealNearbyPlayers(Player healer, float radius, float amount)
	{
		List<Character> list = new List<Character>();
		list.Clear();
		Character.GetCharactersInRange(healer.transform.position, radius, list);
		foreach (Character item in list)
		{
			if (!BaseAI.IsEnemy(item, healer))
			{
				item.Heal(amount);
			}
		}
	}

	internal static readonly Dictionary<string, ItemDrop> OriginalItemDrops;
	public static void Process_Input(Player player, ref float altitude)
	{
		System.Random random = new System.Random();
		if (VL_Utility.Ability3_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetHealCost)
				{
                    if (player.IsBlocking())
                    {
                        var inv = player.GetInventory();
                        if (inv == null) return;

                        int requiredItems = 1;
                        string materialName = "BoneFragments";

                        // 1) checar material
                        ItemDrop.ItemData foundItem = VL_Utility.FindItemByPrefabName(inv, materialName, requiredItems);
                        if (foundItem == null)
                        {
                            player.Message(MessageHud.MessageType.Center,
                                "You need Bone Fragments to create a Spirit Binding Vial.");
                            return;
                        }

                        // 2) checar stamina
                        if (player.GetStamina() < VL_Utility.GetHealCost)
                        {
                            player.Message(MessageHud.MessageType.TopLeft,
                                "Not enough stamina to Create Spirit Binding Vial: (" +
                                player.GetStamina().ToString("#.#") + "/" +
                                VL_Utility.GetHealCost.ToString("#.#") + ")");
                            return;
                        }

                        // 3) cooldown/skill etc (seu código)
                        player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetHealSkillGain);
                        StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
                        float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
                            .m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
                        statusEffect.m_ttl = VL_Utility.GetHealCooldownTime * 20f / (1f + level / 150f);
                        player.GetSEMan().AddStatusEffect(statusEffect);
                        player.UseStamina(VL_Utility.GetHealCost);

                        // 4) consumir 1 Ancient Seed
                        inv.RemoveOneItem(foundItem);

                        // 5) criar o item do prefab e adicionar no inventário
                        const string vialPrefabName = "questitem_wraiths_breath";

                        if (ZNetScene.instance == null)
                        {
                            Debug.Log("ZNetScene not ready.");
                            return;
                        }

                        GameObject vialPrefab = ZNetScene.instance.GetPrefab(vialPrefabName);
                        if (vialPrefab == null)
                        {
                            Debug.Log($"Prefab not found: {vialPrefabName}");
                            return;
                        }

                        ItemDrop vialDrop = vialPrefab.GetComponent<ItemDrop>();
                        if (vialDrop == null)
                        {
                            Debug.Log($"Prefab has no ItemDrop: {vialPrefabName}");
                            return;
                        }

                        ItemDrop.ItemData vialItem = vialDrop.m_itemData.Clone();

                        // adiciona no inventário (retorna false se inventário cheio)
                        bool added = inv.AddItem(vialItem);
                        if (!added)
                        {
                            // fallback: se inventário cheio, dropa no chão
                            ItemDrop.DropItem(vialItem, 1, player.transform.position + player.transform.forward, Quaternion.identity);
                            player.Message(MessageHud.MessageType.TopLeft, "Inventory full. Dropped Spirit Binding Vial on the ground.");
                        }
                        else
                        {
                            player.Message(MessageHud.MessageType.TopLeft,
                                $"Consumed 1 {materialName} to create a {vialItem.m_shared.m_name}.");
                        }

                        player.StartEmote("cheer");
                        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), player.transform.position, Quaternion.identity);
                        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), player.transform.position, Quaternion.identity);
                    }
                    else
                    {
						ValheimLegends.shouldUseGuardianPower = false;
						ValheimLegends.isChanneling = true;
						StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
						statusEffect.m_ttl = VL_Utility.GetHealCooldownTime;
						player.GetSEMan().AddStatusEffect(statusEffect);
						player.UseStamina(VL_Utility.GetHealCost);
						float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_HealPulse"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
						healCharging = true;
						healChargeAmount = 0;
						healChargeAmountMax = 60;
						List<string> list = new List<string>();
						list.Clear();
						list.Add("Burning");
						list.Add("Poison");
						list.Add("Frost");
						list.Add("Wet");
						list.Add("Smoked");
						PurgeStatus_NearbyPlayers(player, 30f + 0.2f * level, list);
						HealNearbyPlayers(player, 30f + 0.2f * level, (10f + level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestHeal);
						player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetHealSkillGain);
					}
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to begin heal : (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetHealCost + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
		else if (VL_Utility.Ability3_Input_Pressed && healCharging && player.GetStamina() > VL_Utility.GetHealCostPerUpdate && Mathf.Max(0f, altitude - player.transform.position.y) <= 1f)
		{
			healChargeAmount++;
			player.UseStamina(VL_Utility.GetHealCostPerUpdate);
			ValheimLegends.isChanneling = true;
			VL_Utility.SetTimer();
			if (healChargeAmount >= healChargeAmountMax)
			{
				healCount++;
				healChargeAmount = 0;
				((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
				UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_HealPulse"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
				float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
					.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
				HealNearbyPlayers(player, 20f + 0.2f * level2, ((float)healCount + level2 * 0.3f) * 2f * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestHeal);
				player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetHealSkillGain * 0.5f);
			}
		}
		else if ((VL_Utility.Ability3_Input_Up || player.GetStamina() <= VL_Utility.GetHealCostPerUpdate || Mathf.Max(0f, altitude - player.transform.position.y) > 1f) && healCharging)
		{
			healCount = 0;
			healChargeAmount = 0;
			healCharging = false;
			ValheimLegends.isChanneling = false;
            ValheimLegends.channelingBlocksMovement = true;
        }
		else if (VL_Utility.Ability2_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
			{
				if (player.GetStamina() >= VL_Utility.GetPurgeCost)
				{
					ValheimLegends.shouldUseGuardianPower = false;
					float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					float level4 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect2.m_ttl = VL_Utility.GetPurgeCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect2);
					player.UseStamina(VL_Utility.GetPurgeCost);
					player.StartEmote("challenge");
					GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Purge"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					HealNearbyPlayers(player, 20f + 0.2f * level4, 0.5f + UnityEngine.Random.Range(0.4f, 0.6f) * level4 * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestPurgeHeal);
					List<Character> list2 = new List<Character>();
					list2.Clear();
					Character.GetCharactersInRange(player.transform.position, 20f + 0.2f * level3, list2);
					foreach (Character item in list2)
					{
						if (BaseAI.IsEnemy(player, item) && VL_Utility.LOS_IsValid(item, player.GetCenterPoint(), player.transform.position))
						{
							UnityEngine.Vector3 vector = item.transform.position - player.transform.position;
							HitData hitData = new HitData();
							hitData.m_damage.m_spirit = UnityEngine.Random.Range(4f + 0.4f * level3, 8f + 0.8f * level3) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestPurgeDamage;
							hitData.m_damage.m_fire = UnityEngine.Random.Range(4f + 0.4f * level3, 8f + 0.8f * level3) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestPurgeDamage;
							hitData.m_pushForce = 0f;
							hitData.m_point = item.GetEyePoint();
							hitData.m_dir = player.transform.position - item.transform.position;
							hitData.m_skill = ValheimLegends.EvocationSkill;
							item.Damage(hitData);
						}
					}
					player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetPurgeSkillGain * 0.5f);
					player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetPurgeSkillGain * 0.5f);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to for Purge: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetPurgeCost + ")");
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
				if (player.GetStamina() >= VL_Utility.GetSanctifyCost)
				{
					StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect3.m_ttl = VL_Utility.GetSanctifyCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect3);
					player.UseStamina(VL_Utility.GetSanctifyCost);
					float level5 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("battleaxe_attack0");
					RaycastHit hitInfo = default(RaycastHit);
					UnityEngine.Vector3 position = player.transform.position;
					UnityEngine.Vector3 vector2 = ((!Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, float.PositiveInfinity, Layermask) || !hitInfo.collider) ? (position + player.GetLookDir() * 1000f) : hitInfo.point);
					UnityEngine.Vector3 position2 = vector2 + player.transform.up * 12f;
					GameObject prefab = ZNetScene.instance.GetPrefab("VL_SanctifyHammer");
					GO_Sanctify = UnityEngine.Object.Instantiate(prefab, position2, UnityEngine.Quaternion.identity);
					P_Sanctify = GO_Sanctify.GetComponent<Projectile>();
					P_Sanctify.name = "Sanctify";
					P_Sanctify.m_respawnItemOnHit = false;
					P_Sanctify.m_spawnOnHit = null;
					P_Sanctify.m_ttl = 30f;
					P_Sanctify.m_gravity = 9f;
					P_Sanctify.m_rayRadius = 1f;
					P_Sanctify.m_aoe = 8f + 0.04f * level5;
					GO_Sanctify.transform.localScale = UnityEngine.Vector3.one;
					HitData hitData2 = new HitData();
					hitData2.m_damage.m_fire = UnityEngine.Random.Range(10f + 0.5f * level5, 20f + 0.75f * level5) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestSanctify;
					hitData2.m_damage.m_blunt = UnityEngine.Random.Range(10f + 0.5f * level5, 20f + 0.75f * level5) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestSanctify;
					hitData2.m_damage.m_spirit = UnityEngine.Random.Range(10f + 0.5f * level5, 20f + 0.75f * level5) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestSanctify;
					hitData2.SetAttacker(player);
					hitData2.m_pushForce = 50f;
					hitData2.m_skill = ValheimLegends.EvocationSkill;
					P_Sanctify.Setup(player, new UnityEngine.Vector3(0f, -1f, 0f), -1f, hitData2, null, null);
					Traverse.Create(P_Sanctify).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
					GO_Sanctify = null;
					player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetSanctifySkillGain);
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Sanctify: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetSanctifyCost + ")");
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
