using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using UnityEngine;

namespace ValheimLegends;

public class Class_Shaman
{
	private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");

	private static int ObjectBlock_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "item");

	private static GameObject GO_CastFX;

	public static bool isWaterWalking = false;

	public static bool gotWindfuryCooldown = false;

	private static int glideDelay = 0;


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
				amount *= 0.7f;
			}
		}
	}


	public static void Process_Input(Player player, ref Rigidbody playerBody, ref float altitude, ref float lastGroundTouch, float waterLevel)
	{
		ValheimLegends.isChanneling = false;
        ValheimLegends.channelingBlocksMovement = true;
        if (ZInput.GetButton("Jump"))
		{
			glideDelay++;
			if (!player.IsDead() && !player.InAttack() && !player.IsEncumbered() && !player.InDodge() && !player.IsKnockedBack() && glideDelay > 20 && player.transform.position.y <= waterLevel + 0.4f)
			{
				bool flag = true;
				if (!player.HaveStamina(1f))
				{
					if (player.IsPlayer())
					{
						Hud.instance.StaminaBarEmptyFlash();
					}
					flag = false;
					isWaterWalking = false;
				}
				if (flag)
				{
					player.UseStamina(0.3f * VL_GlobalConfigs.c_shamanBonusWaterGlideCost);
					VL_Utility.RotatePlayerToTarget(player);
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).StopAllCoroutines();
					RaycastHit hitInfo = default(RaycastHit);
					UnityEngine.Vector3 vector = player.transform.position + player.transform.up * 0.15f;
					UnityEngine.Vector3 lookDir = player.GetLookDir();
					lookDir.y = 0f;
					Physics.SphereCast(vector, 0.1f, lookDir, out hitInfo, 10f, ObjectBlock_Layermask);
					UnityEngine.Vector3 vector2 = new UnityEngine.Vector3(player.transform.position.x + player.GetLookDir().x * 0.3f, waterLevel + 0.3f, player.transform.position.z + player.GetLookDir().z * 0.3f);
					if (UnityEngine.Vector3.Distance(vector, vector2) + 0.25f > UnityEngine.Vector3.Distance(vector, hitInfo.point))
					{
						vector2 = new UnityEngine.Vector3(player.transform.position.x, waterLevel + 0.3f, player.transform.position.z);
					}
					playerBody.position = vector2;
					playerBody.linearVelocity = UnityEngine.Vector3.zero;
					ValheimLegends.isChanneling = true;
					Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_FlyingKick"), player.transform.position + player.transform.up * -0.2f, UnityEngine.Quaternion.LookRotation(player.transform.forward * -1f));
				}
			}
		}
		else
		{
			glideDelay = 0;
		}
		if (player.transform.position.y + 0.3f > waterLevel)
		{
			isWaterWalking = false;
		}
		if (VL_Utility.Ability3_Input_Down)
		{
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				if (player.IsBlocking())
				{
					if (player.GetStamina() >= VL_Utility.GetSpiritBombCost(player))
					{
						ValheimLegends.shouldUseGuardianPower = false;
						float level4 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
						StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
						statusEffect.m_ttl = VL_Utility.GetSpiritBombCooldown(player);
						player.GetSEMan().AddStatusEffect(statusEffect);
						player.UseStamina(VL_Utility.GetSpiritBombCost(player));
						player.StartEmote("challenge");
						GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_guardstone_permitted_add"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
						GO_CastFX = UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), player.transform.position, UnityEngine.Quaternion.identity);
						HealNearbyPlayers(player, 20f + 0.2f * level4, (10f + level4) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_priestHeal * 1.0f);
						player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetSpiritBombSkillGain(player));
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to for Chain Healing: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetSpiritBombCost(player) + ")");
					}

				} else {  
					if (player.GetStamina() > VL_Utility.GetSpiritBombCost(player))
					{
						StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
						statusEffect.m_ttl = VL_Utility.GetSpiritBombCooldown(player) * 1.5f;
						player.GetSEMan().AddStatusEffect(statusEffect);
						player.UseStamina(VL_Utility.GetSpiritBombCost(player));
						float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("battleaxe_attack1");
						Object.Instantiate(ZNetScene.instance.GetPrefab("fx_goblinking_nova"), player.transform.position, UnityEngine.Quaternion.identity);
						SE_SpiritDrain sE_SpiritDrain = (SE_SpiritDrain)ScriptableObject.CreateInstance(typeof(SE_SpiritDrain));
						sE_SpiritDrain.m_ttl = SE_SpiritDrain.m_baseTTL;
						sE_SpiritDrain.damageModifier = 1f + 0.1f * level;
						List<Character> allCharacters = Character.GetAllCharacters();
						foreach (Character item in allCharacters)
						{
							if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 11f + 0.05f * level && VL_Utility.LOS_IsValid(item, player.GetCenterPoint(), player.transform.position))
							{
								UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
								HitData hitData = new HitData();
								hitData.m_damage.m_spirit = Random.Range(15f + 0.8f * level, 30f + 1.5f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_shamanSpiritShock;
								hitData.m_damage.m_lightning = Random.Range(15f + 0.8f * level, 30f + 1.5f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_shamanSpiritShock;
								hitData.m_pushForce = 25f + 0.1f * level;
								hitData.m_point = item.GetEyePoint();
								hitData.m_dir = dir;
								hitData.m_skill = ValheimLegends.EvocationSkill;
								item.Damage(hitData);
								item.GetSEMan().AddStatusEffect(sE_SpiritDrain);
							}
						}
						player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetSpiritBombSkillGain(player));
						if (Class_Shaman.gotWindfuryCooldown)
						{
							StatusEffect statusEffectW = (SE_Windfury_CD)ScriptableObject.CreateInstance(typeof(SE_Windfury_CD));
							player.GetSEMan().RemoveStatusEffect(statusEffectW);
							Class_Shaman.gotWindfuryCooldown = false;
						}
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Spirit Shock: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetSpiritBombCost(player) + ")");
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
                if (player.IsBlocking())
                {
                    var inv = player.GetInventory();
                    if (inv == null) return;

                    int requiredItems = 1;
                    string materialName = "Thunderstone";

                    // 1) checar material
                    ItemDrop.ItemData foundItem = VL_Utility.FindItemByPrefabName(inv, materialName, requiredItems);
                    if (foundItem == null)
                    {
                        player.Message(MessageHud.MessageType.Center,
                            "You need a Thunderstone to create a Spirit Binding Vial.");
                        return;
                    }

                    // 2) checar stamina
                    if (player.GetStamina() < VL_Utility.GetShellCost(player))
                    {
                        player.Message(MessageHud.MessageType.TopLeft,
                            "Not enough stamina to Create Spirit Binding Vial: (" +
                            player.GetStamina().ToString("#.#") + "/" +
                            VL_Utility.GetShellCost(player).ToString("#.#") + ")");
                        return;
                    }

                    // 3) cooldown/skill etc (seu código)
                    player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetHealSkillGain);
                    StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
                    float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
                        .m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
                    statusEffect.m_ttl = VL_Utility.GetHealCooldownTime * 20f / (1f + level / 150f);
                    player.GetSEMan().AddStatusEffect(statusEffect);
                    player.UseStamina(VL_Utility.GetShellCost(player));

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
                        ItemDrop.DropItem(vialItem, 1, player.transform.position + player.transform.forward, UnityEngine.Quaternion.identity);
                        player.Message(MessageHud.MessageType.TopLeft, "Inventory full. Dropped Spirit Binding Vial on the ground.");
                    }
                    else
                    {
                        player.Message(MessageHud.MessageType.TopLeft,
                            $"Consumed 1 {materialName} to create a {vialItem.m_shared.m_name}.");
                    }

                    player.StartEmote("cheer");
                    UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), player.transform.position, UnityEngine.Quaternion.identity);
                    UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_WishbonePing"), player.transform.position, UnityEngine.Quaternion.identity);
                }
                else
                {
					if (player.GetStamina() > VL_Utility.GetShellCost(player))
					{
						StatusEffect statusEffect2 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
						statusEffect2.m_ttl = VL_Utility.GetShellCooldown(player);
						player.GetSEMan().AddStatusEffect(statusEffect2);
						player.UseStamina(VL_Utility.GetShellCost(player));
						float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
						ValheimLegends.shouldUseGuardianPower = false;
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("gpower");
						((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetSpeed(1.25f);
						List<Character> list = new List<Character>();
						list.Clear();
						Character.GetCharactersInRange(player.transform.position, 30f + 0.2f * level2, list);
						GameObject prefab = ZNetScene.instance.GetPrefab("fx_guardstone_permitted_add");
						foreach (Character item2 in list)
						{
							SE_Shell sE_Shell = (SE_Shell)ScriptableObject.CreateInstance(typeof(SE_Shell));
							sE_Shell.m_ttl = SE_Shell.m_baseTTL + 0.3f * level2;
							sE_Shell.spiritDamageOffset = (6f + 0.3f * level2) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_shamanShell;
							sE_Shell.resistModifier = (0.6f - 0.006f * level2) * VL_GlobalConfigs.c_shamanShell;
							sE_Shell.m_icon = ZNetScene.instance.GetPrefab("ShieldSerpentscale").GetComponent<ItemDrop>().m_itemData.GetIcon();
							sE_Shell.doOnce = false;
							if (!BaseAI.IsEnemy(player, item2))
							{
								if (item2 == Player.m_localPlayer)
								{
									item2.GetSEMan().AddStatusEffect(sE_Shell, resetTime: true);
								}
								else if (item2.IsPlayer())
								{
									item2.GetSEMan().AddStatusEffect(sE_Shell.name.GetStableHashCode(), resetTime: true);
								}
								else
								{
									item2.GetSEMan().AddStatusEffect(sE_Shell, resetTime: true);
								}
								Object.Instantiate(prefab, item2.GetCenterPoint(), UnityEngine.Quaternion.identity);
							}
						}
						Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_wraith_death"), player.transform.position, UnityEngine.Quaternion.identity);
						player.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetShellSkillGain(player));
					}
					else
					{
						player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Shell: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetShellCost(player) + ")");
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
			if (!VL_Utility.Ability1_Input_Down)
			{
				return;
			}
			if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
			{
				if (player.GetStamina() > VL_Utility.GetEnrageCost(player))
				{
					StatusEffect statusEffect3 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
					statusEffect3.m_ttl = VL_Utility.GetEnrageCooldown(player);
					player.GetSEMan().AddStatusEffect(statusEffect3);
					player.UseStamina(VL_Utility.GetEnrageCost(player));
					float level3 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("challenge");
					GameObject prefab2 = ZNetScene.instance.GetPrefab("fx_guardstone_permitted_removed");
					prefab2.transform.localScale = UnityEngine.Vector3.one * 3f;
					Object.Instantiate(prefab2, player.GetCenterPoint(), UnityEngine.Quaternion.identity);
					GameObject prefab3 = ZNetScene.instance.GetPrefab("fx_GP_Activation");
					List<Character> list2 = new List<Character>();
					Character.GetCharactersInRange(player.transform.position, 30f, list2);
					SE_Enrage sE_Enrage = (SE_Enrage)ScriptableObject.CreateInstance(typeof(SE_Enrage));
					sE_Enrage.m_ttl = 16f + 0.2f * level3;
					sE_Enrage.staminaModifier = (5f + 0.1f * level3) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_shamanEnrage;
					sE_Enrage.speedModifier = 1.2f + 0.0025f * level3;
					sE_Enrage.m_icon = ZNetScene.instance.GetPrefab("TrophyGoblinBrute").GetComponent<ItemDrop>().m_itemData.GetIcon();
					sE_Enrage.doOnce = false;
					foreach (Character item3 in list2)
					{
						if (!BaseAI.IsEnemy(player, item3))
						{
							if (item3 == Player.m_localPlayer)
							{
								item3.GetSEMan().AddStatusEffect(sE_Enrage, resetTime: true);
							}
							else if (item3.IsPlayer())
							{
								item3.GetSEMan().AddStatusEffect(sE_Enrage.name.GetStableHashCode(), resetTime: true);
							}
							else
							{
								item3.GetSEMan().AddStatusEffect(sE_Enrage, resetTime: true);
							}
							Object.Instantiate(prefab3, item3.GetCenterPoint(), UnityEngine.Quaternion.identity);
						}
					}
					player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetEnrageSkillGain(player));
				}
				else
				{
					player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina for Enrage: (" + player.GetStamina().ToString("#.#") + "/" + VL_Utility.GetEnrageCost(player) + ")");
				}
			}
			else
			{
				player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
			}
		}
	}
}
