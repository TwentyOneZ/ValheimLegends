using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ValheimLegends;

[BepInPlugin("ValheimLegends", "ValheimLegends", "0.5.0")]
[BepInDependency("EpicMMOSystem", BepInDependency.DependencyFlags.SoftDependency)]
public class ValheimLegends : BaseUnityPlugin
{
	public class VL_Player
	{
		public string vl_name;

		public PlayerClass vl_class;
	}

	public enum PlayerClass
	{
		None = 0,
		Berserker = 1,
		Druid = 2,
		Metavoker = 3,
		Mage = 4,
		Priest = 5,
		Monk = 7,
		Ranger = 8,
		Duelist = 9,
		Enchanter = 10,
		Rogue = 11,
		Shaman = 16,
		Valkyrie = 32
	}

	public static ItemDrop coinsItem;

	public static void DefineCoins()
    {
		if (ValheimLegends.coinsItem == null)
		{
			foreach (ItemDrop go in Resources.FindObjectsOfTypeAll(typeof(ItemDrop)) as ItemDrop[])
			{
				if (go.m_itemData.m_shared.m_name.ToLower().Contains("$item_coins"))
				{
					ValheimLegends.coinsItem = go;
					try
					{
						ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, 0, Player.m_localPlayer.transform.position, UnityEngine.Quaternion.identity);
						break;
					}
                    catch
                    {
					}
				}
			}
		}
	}

    [HarmonyPatch(typeof(ZNet), "Awake")]
	[HarmonyPriority(int.MaxValue)]
	public static class ZNet_VL_Register
	{
		public static void Postfix(ZNet __instance, ZRoutedRpc ___m_routedRpc)
		{
			___m_routedRpc.Register<ZPackage>("VL_ConfigSync", VL_ConfigSync.RPC_VL_ConfigSync);
		}
	}

	[HarmonyPatch(typeof(ZNet), "RPC_PeerInfo")]
	public static class ConfigServerSync
	{
		private static void Postfix(ref ZNet __instance, ZRpc rpc)
		{
			MethodBase methodBase = AccessTools.Method(typeof(ZRoutedRpc), "GetServerPeerID");
			ServerID = (long)methodBase.Invoke(ZRoutedRpc.instance, new object[0]);
			if (!__instance.IsServer())
			{
				ZRoutedRpc.instance.InvokeRoutedRPC(ServerID, "VL_ConfigSync", new ZPackage());
			}
		}
	}

	[HarmonyPatch(typeof(PlayerProfile), "SavePlayerToDisk", null)]
	public static class SaveVLPlayer_Patch
	{
		public static void Postfix(PlayerProfile __instance, string ___m_filename, string ___m_playerName)
		{
			try
			{
				Directory.CreateDirectory(Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/characters/VL");
				string text = Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/characters/VL/" + ___m_filename + "_vl.fch";
				string text2 = Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/characters/VL/" + ___m_filename + "_vl.fch.new";
				ZPackage zPackage = new ZPackage();
				zPackage.Write(GetPlayerClassNum);
				byte[] array = zPackage.GenerateHash();
				byte[] array2 = zPackage.GetArray();
				FileStream fileStream = File.Create(text2);
				BinaryWriter binaryWriter = new BinaryWriter(fileStream);
				binaryWriter.Write(array2.Length);
				binaryWriter.Write(array2);
				binaryWriter.Write(array.Length);
				binaryWriter.Write(array);
				binaryWriter.Flush();
				fileStream.Flush(flushToDisk: true);
				fileStream.Close();
				fileStream.Dispose();
				if (File.Exists(text))
				{
					File.Delete(text);
				}
				File.Move(text2, text);
			}
			catch (NullReferenceException)
			{
			}
		}
	}

	[HarmonyPatch(typeof(PlayerProfile), "LoadPlayerFromDisk", null)]
	public class LoadVLPlayer_Patch
	{
		public static void Postfix(PlayerProfile __instance, string ___m_filename, string ___m_playerName)
		{
			try
			{
				if (vl_playerList == null)
				{
					vl_playerList = new List<VL_Player>();
				}
				vl_playerList.Clear();
				ZPackage zPackage = LoadPlayerDataFromDisk(___m_filename);
				if (zPackage != null)
				{
					int vl_class = zPackage.ReadInt();
					VL_Player vL_Player = new VL_Player();
					vL_Player.vl_name = ___m_playerName;
					vL_Player.vl_class = (PlayerClass)vl_class;
					vl_playerList.Add(vL_Player);
				}
			}
			catch (Exception ex)
			{
				ZLog.LogWarning("Exception while loading player VL profile: " + ex.ToString());
			}
		}

		private static ZPackage LoadPlayerDataFromDisk(string m_filename)
		{
			string path = Utils.GetSaveDataPath(FileHelpers.FileSource.Local) + "/characters/VL/" + m_filename + "_vl.fch";
			FileStream fileStream;
			try
			{
				fileStream = File.OpenRead(path);
			}
			catch
			{
				return null;
			}
			byte[] data;
			try
			{
				BinaryReader binaryReader = new BinaryReader(fileStream);
				int count = binaryReader.ReadInt32();
				data = binaryReader.ReadBytes(count);
				int count2 = binaryReader.ReadInt32();
				binaryReader.ReadBytes(count2);
			}
			catch
			{
				ZLog.LogError("  error loading VL player data");
				fileStream.Dispose();
				return null;
			}
			fileStream.Dispose();
			return new ZPackage(data);
		}
	}

	public enum SkillName
	{
		Discipline = 781,
		Abjuration = 791,
		Alteration = 792,
		Conjuration = 793,
		Evocation = 794,
		Illusion = 795
	}

	[HarmonyPatch(typeof(Skills), "CheatRaiseSkill", null)]
	public class CheatRaiseSkill_VL_Patch
	{
		public static bool Prefix(Skills __instance, string name, float value, Player ___m_player)
		{
			if (VL_Console.CheatRaiseSkill(__instance, name, value, ___m_player))
			{
				Console.instance.Print("Skill " + name + " raised " + value);
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Terminal), "TryRunCommand", null)]
	public class Cheats_VL_Patch
	{
		public static void Postfix(Terminal __instance, string text, bool silentFail, bool skipAllowedCheck)
		{
			if ((bool)ZNet.instance && ZNet.instance.IsServer() && (bool)Player.m_localPlayer && __instance.IsCheatsEnabled() && playerEnabled)
			{
				string[] array = text.Split(' ');
				if (array.Length > 1 && array[0] == "vl_changeclass")
				{
					string className = array[1];
					VL_Console.CheatChangeClass(className);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Aoe), "OnHit")]
	public static class Aoe_LOSCheck_Prefix
	{
		private static bool Prefix(Aoe __instance, Collider collider, UnityEngine.Vector3 hitPoint, List<GameObject> ___m_hitList, ref bool __result)
		{
			GameObject gameObject = Projectile.FindHitObject(collider);
			if (___m_hitList.Contains(gameObject))
			{
				__result = false;
				return false;
			}
			IDestructible component = gameObject.GetComponent<IDestructible>();
			if (component != null)
			{
				Character character = component as Character;
				if ((bool)character && !VL_Utility.LOS_IsValid(character, __instance.transform.position))
				{
					__result = false;
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Humanoid), "GetCurrentWeapon")]
	public static class UnarmedDamage
	{
		private static ItemDrop.ItemData Postfix(ItemDrop.ItemData __weapon, ref Character __instance)
		{
			if (__weapon != null && __weapon.m_shared.m_name == "Unarmed")
			{
				Player player = (Player)__instance;
				__weapon.m_shared.m_damages.m_blunt = (2 + (player.GetSkillFactor(Skills.SkillType.Unarmed)) / (100 / 3)) * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 100f));
			}
			return __weapon; 
		}
	}

	[HarmonyPatch(typeof(Player), "ActivateGuardianPower", null)]
	public class ActivatePowerPrevention_Patch
	{
		public static bool Prefix(Player __instance, ref bool __result)
		{
			if (!shouldUseGuardianPower)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Player), "OnDodgeMortal", null)]
	public class DodgeBreaksChanneling_Patch
	{
		public static void Postfix(Player __instance)
		{
			if (isChanneling)
			{
				isChanneling = false;
                channelingBlocksMovement = true;
            }
		}
	}

	[HarmonyPatch(typeof(Player), "StartGuardianPower", null)]
	public class StartPowerPrevention_Patch
	{
		public static bool Prefix(Player __instance, ref bool __result)
		{
			if (!shouldUseGuardianPower)
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

    [HarmonyPatch(typeof(Player), "CanMove", null)]
    public class CanMove_Casting_Patch
    {
        public static void Postfix(Player __instance, ref bool __result)
        {
            if (isChanneling && channelingBlocksMovement)
            {
                __result = false;
            }
        }
    }


    [HarmonyPatch(typeof(Menu), "OnQuit", null)]
	public class QuitYes_Patch
	{
		public static bool Prefix()
		{
			RemoveSummonedWolf();
			return true;
		}
	}

	[HarmonyPatch(typeof(Menu), "OnLogout", null)]
	public class RemoveWolfOnLogout_Patch
	{
		public static bool Prefix()
		{
			RemoveSummonedWolf();
			return true;
		}
	}

	[HarmonyPatch(typeof(Attack), "Start", null)]
	public class ShadowWolfAttack_Patch
	{
		public static bool Prefix(Attack __instance, Humanoid character, Rigidbody body, ZSyncAnimation zanim, CharacterAnimEvent animEvent, VisEquipment visEquipment, ItemDrop.ItemData weapon, Attack previousAttack, float timeSinceLastAttack, float attackDrawPercentage, string ___m_attackAnimation)
		{
			if (character != null && (character.m_name == "Shadow Wolf" || character.m_name.Contains("Demon Wolf")))
			{
                UnityEngine.Vector3 vector = character.transform.position + character.transform.up * 0.3f;
				RaycastHit hitInfo = default(RaycastHit);
				Physics.SphereCast(vector, 0.2f, character.transform.forward, out hitInfo, 3f, Script_WolfAttackMask);
				if (hitInfo.collider != null && hitInfo.collider.gameObject != null)
				{
					hitInfo.collider.gameObject.TryGetComponent<Character>(out var component);
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
					if (flag && BaseAI.IsEnemy(component, character) && !component.IsDodgeInvincible())
					{
                        UnityEngine.Vector3 Vector2 = vector - component.GetEyePoint();
						float num = UnityEngine.Random.Range(0.6f, 1.2f);
						if (character.GetSEMan().HaveStatusEffect("SE_VL_Companion".GetStableHashCode()))
						{
							SE_Companion sE_Companion = (SE_Companion)character.GetSEMan().GetStatusEffect("SE_VL_Companion".GetStableHashCode());
							num *= sE_Companion.damageModifier;
						}
						HitData hitData = new HitData();
						hitData.m_damage = weapon.GetDamage();
						hitData.m_damage.m_slash = weapon.GetDamage().m_slash * num;
						hitData.m_point = hitInfo.point;
						hitData.m_dir = component.transform.position - character.transform.position;
						hitData.m_skill = Skills.SkillType.Unarmed;
						if (component.IsBlocking())
						{
							Player player = component as Player;
							if (player != null)
							{
								MethodBase methodBase = AccessTools.Method(typeof(Humanoid), "BlockAttack");
								methodBase.Invoke(player, new object[2] { hitData, character });
							}
						}
						else
						{
							component.Damage(hitData);
						}
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(BaseAI), "CanSenseTarget", new Type[] { typeof(Character) })]
	public class CanSee_Shadow_Patch
	{
		public static bool Prefix(BaseAI __instance, Character target, ref bool __result)
		{
			if (target != null)
			{
				Player player = target as Player;
				if (player != null && player.GetSEMan().HaveStatusEffect("SE_VL_ShadowStalk".GetStableHashCode()) && player.IsCrouching())
				{
					__result = false;
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Character), "UpdateGroundContact", null)]
	public class Valkyrie_ValidateHeight_Patch
	{
		public static bool Prefix(Character __instance, ref float ___m_maxAirAltitude, bool ___m_groundContact)
		{
			if (vl_player != null && vl_player.vl_class == PlayerClass.Monk && ___m_groundContact && Mathf.Max(0f, ___m_maxAirAltitude - __instance.transform.position.y) > 4f)
			{
				___m_maxAirAltitude -= 6f;
			}
			return true;
		}

		public static void Postfix(Character __instance, float ___m_maxAirAltitude, bool ___m_groundContact)
		{
			if (__instance == Player.m_localPlayer && Class_Valkyrie.inFlight && Mathf.Max(0f, ___m_maxAirAltitude - __instance.transform.position.y) > 1f)
			{
				shouldValkyrieImpact = true;
			}
		}
	}

	[HarmonyPatch(typeof(Character), "ResetGroundContact", null)]
	public class Valkyrie_GroundContact_Patch
	{
		public static void Postfix(Character __instance, float ___m_maxAirAltitude, bool ___m_groundContact)
		{
			if (__instance == Player.m_localPlayer && shouldValkyrieImpact)
			{
				float altitude = Mathf.Max(0f, ___m_maxAirAltitude - __instance.transform.position.y);
				shouldValkyrieImpact = false;
				if (vl_player.vl_class == PlayerClass.Valkyrie)
				{
					Class_Valkyrie.Impact_Effect(Player.m_localPlayer, altitude);
					Class_Valkyrie.inFlight = false;
				}
				if (vl_player.vl_class == PlayerClass.Monk)
				{
					Class_Monk.Impact_Effect(Player.m_localPlayer, altitude);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Humanoid), "UseItem")]
	internal class UseItemPatch
	{
		public static bool Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui, Inventory ___m_inventory, ZSyncAnimation ___m_zanim)
		{
			string name = item.m_shared.m_name;
			Player player = __instance as Player;
			if (player != null && vl_player != null && player.GetPlayerName() == vl_player.vl_name && vl_player.vl_class == PlayerClass.Druid && (name.Contains("$item_pinecone") || name.Contains("$item_beechseeds") || name.Contains("$item_fircone") || name.Contains("$item_ancientseed") || name.Contains("$item_birchseeds")))
			{
				if (inventory == null)
				{
					inventory = ___m_inventory;
				}
				if (!inventory.ContainsItem(item))
				{
					return false;
				}
				GameObject hoverObject = __instance.GetHoverObject();
				Hoverable hoverable = (hoverObject ? hoverObject.GetComponentInParent<Hoverable>() : null);
				if (hoverable != null && !fromInventoryGui)
				{
					Interactable componentInParent = hoverObject.GetComponentInParent<Interactable>();
					if (componentInParent != null && componentInParent.UseItem(__instance, item))
					{
						return false;
					}
				}
				SE_SeedRegeneration sE_SeedRegeneration = (SE_SeedRegeneration)ScriptableObject.CreateInstance(typeof(SE_SeedRegeneration));
				sE_SeedRegeneration.m_ttl = SE_SeedRegeneration.m_baseTTL;
				sE_SeedRegeneration.m_icon = item.GetIcon();
				if (name.Contains("$item_pinecone"))
				{
					sE_SeedRegeneration.m_HealAmount = 10f;
				}
				else if (name.Contains("$item_ancientseed"))
				{
					sE_SeedRegeneration.m_HealAmount = 20f;
				}
				else if (name.Contains("$item_fircone"))
				{
					sE_SeedRegeneration.m_HealAmount = 7f;
				}
				else if (name.Contains("$item_birchseeds"))
				{
					sE_SeedRegeneration.m_HealAmount = 12f;
				}
				else
				{
					sE_SeedRegeneration.m_HealAmount = 5f;
				}
				sE_SeedRegeneration.m_HealAmount *= VL_GlobalConfigs.c_druidBonusSeeds;
				player.GetSEMan().AddStatusEffect(sE_SeedRegeneration, resetTime: true);
				UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), player.transform.position, UnityEngine.Quaternion.identity);
				inventory.RemoveOneItem(item);
				__instance.m_consumeItemEffects.Create(Player.m_localPlayer.transform.position, UnityEngine.Quaternion.identity);
				___m_zanim.SetTrigger("eat");
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Character), "Stagger", null)]
	public class VL_StaggerPrevention_Patch
	{
		public static bool Prefix(Character __instance)
		{
			if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Berserk".GetStableHashCode()))
			{
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Attack), "GetAttackStamina", null)]
	public class AttackStaminaReduction_Patch
	{
		public static void Postfix(Attack __instance, ref Humanoid ___m_character, ItemDrop.ItemData ___m_weapon, ref float __result)
		{
			if (___m_character != null || !___m_character.IsPlayer())
			{
				return;
			}
			Player localPlayer = Player.m_localPlayer;
			if (localPlayer == null || ___m_weapon == null)
			{
				return;
			}
			ItemDrop.ItemData hasLeftItem = Traverse.Create(localPlayer).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
			ItemDrop.ItemData hasRightItem = Traverse.Create(localPlayer).Field("m_rightItem").GetValue<ItemDrop.ItemData>();
			if (hasLeftItem == null || hasRightItem == null)
			{
				return;
			}
			ItemDrop.ItemData.SharedData sharedL = hasLeftItem.m_shared;
			ItemDrop.ItemData.SharedData sharedR = hasRightItem.m_shared;
			if (sharedL == null || sharedR == null)
			{
				return;
			}
			if (ValheimLegends.vl_player != null && ValheimLegends.vl_player.vl_class == ValheimLegends.PlayerClass.Berserker && ___m_weapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
			{
				if ((sharedL.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) && (sharedR.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) && (!sharedL.m_name.ToLower().Contains("torch")) && (sharedR.m_skillType == sharedL.m_skillType))
				{
					__result *= Mathf.Sqrt(0.5f) * VL_GlobalConfigs.c_berserkerBonus2h;
				}
			}
			if (ValheimLegends.vl_player != null && ValheimLegends.vl_player.vl_class == ValheimLegends.PlayerClass.Rogue && ___m_weapon.m_shared.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon)
			{
				if ((sharedL.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) && (sharedR.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) && (!sharedL.m_name.ToLower().Contains("torch")) && (sharedR.m_skillType == sharedL.m_skillType) && (sharedR.m_skillType == Skills.SkillType.Knives))
				{
					__result *= Mathf.Sqrt(0.7f);
				}
			}
			return;
		}
	}

	[HarmonyPatch(typeof(Character), "Damage", null)]
	public class VL_Damage_Patch
	{
		public static bool Prefix(Character __instance, ref HitData hit, float ___m_maxAirAltitude)
		{
			Character attacker = hit.GetAttacker();
			if (__instance == Player.m_localPlayer && Class_Valkyrie.inFlight)
			{
				Class_Valkyrie.inFlight = false;
				return false;
			}
			if (__instance.GetSEMan() != null && __instance.GetSEMan().HaveStatusEffect("SE_VL_Charm".GetStableHashCode()) && attacker.IsPlayer())
			{
				SE_Charm sE_Charm = (SE_Charm)__instance.GetSEMan().GetStatusEffect("SE_VL_Charm".GetStableHashCode());
				sE_Charm.charmPower = Mathf.Clamp(4f / (Mathf.Sqrt(__instance.GetMaxHealth()) * __instance.GetHealthPercentage() * __instance.GetHealthPercentage()), 0.05f, 0.95f); 
				__instance.m_faction = sE_Charm.originalFaction;
				__instance.SetTamed(tamed: false);
				__instance.GetSEMan().RemoveStatusEffect(sE_Charm, quiet: true);
				StatusEffect statusEffect = (SE_CharmImmunity)ScriptableObject.CreateInstance(typeof(SE_CharmImmunity));
				statusEffect.m_ttl = Mathf.Clamp(__instance.GetHealthPercentage() * VL_GlobalConfigs.g_CooldownModifer * 60f, 5f, 300f);
				__instance.GetSEMan().AddStatusEffect(statusEffect);
				UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), __instance.GetEyePoint(), UnityEngine.Quaternion.identity);
			}
			if (__instance.GetSEMan() != null && hit.HaveAttacker() && !hit.m_ranged && __instance.GetSEMan().HaveStatusEffect("SE_VL_BiomeBlackForest".GetStableHashCode()))
			{
				SE_BiomeBlackForest sE_BiomeBlackForest = __instance.GetSEMan().GetStatusEffect("SE_VL_BiomeBlackForest".GetStableHashCode()) as SE_BiomeBlackForest;
				HitData hitData = new()
				{
					m_attacker = __instance.GetZDOID(),
					m_dir = hit.m_dir * -1,
					m_point = attacker.transform.localPosition,
					m_damage = hit.m_damage,
				};
				hitData.ApplyModifier(sE_BiomeBlackForest.reflectModifier);
				attacker.Damage(hitData);
			}

			if (attacker != null)
			{
                if (__instance.GetSEMan() != null && hit.HaveAttacker() && !hit.m_ranged && __instance.GetSEMan().HaveStatusEffect("SE_VL_FlameArmor".GetStableHashCode()))
                {
                    Player localplayer = Player.m_localPlayer;
                    if (localplayer != null)
                    {
                        if (localplayer.GetSEMan().HaveStatusEffect("SE_VL_FlameWeapon".GetStableHashCode()))
                        {
                            long pid = localplayer.GetPlayerID();
                            int stacks = AddEnchanterWeaponCharges(pid, EnchanterWeaponElement.Flame, 1);
                            UpdateEnchanterWeaponSEName(localplayer, EnchanterWeaponElement.Flame, stacks, localplayer.GetEyePoint());
                        }
                    }
                }
                if (__instance.GetSEMan() != null && hit.HaveAttacker() && !hit.m_ranged && __instance.GetSEMan().HaveStatusEffect("SE_VL_IceArmor".GetStableHashCode()))
				{
					Player localplayer = Player.m_localPlayer;
					if (localplayer != null)
					{
                        float abjurationLevel = localplayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
                            .m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
                        SE_Slow sE_Slow = (SE_Slow)ScriptableObject.CreateInstance(typeof(SE_Slow));
                        sE_Slow.m_ttl = 4f + 6f * abjurationLevel;
                        sE_Slow.speedAmount = 0.7f - (abjurationLevel / 250f);
                        attacker.GetSEMan().AddStatusEffect(sE_Slow.name.GetStableHashCode(), resetTime: true);
                        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_DvergerMage_Ice_hit"), hit.GetAttacker().transform.position, UnityEngine.Quaternion.identity);

                        if (localplayer.GetSEMan().HaveStatusEffect("SE_VL_IceWeapon".GetStableHashCode())) {
                            long pid = localplayer.GetPlayerID();
                            int stacks = AddEnchanterWeaponCharges(pid, EnchanterWeaponElement.Ice, 1);
                            UpdateEnchanterWeaponSEName(localplayer, EnchanterWeaponElement.Ice, stacks, localplayer.GetEyePoint());

                            if (localplayer.GetSEMan().HaveStatusEffect("Burning".GetStableHashCode()))
                            {
                                localplayer.GetSEMan().RemoveStatusEffect("Burning".GetStableHashCode());
                            }
                        }
                    }
                }
				if (__instance.GetSEMan() != null && hit.HaveAttacker() && __instance.GetSEMan().HaveStatusEffect("SE_VL_ThunderArmor".GetStableHashCode()))
				{
					Player localplayer = Player.m_localPlayer;
					if (localplayer != null)
					{
						if (localplayer.GetSEMan().HaveStatusEffect("SE_VL_ThunderWeapon".GetStableHashCode()))
						{
							long pid = localplayer.GetPlayerID();
							int stacks = AddEnchanterWeaponCharges(pid, EnchanterWeaponElement.Thunder, 1);
							UpdateEnchanterWeaponSEName(localplayer, EnchanterWeaponElement.Thunder, stacks, localplayer.GetEyePoint());
						}

                        float abjurationLevel = localplayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
                            .m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
						float playerLevel = EpicMMOSystem.LevelSystem.Instance.getLevel();

                        if (hit.m_ranged)
						{
                            if (UnityEngine.Random.Range(0f, 1f) <= Math.Clamp((EpicMMOSystem.LevelSystem.Instance.getLevel() / hit.m_damage.GetTotalDamage()), 0.2f, 0.6f))
                            {
                                hit.ApplyModifier(0f);
                                UnityEngine.Vector3 dir = attacker.transform.position - localplayer.transform.position;
                                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_chainlightning_spread"), localplayer.GetEyePoint(), UnityEngine.Quaternion.LookRotation(hit.GetAttacker().transform.position - localplayer.GetEyePoint()));
                                localplayer.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetFireballSkillGain * 0.015f);
                            }
						}
						else
						{
                            if (UnityEngine.Random.Range(0f, 1f) <= Math.Clamp((EpicMMOSystem.LevelSystem.Instance.getLevel() / hit.m_damage.GetTotalDamage()), 0.05f, 0.15f))
                            {
                                hit.GetAttacker().Stagger(-hit.m_dir);
                                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_chainlightning_spread"), localplayer.GetEyePoint(), UnityEngine.Quaternion.LookRotation(hit.GetAttacker().transform.position - localplayer.GetEyePoint()));
                                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_chainlightning_hit"), hit.GetAttacker().GetEyePoint(), UnityEngine.Quaternion.identity);
                                localplayer.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetFireballSkillGain * 0.015f);
                            }
                        }
                    }
                }
				if (hit != null && __instance.GetSEMan() != null && __instance.GetSEMan().HaveStatusEffect("SE_VL_Reactivearmor".GetStableHashCode()))
				{
					SE_Reactivearmor SE_Reactivearmor = (SE_Reactivearmor)__instance.GetSEMan().GetStatusEffect("SE_VL_Reactivearmor".GetStableHashCode());
					if (SE_Reactivearmor.hitCount > 0)
					{
						float staminaCost = hit.m_damage.GetTotalDamage() * 2f * SE_Reactivearmor.staminaModifier * (1f - (EpicMMOSystem.LevelSystem.Instance.getStaminaReduction() / 100f));
						float currentStamina = (__instance.GetMaxStamina() * __instance.GetStaminaPercentage());
						if (currentStamina >= staminaCost)
						{
							__instance.UseStamina(staminaCost);
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightburst"), __instance.GetEyePoint(), UnityEngine.Quaternion.LookRotation(Player.m_localPlayer.GetLookDir()));
							hit.ApplyModifier(0f);
							if (!hit.m_ranged)
							{
								hit.GetAttacker().Stagger(-hit.m_dir);
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ForwardLightningShock"), hit.GetAttacker().transform.position, UnityEngine.Quaternion.LookRotation(__instance.GetLookDir()));
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightSuction"), hit.GetAttacker().GetEyePoint(), UnityEngine.Quaternion.identity);
							}
							SE_Reactivearmor.hitCount--;
							__instance.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetWarpSkillGain * 0.25f);
						}
						else
						{
							hit.ApplyModifier(1f - (currentStamina / staminaCost));
							__instance.UseStamina(staminaCost);
							__instance.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetWarpSkillGain * 0.25f);
							if (!hit.m_ranged)
							{
								hit.GetAttacker().Stagger(-hit.m_dir);
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ForwardLightningShock"), hit.GetAttacker().transform.position, UnityEngine.Quaternion.LookRotation(__instance.GetLookDir()));
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightSuction"), hit.GetAttacker().GetEyePoint(), UnityEngine.Quaternion.identity);
							}
							SE_Reactivearmor.hitCount = 0;
						}
					}
					if (SE_Reactivearmor.hitCount == 0)
					{
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightSuction"), __instance.transform.position, UnityEngine.Quaternion.identity);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), __instance.GetEyePoint(), UnityEngine.Quaternion.identity);
						__instance.GetSEMan().RemoveStatusEffect(SE_Reactivearmor, quiet: true);
						StatusEffect statusEffect4 = (SE_CDReactivearmor)ScriptableObject.CreateInstance(typeof(SE_CDReactivearmor));
						statusEffect4.m_ttl = VL_Utility.GetLightCooldownTime * 3;
						__instance.GetSEMan().AddStatusEffect(statusEffect4);
					}
				}
				if (__instance.GetSEMan() != null && __instance.GetSEMan().HaveStatusEffect("SE_VL_Weaken".GetStableHashCode()) && attacker.IsPlayer())
				{
					SE_Weaken sE_Weaken = (SE_Weaken)__instance.GetSEMan().GetStatusEffect("SE_VL_Weaken".GetStableHashCode());
					attacker.AddStamina(5f + hit.GetTotalDamage() * sE_Weaken.staminaDrain);
				}
				if (__instance.GetSEMan() != null && __instance.GetSEMan().HaveStatusEffect("SE_VL_Charm".GetStableHashCode()))
				{
					SE_Charm sE_Charm = (SE_Charm)__instance.GetSEMan().GetStatusEffect("SE_VL_Charm".GetStableHashCode());
					sE_Charm.charmPower = Mathf.Clamp(4f / (Mathf.Sqrt(__instance.GetMaxHealth()) * __instance.GetHealthPercentage() * __instance.GetHealthPercentage()), 0.05f, 0.95f);
					//Debug.Log($"Charm power ({__instance.m_name}): {sE_Charm.charmPower}");
					//Debug.Log($"Charmed attacks: {__instance.m_name} damaged by {attacker.m_name}, chance: {100f * sE_Charm.charmPower * 2f}%!");
					sE_Charm.m_ttl = (sE_Charm.m_ttl / 2f) - (hit.GetTotalDamage() * 0.2f);
					//Debug.Log($"Charmed Time Left (" + attacker.GetHoverName().ToString() + "): " + sE_Charm.GetRemaningTime().ToString("#.#") + " s");
					if ((hit.m_damage.GetTotalDamage() > 0) && ((UnityEngine.Random.value > sE_Charm.charmPower * 2f) || sE_Charm.m_ttl < 0))
					{
						//Debug.Log($"Charm released!");
						float charmPower = Mathf.Clamp(sE_Charm.charmPower * 2f, 1f, 150f);
						__instance.m_faction = sE_Charm.originalFaction;
						__instance.SetTamed(tamed: false);
						__instance.GetSEMan().RemoveStatusEffect(sE_Charm, quiet: true);
						StatusEffect statusEffect = (SE_CharmImmunity)ScriptableObject.CreateInstance(typeof(SE_CharmImmunity));
						statusEffect.m_ttl = Mathf.Clamp(__instance.GetHealthPercentage() * VL_GlobalConfigs.g_CooldownModifer * 60f, 5f, 300f);
						__instance.GetSEMan().AddStatusEffect(statusEffect);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), __instance.GetEyePoint(), UnityEngine.Quaternion.identity);
					}
				}
				if (attacker.GetSEMan() != null && attacker.GetSEMan().HaveStatusEffect("SE_VL_Charm".GetStableHashCode()))
				{
					SE_Charm sE_Charm = (SE_Charm)attacker.GetSEMan().GetStatusEffect("SE_VL_Charm".GetStableHashCode());
					sE_Charm.charmPower = Mathf.Clamp(4f / (Mathf.Sqrt(attacker.GetMaxHealth()) * attacker.GetHealthPercentage() * attacker.GetHealthPercentage()), 0.05f, 0.95f);
					//Debug.Log($"Charm power ({__instance.m_name}): {sE_Charm.charmPower}");
					//Debug.Log($"Charmed attacks: {attacker.m_name} attacking {attacker.m_name}, chance: {100f * (sE_Charm.charmPower)}%!");
					sE_Charm.m_ttl = sE_Charm.m_ttl - (hit.GetTotalDamage() * 0.2f);
					//Debug.Log($"Charmed Time Left (" + attacker.GetHoverName().ToString() + "): " + sE_Charm.GetRemaningTime().ToString("#.#") + " s");
					if ((hit.m_damage.GetTotalDamage() > 0) && ((UnityEngine.Random.value > sE_Charm.charmPower) || sE_Charm.m_ttl < 0))
					{
						//Debug.Log($"Charm released!");
						attacker.m_faction = sE_Charm.originalFaction;
						attacker.SetTamed(tamed: false);
						attacker.GetSEMan().RemoveStatusEffect(sE_Charm, quiet: true);
						StatusEffect statusEffect = (SE_CharmImmunity)ScriptableObject.CreateInstance(typeof(SE_CharmImmunity));
						statusEffect.m_ttl = Mathf.Clamp(attacker.GetHealthPercentage() * VL_GlobalConfigs.g_CooldownModifer * 60f, 5f, 300f);
						attacker.GetSEMan().AddStatusEffect(statusEffect);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), attacker.GetEyePoint(), UnityEngine.Quaternion.identity);
					}
				}
				if (__instance.m_name == "Shadow Wolf" && !BaseAI.IsEnemy(__instance, attacker))
				{
					hit.m_damage.Modify(0.1f);
				}
				Player player = attacker as Player;
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_Weaken".GetStableHashCode()))
				{
					SE_Weaken sE_Weaken = (SE_Weaken)attacker.GetSEMan().GetStatusEffect("SE_VL_Weaken".GetStableHashCode());
					hit.m_damage.Modify(1f - sE_Weaken.damageReduction);
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_ShadowStalk".GetStableHashCode()))
				{
					attacker.GetSEMan().RemoveStatusEffect("SE_VL_ShadowStalk".GetStableHashCode(), quiet: true);
				}

				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_Rogue".GetStableHashCode()))
				{
					Player localPlayer = Player.m_localPlayer;
					ItemDrop.ItemData hasLeftItem = Traverse.Create(localPlayer).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
					ItemDrop.ItemData hasRightItem = Traverse.Create(localPlayer).Field("m_rightItem").GetValue<ItemDrop.ItemData>();
					if (hasRightItem != null)
					{
						if (hasLeftItem != null)
						{
							ItemDrop.ItemData.SharedData sharedL = hasLeftItem.m_shared;
							ItemDrop.ItemData.SharedData sharedR = hasRightItem.m_shared;
							bool isDualWieldingDaggers = (sharedL != null && sharedR != null && (sharedL.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) && (sharedR.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) && (sharedL.m_skillType == sharedR.m_skillType) && (sharedL.m_skillType == Skills.SkillType.Knives));
							if (isDualWieldingDaggers || sharedR.m_name.ToLower().Contains("skoll") || sharedL.m_name.ToLower().Contains("skoll"))
							{
								float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
									.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
								player.RaiseSkill(ValheimLegends.AlterationSkill, 0.001f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f)));
								hit.m_damage.m_poison += (EpicMMOSystem.LevelSystem.Instance.getLevel() * (1f + (level / 80f))) * 0.5f;
							}
						}
						else
						{
							ItemDrop.ItemData.SharedData sharedR = hasRightItem.m_shared;
							bool isSingleWieldingDaggers = (sharedR != null && (sharedR.m_itemType == ItemDrop.ItemData.ItemType.OneHandedWeapon) && (sharedR.m_skillType == Skills.SkillType.Knives));
							if (isSingleWieldingDaggers || sharedR.m_name.ToLower().Contains("skoll"))
							{
								float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AlterationSkillDef)
									.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
								player.RaiseSkill(ValheimLegends.AlterationSkill, 0.001f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f)));
								hit.m_damage.m_poison += (EpicMMOSystem.LevelSystem.Instance.getLevel() * (1f + (level / 80f))) * 0.5f;
							}
						}
					}
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_Monk".GetStableHashCode()))
				{
					if (Class_Monk.PlayerIsUnarmed && (hit.m_damage.m_blunt > 0f || hit.m_damage.m_slash > 0f))
					{
						float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
						float chiDamage = (EpicMMOSystem.LevelSystem.Instance.getLevel() * (1f + (level2 / 80f))) * 0.5f;
                        if (!Class_Monk.PlayerIsBareHanded)
						{
                            chiDamage *= attacker.GetStaminaPercentage();
                        } 
						else
						{
                            hit.m_damage.m_blunt += chiDamage;
                        }
                        hit.m_damage.m_spirit += chiDamage;
						SE_Monk sE_Monk = (SE_Monk)attacker.GetSEMan().GetStatusEffect("SE_VL_Monk".GetStableHashCode());
						sE_Monk.maxHitCount = 5 + Mathf.RoundToInt(0.4f * Mathf.Sqrt(Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f))));
						sE_Monk.hitCount++;
						sE_Monk.hitCount = Mathf.Clamp(sE_Monk.hitCount, 0, sE_Monk.maxHitCount);
						sE_Monk.refreshed = true;
						player.RaiseSkill(ValheimLegends.DisciplineSkill, 0.001f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f)));
					}
				}
                if (attacker.GetSEMan().HaveStatusEffect("SE_VL_DruidFenringForm".GetStableHashCode()))
                {
                    if (Class_Monk.PlayerIsBareHanded && (hit.m_damage.m_blunt > 0f))
                    {
                        float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
                            .m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
                        float clawDamage = (EpicMMOSystem.LevelSystem.Instance.getLevel() * (1f + (level2 / 80f))) * 0.25f;
                        hit.m_damage.m_blunt += clawDamage;
                        hit.m_damage.m_slash += clawDamage;
                        player.RaiseSkill(ValheimLegends.DisciplineSkill, 0.001f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f)));
                    }
                }
                if (attacker.GetSEMan().HaveStatusEffect("SE_VL_Shell".GetStableHashCode()))
				{
					SE_Shell sE_Shell = attacker.GetSEMan().GetStatusEffect("SE_VL_Shell".GetStableHashCode()) as SE_Shell;
					hit.m_damage.m_spirit += sE_Shell.spiritDamageOffset;
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_BiomeMeadows".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
					{
					SE_BiomeMeadows sE_BiomeMeadows = attacker.GetSEMan().GetStatusEffect("SE_VL_BiomeMeadows".GetStableHashCode()) as SE_BiomeMeadows;
						attacker.Heal(hit.m_damage.GetTotalDamage() * attacker.GetHealthPercentage() * sE_BiomeMeadows.lifestealPercent * UnityEngine.Random.Range(0.8f, 1.2f));
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_BiomeBlackForest".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
				{
					SE_BiomeBlackForest sE_BiomeBlackForest = attacker.GetSEMan().GetStatusEffect("SE_VL_BiomeBlackForest".GetStableHashCode()) as SE_BiomeBlackForest;
					if (UnityEngine.Random.value < sE_BiomeBlackForest.critChance)
					{
						hit.ApplyModifier(hit.m_backstabBonus);
						attacker.m_critHitEffects.Create(hit.m_point, UnityEngine.Quaternion.identity, attacker.transform);
						player.Message(MessageHud.MessageType.TopLeft, $"Critical! ({(int)hit.m_backstabBonus}x damage)", 0, null);
					} else if (UnityEngine.Random.value < sE_BiomeBlackForest.critChance && !hit.m_ranged)
					{
						if (ValheimLegends.coinsItem == null)
						{
							ValheimLegends.DefineCoins();
						}
						if (ValheimLegends.coinsItem != null)
						{
							int coinsSpoiled = Mathf.CeilToInt(UnityEngine.Random.Range(0.2f, 0.5f) * Mathf.Sqrt(__instance.GetMaxHealth()));
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_stack_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
							if (player.GetInventory().CanAddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled))
							{
								player.GetInventory().AddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled);
							}
							else
							{
								ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, player.transform.position, UnityEngine.Quaternion.identity);
							}
							player.Message(MessageHud.MessageType.TopLeft, "Snatched " + coinsSpoiled.ToString("#") + " coins from " + __instance.GetHoverName() + "!");
						}
					}
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_BiomeSwamp".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
				{
					SE_BiomeSwamp sE_BiomeSwamp = attacker.GetSEMan().GetStatusEffect("SE_VL_BiomeSwamp".GetStableHashCode()) as SE_BiomeSwamp;
					hit.m_damage.m_poison += Mathf.Clamp(sE_BiomeSwamp.biomeDamageOffset * UnityEngine.Random.Range(0.8f, 1.2f), 0f, hit.m_damage.GetTotalDamage() * 0.5f);
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_BiomeMountain".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
				{
					SE_BiomeMountain sE_BiomeMountain = attacker.GetSEMan().GetStatusEffect("SE_VL_BiomeMountain".GetStableHashCode()) as SE_BiomeMountain;
					hit.m_damage.m_frost += Mathf.Clamp(sE_BiomeMountain.biomeDamageOffset * UnityEngine.Random.Range(0.9f, 1.1f), 0f, hit.m_damage.GetTotalDamage() * 0.5f);
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_BiomePlains".GetStableHashCode()) && attacker.IsPlayer() && hit.GetTotalDamage() > 0f)
				{
					SE_BiomePlains sE_BiomePlains = attacker.GetSEMan().GetStatusEffect("SE_VL_BiomePlains".GetStableHashCode()) as SE_BiomePlains;
					if (player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()) && UnityEngine.Random.value < sE_BiomePlains.cooldownChance)
					{
						StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_Ability1_CD".GetStableHashCode());
						statusEffect.m_ttl--;
					}
					if (player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()) && UnityEngine.Random.value < sE_BiomePlains.cooldownChance)
					{
						StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_Ability2_CD".GetStableHashCode());
						statusEffect.m_ttl--;
					}
					if (player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()) && UnityEngine.Random.value < sE_BiomePlains.cooldownChance)
					{
						StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_Ability3_CD".GetStableHashCode());
						statusEffect.m_ttl--;
					}
					if (player.GetSEMan().HaveStatusEffect("SE_VL_DyingLight_CD".GetStableHashCode()) && UnityEngine.Random.value < sE_BiomePlains.cooldownChance)
					{
						StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_DyingLight_CD".GetStableHashCode());
						statusEffect.m_ttl--;
					}
					if (player.GetSEMan().HaveStatusEffect("SE_VL_CDReactivearmor".GetStableHashCode()) && UnityEngine.Random.value < sE_BiomePlains.cooldownChance)
					{
						StatusEffect statusEffect = player.GetSEMan().GetStatusEffect("SE_VL_CDReactivearmor".GetStableHashCode());
						statusEffect.m_ttl--;
					}
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_BiomeOcean".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
				{
					SE_BiomeOcean sE_BiomeOcean = attacker.GetSEMan().GetStatusEffect("SE_VL_BiomeOcean".GetStableHashCode()) as SE_BiomeOcean;
					hit.m_damage.m_spirit += Mathf.Clamp(sE_BiomeOcean.biomeDamageOffset * UnityEngine.Random.Range(0.5f, 2.0f), 0f, hit.m_damage.GetTotalDamage() * 0.5f);
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_BiomeMist".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
				{
					SE_BiomeMist sE_BiomeMist = attacker.GetSEMan().GetStatusEffect("SE_VL_BiomeMist".GetStableHashCode()) as SE_BiomeMist;
					hit.m_damage.m_lightning += Mathf.Clamp(sE_BiomeMist.biomeDamageOffset * UnityEngine.Random.Range(0.25f, 1.75f), 0f, hit.m_damage.GetTotalDamage() * 0.5f);
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_BiomeAsh".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
				{
					SE_BiomeAsh sE_BiomeAsh = attacker.GetSEMan().GetStatusEffect("SE_VL_BiomeAsh".GetStableHashCode()) as SE_BiomeAsh;
					hit.m_damage.m_fire += Mathf.Clamp(sE_BiomeAsh.biomeDamageOffset * UnityEngine.Random.Range(0.5f, 1.5f), 0f, hit.m_damage.GetTotalDamage() * 0.5f);
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_Berserk".GetStableHashCode()))
				{
					SE_Berserk sE_Berserk = attacker.GetSEMan().GetStatusEffect("SE_VL_Berserk".GetStableHashCode()) as SE_Berserk;
					attacker.AddStamina(hit.GetTotalDamage() * sE_Berserk.healthAbsorbPercent);
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_Execute".GetStableHashCode()))
				{
					SE_Execute sE_Execute = attacker.GetSEMan().GetStatusEffect("SE_VL_Execute".GetStableHashCode()) as SE_Execute;
					hit.m_staggerMultiplier *= sE_Execute.staggerForce;
					hit.m_damage.m_blunt *= sE_Execute.damageBonus;
					hit.m_damage.m_pierce *= sE_Execute.damageBonus;
					hit.m_damage.m_slash *= sE_Execute.damageBonus;
					sE_Execute.hitCount--;
					if (sE_Execute.hitCount <= 0)
					{
						attacker.GetSEMan().RemoveStatusEffect(sE_Execute, quiet: true);
					}
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_Companion".GetStableHashCode()))
				{
					SE_Companion sE_Companion = attacker.GetSEMan().GetStatusEffect("SE_VL_Companion".GetStableHashCode()) as SE_Companion;
					hit.m_damage.Modify(sE_Companion.damageModifier);
				}
				if (attacker.GetSEMan().HaveStatusEffect("SE_VL_RootsBuff".GetStableHashCode()))
				{
					SE_RootsBuff sE_RootsBuff = attacker.GetSEMan().GetStatusEffect("SE_VL_RootsBuff".GetStableHashCode()) as SE_RootsBuff;
					hit.m_damage.Modify(sE_RootsBuff.damageModifier);
				}

				if (vl_player != null && player != null && vl_player.vl_name == player.GetPlayerName())
				{
					if (vl_player.vl_class == ValheimLegends.PlayerClass.Duelist)
					{
						ItemDrop.ItemData hasLeftItem = Traverse.Create(player).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
						if (hasLeftItem == null && player.GetCurrentWeapon() != null && player.GetCurrentWeapon().m_shared.m_itemType != ItemDrop.ItemData.ItemType.TwoHandedWeapon && (player.GetCurrentWeapon().m_shared.m_skillType == hit.m_skill) && (player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Swords || player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Knives || player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Axes || player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Spears))
						{
							if (UnityEngine.Random.value < ((5f + EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance()) / 100f))
							{
								hit.ApplyModifier(hit.m_backstabBonus * 0.5f);
								attacker.m_critHitEffects.Create(hit.m_point, UnityEngine.Quaternion.identity, attacker.transform);
								player.Message(MessageHud.MessageType.TopLeft, $"Melee critical! ({((int)(hit.m_backstabBonus * 10) * 0.05f)}x damage)", 0, null);
							}
						}
					}
					if (vl_player.vl_class == ValheimLegends.PlayerClass.Priest)
					{
						ItemDrop.ItemData hasLeftItem = Traverse.Create(player).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
						if (player.GetCurrentWeapon() != null && player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Clubs && (player.GetCurrentWeapon().m_shared.m_skillType == hit.m_skill))
						{
							float level2 = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
								.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
							hit.m_damage.m_spirit += (EpicMMOSystem.LevelSystem.Instance.getLevel() * (1f + (level2 / 80f))) * 0.5f;
							player.RaiseSkill(ValheimLegends.DisciplineSkill, 0.001f * VL_GlobalConfigs.g_SkillGainModifer * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 16f)));
						}
					}
                    if (vl_player.vl_class == ValheimLegends.PlayerClass.Shaman && !Class_Shaman.gotWindfuryCooldown)
                    {
                        // item equipado na mão principal
                        ItemDrop.ItemData rightHand = player.GetCurrentWeapon();

                        if (rightHand != null && rightHand.IsWeapon() && rightHand.m_shared.m_name.ToLower() != "unarmed" && !hit.m_ranged)
                        {
                            float level2 = player.GetSkills().GetSkillList()
                                .FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
                                .m_level * (1f + Mathf.Clamp(
                                    (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) +
                                    (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f),
                                    0f, 0.5f));

                            if (UnityEngine.Random.value < (0.05f + (level2 / 800f)))
                            {
                                HitData hitData2 = new HitData();
                                hitData2.m_pushForce = rightHand.GetDeflectionForce();
                                hitData2.m_dir = attacker.transform.position - __instance.transform.position;
                                hitData2.m_dir.y = 0f;
                                hitData2.m_dir.Normalize();
                                hitData2.m_point = attacker.GetEyePoint();
                                hitData2.m_damage = hit.m_damage;
                                hitData2.ApplyModifier(0.3f + (level2 / 160));

                                UnityEngine.Object.Instantiate(
                                    ZNetScene.instance.GetPrefab("fx_VL_ParticleLightburst"),
                                    player.GetEyePoint(),
                                    UnityEngine.Quaternion.LookRotation(player.GetLookDir())
                                );

                                UnityEngine.Object.Instantiate(
                                    ZNetScene.instance.GetPrefab("fx_VL_Shock"),
                                    player.GetEyePoint() + player.GetLookDir() * 2.5f + player.transform.right * 0.25f,
                                    UnityEngine.Quaternion.LookRotation(player.GetLookDir())
                                );

                                __instance.Damage(hitData2);
                                __instance.Damage(hitData2);

                                StatusEffect statusEffectW =
                                    (SE_Windfury_CD)ScriptableObject.CreateInstance(typeof(SE_Windfury_CD));
                                player.GetSEMan().AddStatusEffect(statusEffectW);

                                Class_Shaman.gotWindfuryCooldown = true;
                            }
                        }
                    }

                    if (vl_player.vl_class == ValheimLegends.PlayerClass.Rogue && attacker.GetHoverName() == vl_player.vl_name && !hit.m_ranged)
					{
						Player localPlayer = Player.m_localPlayer;
						if (localPlayer.GetCurrentWeapon() != null)
						{
							ItemDrop.ItemData value = Traverse.Create(localPlayer).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
							ItemDrop.ItemData.SharedData shared = localPlayer.GetCurrentWeapon().m_shared;
							if (shared != null && (shared.m_name.ToLower() == "unarmed" || shared.m_attachOverride == ItemDrop.ItemData.ItemType.Hands) && value == null)
							{
								SE_Rogue sE_Rogue = (SE_Rogue)localPlayer.GetSEMan().GetStatusEffect("SE_VL_Rogue".GetStableHashCode());
								if (sE_Rogue.hitCount > 0 && (sE_Rogue.lastSnatched == null || !sE_Rogue.lastSnatched.Contains(__instance.GetInstanceID())))
								{
									if (ValheimLegends.coinsItem == null)
									{
										ValheimLegends.DefineCoins();
									}
									if (ValheimLegends.coinsItem != null)
									{
										int coinsSpoiled = Mathf.CeilToInt(UnityEngine.Random.Range(0.33f, 1f) * (1f + (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f)) * Mathf.Sqrt(__instance.GetMaxHealth()));
										if (coinsSpoiled < EpicMMOSystem.LevelSystem.Instance.getLevel())
										{
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_stack_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
										}
										else
										{
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_pile_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_pile_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
										}
										if (localPlayer.GetInventory().CanAddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled))
										{
											localPlayer.GetInventory().AddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled);
										}
										else
										{
											ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, localPlayer.transform.position, UnityEngine.Quaternion.identity);
										}
										localPlayer.Message(MessageHud.MessageType.TopLeft, "Snatched " + coinsSpoiled.ToString("#") + " coins from " + __instance.GetHoverName() + "!");
										sE_Rogue.hitCount--;
										if (sE_Rogue.lastSnatched == null)
										{
											sE_Rogue.lastSnatched = new List<int>();
											sE_Rogue.lastSnatched.Clear();
										}
										sE_Rogue.lastSnatched.Add(__instance.GetInstanceID());
									}
								}
							}
						}
					}
					if (vl_player.vl_class == ValheimLegends.PlayerClass.Duelist && attacker.GetHoverName() == vl_player.vl_name && !hit.m_ranged && __instance.IsStaggering())
					{
						if (Class_Duelist.challengedMastery != null && Class_Duelist.challengedMastery.Contains(__instance.GetInstanceID()))
						{
							Class_Duelist.challengedMastery.Remove(__instance.GetInstanceID());
							int coinsSpoiled = Mathf.CeilToInt(Mathf.Sqrt(__instance.GetMaxHealth()) + ((1f + (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f)) * Mathf.Sqrt(__instance.GetMaxHealth())));
							if (coinsSpoiled < EpicMMOSystem.LevelSystem.Instance.getLevel())
							{
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_destroyed"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_stack_destroyed"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
							}
							else
							{
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_pile_destroyed"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_pile_destroyed"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
							}
							if (player.GetInventory().CanAddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled))
							{
								player.GetInventory().AddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled);
							}
							else
							{
								ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, player.transform.position, UnityEngine.Quaternion.identity);
							}
							player.Message(MessageHud.MessageType.TopLeft, "Spoiled " + coinsSpoiled.ToString("#") + " coins from " + __instance.GetHoverName() + "!");
                            //ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, localPlayer.transform.position, UnityEngine.Quaternion.identity);
                        }
                    }
					if (vl_player.vl_class != ValheimLegends.PlayerClass.Shaman)
                    {
						Class_Shaman.gotWindfuryCooldown = false;
						if (player.GetSEMan().HaveStatusEffect("SE_VL_Windfury_CD".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_Windfury_CD".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
					}
					if (vl_player.vl_class != ValheimLegends.PlayerClass.Metavoker)
					{
						if (player.GetSEMan().HaveStatusEffect("SE_VL_CDReactivearmor".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_CDReactivearmor".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
					}
					if (vl_player.vl_class == ValheimLegends.PlayerClass.Ranger)
					{
						ItemDrop.ItemData hasLeftItem = Traverse.Create(player).Field("m_leftItem").GetValue<ItemDrop.ItemData>();
						if (player.GetCurrentWeapon() != null && (player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Bows || (player.GetCurrentWeapon().m_shared.m_skillType == Skills.SkillType.Spears && hit.m_ranged)) && (player.GetCurrentWeapon().m_shared.m_skillType == hit.m_skill))
						{
							if (UnityEngine.Random.value < ((5f + EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance()) / 100f))
							{
								hit.ApplyModifier(hit.m_backstabBonus);
								attacker.m_critHitEffects.Create(hit.m_point, UnityEngine.Quaternion.identity, attacker.transform);
								player.Message(MessageHud.MessageType.TopLeft, $"Ranged critical! ({(int)hit.m_backstabBonus}x damage)", 0, null);
							}
						}
					}
					if (vl_player.vl_class == PlayerClass.Berserker)
					{
						hit.m_damage.Modify(Mathf.Clamp(1f + (1f - (float)Math.Sqrt(attacker.GetHealthPercentage())) * VL_GlobalConfigs.c_berserkerBonusDamage, 1f, 2.0f));
					}
                    else if (vl_player.vl_class == PlayerClass.Enchanter)
                    {
                        // Remove any "Enchant ..." temporary debuff/buff (separate from Weapon/Armor imbues)
                        StatusEffect statusEffect = Class_Enchanter.HasEnchantBuff(player);
                        if (statusEffect != null)
                        {
                            StatusEffect oldStatus = player.GetSEMan().GetStatusEffect(statusEffect.name.GetStableHashCode());
                            player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
                        }

                        float evocationLevel = player.GetSkills().GetSkillList()
                            .FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
                            .m_level * (1f + Mathf.Clamp(
                                (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) +
                                (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f),
                                0f, 0.5f));

                        long pid = player.GetPlayerID();

                        if (player.GetSEMan().HaveStatusEffect("SE_VL_FlameWeapon".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
                        {
                            // reset outras cargas para evitar "mix"
                            SetEnchanterWeaponCharges(pid, EnchanterWeaponElement.Ice, 0);
                            SetEnchanterWeaponCharges(pid, EnchanterWeaponElement.Thunder, 0);

                            int stacks = AddEnchanterWeaponCharges(pid, EnchanterWeaponElement.Flame, 1);
                            UpdateEnchanterWeaponSEName(player, EnchanterWeaponElement.Flame, stacks, __instance.GetEyePoint());

                            // bônus de dano original
                            hit.m_damage.m_fire += (EpicMMOSystem.LevelSystem.Instance.getLevel() / 6f) *
                                UnityEngine.Random.Range(1.0f, 2.0f) *
                                (1f + (evocationLevel / 150f));
                            player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.03f);

                        }
                        // ============================================================
                        // ICE WEAPON: stacks aumentam chance de proc até 100% com 10 cargas.
                        // (Ao procar, zera as cargas)
                        // ============================================================
                        else if (player.GetSEMan().HaveStatusEffect("SE_VL_IceWeapon".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
                        {
                            SetEnchanterWeaponCharges(pid, EnchanterWeaponElement.Flame, 0);
                            SetEnchanterWeaponCharges(pid, EnchanterWeaponElement.Thunder, 0);

                            int stacks = AddEnchanterWeaponCharges(pid, EnchanterWeaponElement.Ice, 1);
                            UpdateEnchanterWeaponSEName(player, EnchanterWeaponElement.Ice, stacks, __instance.GetEyePoint());

                            SE_Slow sE_Slow = (SE_Slow)ScriptableObject.CreateInstance(typeof(SE_Slow));
                            sE_Slow.m_ttl = 4f + 6f * evocationLevel;
                            sE_Slow.speedAmount = 0.7f - (evocationLevel / 250f);

                            // bônus de dano original
                            hit.m_damage.m_frost += (EpicMMOSystem.LevelSystem.Instance.getLevel() / 6f) *
                                UnityEngine.Random.Range(0.5f, 1.5f) *
                                (1f + (evocationLevel / 150f));
                            player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.03f);
                        }
                        // ============================================================
                        // THUNDER WEAPON: cada hit acumula 1 carga. 
                        // (O proc original de chain lightning continua existindo)
                        // ============================================================
                        else if (player.GetSEMan().HaveStatusEffect("SE_VL_ThunderWeapon".GetStableHashCode()) && hit.GetTotalDamage() > 0f)
                        {
                            SetEnchanterWeaponCharges(pid, EnchanterWeaponElement.Flame, 0);
                            SetEnchanterWeaponCharges(pid, EnchanterWeaponElement.Ice, 0);

                            int stacks = AddEnchanterWeaponCharges(pid, EnchanterWeaponElement.Thunder, 1);
                            UpdateEnchanterWeaponSEName(player, EnchanterWeaponElement.Thunder, stacks, __instance.GetEyePoint());

                            // bônus de dano original
                            hit.m_damage.m_lightning += (EpicMMOSystem.LevelSystem.Instance.getLevel() / 8f) *
                                UnityEngine.Random.Range(0.5f, 1.5f) *
                                (1f + (evocationLevel / 150f));
                            player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.03f);

                            UnityEngine.Vector3 Vector3 = player.GetEyePoint() + player.GetLookDir() * 4f;
                            List<Character> list = new List<Character>();
                            list.Clear();
                            Character.GetCharactersInRange(Vector3, 4f, list);
                            foreach (Character item in list)
                            {
                                float chainDamage = 0.7f;
                                if (BaseAI.IsEnemy(player, item) && VL_Utility.LOS_IsValid(item, Vector3, Vector3))
                                {
                                    UnityEngine.Vector3 dir = item.transform.position - player.transform.position;
                                    HitData hitData2 = new HitData();
                                    hitData2.m_damage.m_lightning = (EpicMMOSystem.LevelSystem.Instance.getLevel() / 4f) *
                                        UnityEngine.Random.Range(0.1f, 1.9f) *
                                        (1f + (evocationLevel / 150f)) *
                                        chainDamage;
                                    hitData2.m_pushForce = 0f;
                                    hitData2.m_point = item.GetEyePoint();
                                    hitData2.m_dir = dir;
                                    hitData2.m_skill = ValheimLegends.EvocationSkill;
                                    item.Damage(hitData2);
                                    chainDamage *= 0.7f;
                                    player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.015f);
                                }
                            }
                        }
                    }
                    else
                    {
						if (player.GetSEMan().HaveStatusEffect("SE_VL_Fireaffinity".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_Fireaffinity".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
						else if (player.GetSEMan().HaveStatusEffect("SE_VL_Frostaffinity".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_Frostaffinity".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
						else if (player.GetSEMan().HaveStatusEffect("SE_VL_Lightningaffinity".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_Lightningaffinity".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
						else if (player.GetSEMan().HaveStatusEffect("SE_VL_FlameWeapon".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_FlameWeapon".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
						else if (player.GetSEMan().HaveStatusEffect("SE_VL_IceWeapon".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_IceWeapon".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
						else if (player.GetSEMan().HaveStatusEffect("SE_VL_ThunderWeapon".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_ThunderWeapon".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
						else if (player.GetSEMan().HaveStatusEffect("SE_VL_FlameArmor".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_FlameArmor".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
						else if (player.GetSEMan().HaveStatusEffect("SE_VL_IceArmor".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_IceArmor".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
						else if (player.GetSEMan().HaveStatusEffect("SE_VL_ThunderArmor".GetStableHashCode()))
						{
							StatusEffect oldStatus = player.GetSEMan().GetStatusEffect("SE_VL_ThunderArmor".GetStableHashCode());
							player.GetSEMan().RemoveStatusEffect(oldStatus, quiet: true);
						}
					}
				}
			}
			return true;
		}
	}

    // ============================================================
    // Enchanter weapon charge tracking (non-persistent)
    // ============================================================
    private static readonly Dictionary<long, int> _enchFlameCharges = new();
    private static readonly Dictionary<long, int> _enchIceCharges = new();
    private static readonly Dictionary<long, int> _enchThunderCharges = new();

    public enum EnchanterWeaponElement
    {
        Flame,
        Ice,
        Thunder
    }

    public static int GetEnchanterWeaponCharges(Player player, EnchanterWeaponElement element)
    {
        if (player == null) return 0;
        long pid = player.GetPlayerID();
        return element switch
        {
            EnchanterWeaponElement.Flame => _enchFlameCharges.TryGetValue(pid, out var a) ? a : 0,
            EnchanterWeaponElement.Ice => _enchIceCharges.TryGetValue(pid, out var b) ? b : 0,
            EnchanterWeaponElement.Thunder => _enchThunderCharges.TryGetValue(pid, out var c) ? c : 0,
            _ => 0
        };
    }

    private static void SetEnchanterWeaponCharges(long pid, EnchanterWeaponElement element, int value)
    {
        value = Mathf.Clamp(value, 0, 100);
        switch (element)
        {
            case EnchanterWeaponElement.Flame: _enchFlameCharges[pid] = value; break;
            case EnchanterWeaponElement.Ice: _enchIceCharges[pid] = value; break;
            case EnchanterWeaponElement.Thunder: _enchThunderCharges[pid] = value; break;
        }
    }

    private static int AddEnchanterWeaponCharges(long pid, EnchanterWeaponElement element, int add = 1)
    {
        int cur = element switch
        {
            EnchanterWeaponElement.Flame => _enchFlameCharges.TryGetValue(pid, out var a) ? a : 0,
            EnchanterWeaponElement.Ice => _enchIceCharges.TryGetValue(pid, out var b) ? b : 0,
            EnchanterWeaponElement.Thunder => _enchThunderCharges.TryGetValue(pid, out var c) ? c : 0,
            _ => 0
        };
        int v = Mathf.Clamp(cur + add, 0, 100);
        SetEnchanterWeaponCharges(pid, element, v);
        return v;
    }

    private static void ClearEnchanterWeaponCharges(long pid)
    {
        _enchFlameCharges.Remove(pid);
        _enchIceCharges.Remove(pid);
        _enchThunderCharges.Remove(pid);
    }

	private static void UpdateEnchanterWeaponSEName(Player player, EnchanterWeaponElement element, int stacks, UnityEngine.Vector3 location)
	{
		if (player == null) return;

		player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.03f);

		int hash = element switch
		{
			EnchanterWeaponElement.Flame => "SE_VL_FlameWeapon".GetStableHashCode(),
			EnchanterWeaponElement.Ice => "SE_VL_IceWeapon".GetStableHashCode(),
			EnchanterWeaponElement.Thunder => "SE_VL_ThunderWeapon".GetStableHashCode(),
			_ => 0
		};
		if (hash == 0) return;

		StatusEffect se = player.GetSEMan()?.GetStatusEffect(hash);
		if (se == null) return;

		string baseName = element switch
		{
			EnchanterWeaponElement.Flame => "Flame Weapon",
			EnchanterWeaponElement.Ice => "Ice Weapon",
			EnchanterWeaponElement.Thunder => "Thunder Weapon",
			_ => se.m_name
		};
		float evocationLevel = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef)
			.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
		float abjurationLevel = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.AbjurationSkillDef)
			.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddHp() / 400f) + (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f), 0f, 0.5f));
		float procChance = Mathf.Clamp(stacks, 0, 100) + (1f + (evocationLevel / 100f));

		se.m_name = $"{baseName}: {procChance} %";

		if (UnityEngine.Random.RandomRangeInt(0, 100) <= procChance)
		{
			if (element == EnchanterWeaponElement.Flame)
			{
				UnityEngine.Vector3 Vector3 = player.transform.position;
				List<Character> list = new List<Character>();
				list.Clear();
				Character.GetCharactersInRange(Vector3, 30f, list);
				foreach (Character item in list)
				{
					if (!BaseAI.IsEnemy(player, item) || item.IsPlayer())
					{
						float heal = 5f + (EpicMMOSystem.LevelSystem.Instance.getLevel() * 0.25f * (1f + (abjurationLevel / 150f)));
                        item.Heal(heal, showText: true);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_health_medium"), item.GetCenterPoint(), UnityEngine.Quaternion.identity);
                        ReduceAllCooldowns(item, 0.9f);
                    }
				}
                Vector3 = location;
                list.Clear();
                Character.GetCharactersInRange(Vector3, 15f, list);
                foreach (Character item in list)
                {
                    if (BaseAI.IsEnemy(player, item) && VL_Utility.LOS_IsValid(item, Vector3))
                    {
                        UnityEngine.Vector3 dir = item.transform.position - location;
                        HitData hitData = new HitData();
                        hitData.m_damage.m_fire = (10f + (EpicMMOSystem.LevelSystem.Instance.getLevel()) * UnityEngine.Random.Range(1.0f, 2.0f) * (1f + (evocationLevel / 150f))) * VL_GlobalConfigs.g_DamageModifer;
                        hitData.m_pushForce = 0f;
                        hitData.m_point = item.GetEyePoint();
                        hitData.m_dir = dir;
                        hitData.m_skill = ValheimLegends.EvocationSkill;
                        item.Damage(hitData);
                        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_CinderFire_Burn"), item.transform.position, UnityEngine.Quaternion.identity);
                    }
                }
                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_fireball_staff_explosion"), location, UnityEngine.Quaternion.identity);
                SetEnchanterWeaponCharges(player.GetPlayerID(), EnchanterWeaponElement.Flame, 0);
				UpdateEnchanterWeaponSEName(player, EnchanterWeaponElement.Flame, 0, location);
			}
			else if (element == EnchanterWeaponElement.Ice)
			{
				UnityEngine.Vector3 Vector3 = player.transform.position;
				List<Character> list = new List<Character>();
				list.Clear();
				Character.GetCharactersInRange(Vector3, 30f, list);
				foreach (Character item in list)
				{
					if (!BaseAI.IsEnemy(player, item) || item.IsPlayer())
					{
						float eitr = 10f + (EpicMMOSystem.LevelSystem.Instance.getLevel() * 0.5f * (1f + (abjurationLevel / 75f)));
						item.AddEitr(eitr);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_Potion_frostresist"), item.GetCenterPoint(), UnityEngine.Quaternion.identity);
                        ReduceAllCooldowns(item, 0.9f);
                        if (item.GetSEMan().HaveStatusEffect("Burning".GetStableHashCode()))
                        {
                            item.GetSEMan().RemoveStatusEffect("Burning".GetStableHashCode());
                        }
                    }
                }
                Vector3 = location;
                list.Clear();
                Character.GetCharactersInRange(Vector3, 15f, list);
                foreach (Character item in list)
                {
                    if (BaseAI.IsEnemy(player, item) && VL_Utility.LOS_IsValid(item, Vector3))
                    {
                        UnityEngine.Vector3 dir = item.transform.position - location;
                        HitData hitData = new HitData();
                        hitData.m_damage.m_frost = (10f + (EpicMMOSystem.LevelSystem.Instance.getLevel()) * UnityEngine.Random.Range(0.5f, 1.5f) * (1f + (evocationLevel / 150f))) * VL_GlobalConfigs.g_DamageModifer;
                        hitData.m_pushForce = 0f;
                        hitData.m_point = item.GetEyePoint();
                        hitData.m_dir = dir;
                        hitData.m_skill = ValheimLegends.EvocationSkill;
                        item.Damage(hitData);
                        SE_Slow sE_Slow = (SE_Slow)ScriptableObject.CreateInstance(typeof(SE_Slow));
                        sE_Slow.m_ttl = 4f + 6f * (evocationLevel);
                        sE_Slow.speedAmount = 0.01f;
                        item.GetSEMan().AddStatusEffect(sE_Slow.name.GetStableHashCode(), resetTime: true);
                        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_DvergerMage_Ice_hit"), item.transform.position, UnityEngine.Quaternion.identity);
                    }
                }
                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_guardstone_activate"), location, UnityEngine.Quaternion.identity);
                SetEnchanterWeaponCharges(player.GetPlayerID(), EnchanterWeaponElement.Ice, 0);
				UpdateEnchanterWeaponSEName(player, EnchanterWeaponElement.Ice, 0, location);
			}
			else if (element == EnchanterWeaponElement.Thunder)
			{
				UnityEngine.Vector3 Vector3 = player.transform.position;
				List<Character> list = new List<Character>();
				list.Clear();
				Character.GetCharactersInRange(Vector3, 30f, list);
                foreach (Character item in list)
				{
					if ((!BaseAI.IsEnemy(player, item) || item.IsPlayer()))

                    {
						float stamina = 10f + (EpicMMOSystem.LevelSystem.Instance.getLevel() * 0.5f * (1f + (abjurationLevel / 75f)));
                        item.AddStamina(stamina);
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), item.GetCenterPoint(), UnityEngine.Quaternion.identity);
						ReduceAllCooldowns(item, 0.9f);
                    }
				}
                Vector3 = location;
                list.Clear();
                Character.GetCharactersInRange(Vector3, 30f, list);
                float chainDamage = 0.7f;
                foreach (Character item in list)
                {
					if (BaseAI.IsEnemy(player, item) && VL_Utility.LOS_IsValid(item, Vector3))
                    {
                        UnityEngine.Vector3 dir = location - player.transform.position;
                        HitData hitData2 = new HitData();
                        hitData2.m_damage.m_lightning = (10f + (EpicMMOSystem.LevelSystem.Instance.getLevel()) * UnityEngine.Random.Range(0.8f, 1.8f) * (1f + (evocationLevel / 150f))) * chainDamage * VL_GlobalConfigs.g_DamageModifer;
                        hitData2.m_pushForce = 0f;
                        hitData2.m_point = item.GetEyePoint();
                        hitData2.m_dir = dir;
                        hitData2.m_skill = ValheimLegends.EvocationSkill;
                        item.Damage(hitData2);
                        chainDamage = chainDamage * 0.7f;
                        player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.015f);
                        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_chainlightning_hit"), item.transform.position, UnityEngine.Quaternion.identity);
                    }
                }
                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_eikthyr_stomp"), location, UnityEngine.Quaternion.identity);
                //GameCamera.instance.AddShake(location, 15f, 2f, continous: false);
                SetEnchanterWeaponCharges(player.GetPlayerID(), EnchanterWeaponElement.Thunder, 0);
				UpdateEnchanterWeaponSEName(player, EnchanterWeaponElement.Thunder, 0, location);
			}
		}
	}		

    public static void ReduceAllCooldowns(Character player, float multiplier)
    {
        // multiplier 0.9f = reduz 10% do restante
        if (player == null) return;
        if (!player.IsPlayer()) return;

        int cd1 = "SE_VL_Ability1_CD".GetStableHashCode();
        int cd2 = "SE_VL_Ability2_CD".GetStableHashCode();
        int cd3 = "SE_VL_Ability3_CD".GetStableHashCode();

        StatusEffect se1 = player.GetSEMan().GetStatusEffect(cd1);
        if (se1 != null) se1.m_ttl *= multiplier;

        StatusEffect se2 = player.GetSEMan().GetStatusEffect(cd2);
        if (se2 != null) se2.m_ttl *= multiplier;

        StatusEffect se3 = player.GetSEMan().GetStatusEffect(cd3);
        if (se3 != null) se3.m_ttl *= multiplier;
    }


    [HarmonyPatch(typeof(Player), "TeleportTo", null)]
	public static class DestroySummonedWhenTeleporting_Patch
	{
		[HarmonyPriority(800)]
		public static bool Prefix(Player __instance)
		{
			RemoveSummonedWolf();
			return true;
		}
	}

	[HarmonyPatch(typeof(Attack), "DoMeleeAttack", null)]
	public class MeleeAttack_Patch
	{
		public static bool Prefix(Attack __instance, Humanoid ___m_character, ref float ___m_damageMultiplier)
		{
			if (___m_character.GetSEMan().HaveStatusEffect("SE_VL_Berserk".GetStableHashCode()))
			{
				SE_Berserk sE_Berserk = (SE_Berserk)___m_character.GetSEMan().GetStatusEffect("SE_VL_Berserk".GetStableHashCode());
				___m_damageMultiplier = sE_Berserk.damageModifier;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Attack), "FireProjectileBurst", null)]
	public class ProjectileAttack_Prefix
	{
		public static bool Prefix(Attack __instance, Humanoid ___m_character, ref float ___m_attackDrawPercentage, ref float ___m_projectileVel, ref float ___m_forceMultiplier, ref float ___m_staggerMultiplier, ref float ___m_damageMultiplier, ref float ___m_projectileAccuracy, ref float ___m_projectileAccuracyMin, ref float ___m_projectileVelMin, ref ItemDrop.ItemData ___m_weapon)
		{
			if (___m_character.GetSEMan().HaveStatusEffect("SE_VL_PowerShot".GetStableHashCode()))
			{
				___m_projectileVel *= 2f;
				___m_damageMultiplier = 1.4f * VL_GlobalConfigs.c_rangerPowerShot + ___m_character.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == DisciplineSkillDef)
					.m_level * 0.015f * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
				SE_PowerShot sE_PowerShot = ___m_character.GetSEMan().GetStatusEffect("SE_VL_PowerShot".GetStableHashCode()) as SE_PowerShot;
				sE_PowerShot.hitCount--;
				if (sE_PowerShot.hitCount <= 0)
				{
					___m_character.GetSEMan().RemoveStatusEffect(sE_PowerShot, quiet: true);
				}
			}
			if (___m_character.GetSEMan().HaveStatusEffect("SE_VL_Ranger".GetStableHashCode()))
			{
				SE_Ranger sE_Ranger = (SE_Ranger)___m_character.GetSEMan().GetStatusEffect("SE_VL_Ranger".GetStableHashCode());
				if (sE_Ranger.hitCount > 0f)
				{
					___m_attackDrawPercentage = 0.9f;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Projectile), "OnHit", null)]
	public class Projectile_Hit_Patch
	{
		public static void Postfix(Projectile __instance, Collider collider, UnityEngine.Vector3 hitPoint, bool water, float ___m_aoe, int ___s_rayMaskSolids, Character ___m_owner, UnityEngine.Vector3 ___m_vel)
		{

            // ===== DRUID ROOT HIT =====
            if (__instance != null && (__instance.name == "VL_DruidRoot" || __instance.name == "Root"))
            {
                // pega o alvo acertado
                GameObject hitObj = Projectile.FindHitObject(collider);
                if (hitObj != null)
                {
                    Character target =
                        hitObj.GetComponent<Character>() ??
                        hitObj.GetComponentInParent<Character>();

                    // aplica apenas em criaturas (não players) e apenas se tiver SEMan
                    if (target != null && !target.IsPlayer() && target.GetSEMan() != null)
                    {
                        // (opcional) se quiser evitar bosses serem travados:
                        if (!target.m_boss)
                        {
                            var rooted = (SE_Rooted)ScriptableObject.CreateInstance(typeof(SE_Rooted));
                            rooted.m_ttl = 1f;
                            target.GetSEMan().AddStatusEffect(rooted, resetTime: true);
                        }
                    }
                }

                // reduzir CD da Ability 1 do Druida em 1% por hit
                if (___m_owner is Player ownerPlayer && ownerPlayer.GetSEMan() != null)
                {
                    int cdHash = "SE_VL_Ability1_CD".GetStableHashCode();
                    StatusEffect cd = ownerPlayer.GetSEMan().GetStatusEffect(cdHash);

                    if (cd != null)
                    {
                        cd.m_ttl *= 0.99f; // -1%

                        // opcional: se ficar muito pequeno, remove o CD
                        if (cd.m_ttl <= 0.05f)
                        {
                            ownerPlayer.GetSEMan().RemoveStatusEffect(cdHash);
                        }
                    }
                    cdHash = "SE_VL_Ability2_CD".GetStableHashCode();
                    cd = ownerPlayer.GetSEMan().GetStatusEffect(cdHash);

                    if (cd != null)
                    {
                        cd.m_ttl *= 0.99f; // -1%

                        // opcional: se ficar muito pequeno, remove o CD
                        if (cd.m_ttl <= 0.05f)
                        {
                            ownerPlayer.GetSEMan().RemoveStatusEffect(cdHash);
                        }
                    }
                }

                return; // não deixa cair no resto do patch
            }
            if (__instance.name == "VL_Charm")
			{
				bool hitCharacter = false;
				if (!(__instance.m_aoe > 0f))
				{
					return;
				}
				Collider[] array = Physics.OverlapSphere(hitPoint, __instance.m_aoe, ___s_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
				HashSet<GameObject> hashSet = new HashSet<GameObject>();
				Collider[] array2 = array;
				Collider[] array3 = array2;
				foreach (Collider collider2 in array3)
				{
					GameObject gameObject = Projectile.FindHitObject(collider2);
					IDestructible component = gameObject.GetComponent<IDestructible>();
					if (component == null || hashSet.Contains(gameObject))
					{
						continue;
					}
					hashSet.Add(gameObject);
					if (IsValidTarget(component, ref hitCharacter, ___m_owner, __instance.m_dodgeable))
					{
						Character component2 = null;
						gameObject.TryGetComponent<Character>(out component2);
						bool flag = component2 != null;
						if (component2 == null)
						{
							component2 = (Character)gameObject.GetComponentInParent(typeof(Character));
							flag = component2 != null;
						}
						if (flag && !component2.IsPlayer() && ___m_owner is Player && !component2.m_boss && component2.GetSEMan() != null && !component2.GetSEMan().HaveStatusEffect("SE_VL_CharmImmunity".GetStableHashCode()))
						{
							Player player = ___m_owner as Player;
							SE_Charm sE_Charm = (SE_Charm)ScriptableObject.CreateInstance(typeof(SE_Charm));
							sE_Charm.m_ttl = SE_Charm.m_baseTTL * VL_GlobalConfigs.c_enchanterCharm;
							sE_Charm.summoner = player;
							sE_Charm.originalFaction = component2.m_faction;
							sE_Charm.charmPower = Mathf.Clamp(4f / (Mathf.Sqrt(component2.GetMaxHealth()) * component2.GetHealthPercentage() * component2.GetHealthPercentage()),0.05f,0.95f);
							component2.m_faction = player.GetFaction();
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_boar_pet"), component2.GetEyePoint(), UnityEngine.Quaternion.identity);
							component2.GetSEMan().AddStatusEffect(sE_Charm);
							component2.SetTamed(tamed: true);
						}
					}
				}
			}
			else
			{
				if (!(__instance.name == "VL_ValkyrieSpear"))
				{
					return;
				}
				bool hitCharacter2 = false;
				if (!(__instance.m_aoe > 0f))
				{
					return;
				}
				Collider[] array4 = Physics.OverlapSphere(hitPoint, __instance.m_aoe, ___s_rayMaskSolids, QueryTriggerInteraction.UseGlobal);
				HashSet<GameObject> hashSet2 = new HashSet<GameObject>();
				Collider[] array5 = array4;
				Collider[] array6 = array5;
				foreach (Collider collider3 in array6)
				{
					GameObject gameObject2 = Projectile.FindHitObject(collider3);
					IDestructible component3 = gameObject2.GetComponent<IDestructible>();
					if (component3 == null || hashSet2.Contains(gameObject2))
					{
						continue;
					}
					hashSet2.Add(gameObject2);
					if (IsValidTarget(component3, ref hitCharacter2, ___m_owner, __instance.m_dodgeable))
					{
						Character component4 = null;
						gameObject2.TryGetComponent<Character>(out component4);
						bool flag2 = component4 != null;
						if (component4 == null)
						{
							component4 = (Character)gameObject2.GetComponentInParent(typeof(Character));
							flag2 = component4 != null;
						}
						if (flag2 && !component4.IsPlayer() && ___m_owner is Player && !component4.m_boss)
						{
							Player player2 = ___m_owner as Player;
							UnityEngine.Vector3 forceDirection = component4.transform.position - player2.transform.position;
							float magnitude = forceDirection.magnitude;
							float num = component4.GetMass() * 0.05f;
							UnityEngine.Vector3 vector = new UnityEngine.Vector3(0f, 4f / num, 0f);
							component4.Stagger(forceDirection);
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightburst"), component4.transform.position, UnityEngine.Quaternion.LookRotation(new UnityEngine.Vector3(0f, 1f, 0f)));
							Traverse.Create(component4).Field("m_pushForce").SetValue(vector);
						}
					}
				}
			}
		}
	}

	[HarmonyPatch(typeof(Player), "InShelter", null)]
	public class PlayerShelter_BiomePatch
	{
		public static void Postfix(Player __instance, ref bool __result)
		{
			if (__instance.IsPlayer() && __instance.GetSEMan().HaveStatusEffect("SE_VL_BiomeBlackForest".GetStableHashCode()))
			{
				__result = true;
			}
		}
	}

	[HarmonyPatch(typeof(Character), "UpdateMotion", null)]
	public class ClassMotionUpdate_Postfix
	{
		public static bool Prefix(Character __instance, ref bool ___m_flying, float ___m_waterLevel)
		{
			if (vl_player != null && vl_player.vl_class == PlayerClass.Shaman && Class_Shaman.isWaterWalking)
			{
				___m_flying = true;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Player), "GetMaxCarryWeight", null)]
	public class PlayerCarryWeight_BiomePatch
	{
		public static void Postfix(Player __instance, ref float __result)
		{
			if (__instance.IsPlayer() && __instance.GetSEMan().HaveStatusEffect("SE_VL_BiomeMeadows".GetStableHashCode()))
			{
				SE_BiomeMeadows sE_BiomeMeadows = (SE_BiomeMeadows)__instance.GetSEMan().GetStatusEffect("SE_VL_BiomeMeadows".GetStableHashCode());
				__result += sE_BiomeMeadows.carryModifier;
			}
		}
	}

	[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.IsCold))]
	public static class EnvManIsCold
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(EnvMan __instance, ref bool __result)
		{
			if (Player.m_localPlayer == null)
				return true;
			if (Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_BiomeMountain".GetStableHashCode()) || Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_FlameArmor".GetStableHashCode()))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.IsWet))]
	public static class EnvManIsWet
	{
		[HarmonyPriority(Priority.First)]
		public static bool Prefix(EnvMan __instance, ref bool __result)
		{
			if (Player.m_localPlayer == null)
				return true;
			if (Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_BiomeSwamp".GetStableHashCode()))
			{
				__result = false;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Humanoid), "BlockAttack", null)]
	public class Block_Class_Patch
	{
		public static bool Prefix(Humanoid __instance, HitData hit, Character attacker, float ___m_blockTimer, ItemDrop.ItemData ___m_leftItem, ref bool __result)
		{
			if (__instance == Player.m_localPlayer)
			{
				if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Bulwark".GetStableHashCode()))
				{
					Class_Valkyrie.isBlocking = true;
				}
				else if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Reactivearmor".GetStableHashCode()))
				{
					__result = false;
					return false;
				}
				else
				{
					if (vl_player.vl_class == PlayerClass.Duelist && ___m_leftItem == null)
					{
						if (UnityEngine.Vector3.Dot(hit.m_dir, __instance.transform.forward) > 0f)
						{
							__result = false;
							return false;
						}
						ItemDrop.ItemData currentWeapon = __instance.GetCurrentWeapon();
						if (currentWeapon == null)
						{
							__result = false;
							return false;
						}
						HitData hitData = new HitData();
						hitData.m_damage = hit.m_damage;
						float level = __instance.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f));
						bool flag = currentWeapon.m_shared.m_timedBlockBonus > 1f && ___m_blockTimer != -1f && ___m_blockTimer < 0.25f;
						float skillFactor = __instance.GetSkillFactor(Skills.SkillType.Blocking);
						float num = currentWeapon.GetBlockPower(skillFactor);
						if (flag)
						{
							num *= currentWeapon.m_shared.m_timedBlockBonus;
							num = ((!__instance.GetSEMan().HaveStatusEffect("SE_VL_Riposte".GetStableHashCode())) ? (num + 2f * level * VL_GlobalConfigs.c_duelistBonusParry) : (num + 10f * level * VL_GlobalConfigs.c_duelistBonusParry));
						}
						float totalBlockableDamage = hit.GetTotalBlockableDamage();
						float num2 = Mathf.Min(totalBlockableDamage, num);
						float num3 = Mathf.Clamp01(num2 / num);
						float stamina = __instance.m_blockStaminaDrain * num3 * 0.5f;
						__instance.UseStamina(stamina);
						bool flag2 = __instance.HaveStamina();
						bool flag3 = flag2 && num >= totalBlockableDamage;
						if (flag2)
						{
							hit.m_statusEffectHash = "".GetStableHashCode();
							hit.BlockDamage(num2);
							DamageText.instance.ShowText(DamageText.TextType.Blocked, hit.m_point + UnityEngine.Vector3.up * 0.5f, num2);
						}
						if (!flag2 || !flag3)
						{
							__instance.Stagger(hit.m_dir);
						}
						__instance.RaiseSkill(Skills.SkillType.Blocking, flag ? 2f : 1f);
						currentWeapon.m_shared.m_blockEffect.Create(hit.m_point, UnityEngine.Quaternion.identity);
						if ((bool)attacker && flag && flag3)
						{
							__instance.m_perfectBlockEffect.Create(hit.m_point, UnityEngine.Quaternion.identity);
							if (attacker.m_staggerWhenBlocked)
							{
								attacker.Stagger(-hit.m_dir);
							}
						}
						if (flag3)
						{
							float num4 = Mathf.Clamp01(num3 * 0.5f);
							hit.m_pushForce *= num4;
							if ((bool)attacker && flag)
							{
								HitData hitData2 = new HitData();
								//hitData2.m_pushForce = currentWeapon.GetDeflectionForce() * (1f - num4);
								hitData2.m_pushForce = hit.m_pushForce;
								hitData2.m_dir = __instance.transform.position - attacker.transform.position;
								hitData2.m_dir.y = 0f;
								hitData2.m_dir.Normalize();
								hitData2.m_point = attacker.GetEyePoint();
								if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Riposte".GetStableHashCode()) && (attacker.transform.position - __instance.transform.position).magnitude < 8f)
								{
									SE_Riposte sE_Riposte = (SE_Riposte)__instance.GetSEMan().GetStatusEffect("SE_VL_Riposte".GetStableHashCode());
									__instance.GetSEMan().RemoveStatusEffect("SE_VL_Riposte".GetStableHashCode());
									hitData2.m_damage = hitData.m_damage;
									//((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("atgeir_attack2");
									UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ParticleLightSuction"), __instance.GetEyePoint(), UnityEngine.Quaternion.identity);
									float num5 = UnityEngine.Random.Range(0.3f, 0.5f) + level / 150f;
									hitData2.ApplyModifier(num5 * VL_GlobalConfigs.c_duelistRiposte);
									__instance.RaiseSkill(DisciplineSkill, VL_Utility.GetRiposteSkillGain * 2f);
									if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
									{
										__instance.GetSEMan().GetStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()).m_ttl -= 5f;
									}
									if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
									{
										__instance.GetSEMan().GetStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()).m_ttl -= 5f;
									}
									if (Class_Duelist.challengedMastery != null && Class_Duelist.challengedMastery.Contains(attacker.GetInstanceID()))
									{
										Class_Duelist.challengedMastery.Remove(attacker.GetInstanceID());
										int coinsSpoiled = Mathf.CeilToInt(Mathf.Sqrt(attacker.GetMaxHealth()) + ((1f + (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f)) * Mathf.Sqrt(attacker.GetMaxHealth())));
										if (coinsSpoiled < EpicMMOSystem.LevelSystem.Instance.getLevel())
										{
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_stack_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
										}
										else
										{
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_pile_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_pile_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
										}
										if (__instance.GetInventory().CanAddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled))
                                        {
											__instance.GetInventory().AddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled);
                                        }
										else
                                        {
											ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, __instance.transform.position, UnityEngine.Quaternion.identity);
										}
										__instance.Message(MessageHud.MessageType.TopLeft, "Spoiled " + coinsSpoiled.ToString("#") + " coins from " + attacker.GetHoverName() + "!");
                                        StatusEffect statusEffect4 = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
                                        //ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, localPlayer.transform.position, UnityEngine.Quaternion.identity);
                                    }
                                }
								else if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Riposte".GetStableHashCode()) && hit.m_ranged)
								{
									SE_Riposte sE_Riposte = (SE_Riposte)__instance.GetSEMan().GetStatusEffect("SE_VL_Riposte".GetStableHashCode());
									hit.ApplyModifier(0f);
									UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Smokeburst"), __instance.GetEyePoint(), UnityEngine.Quaternion.identity);
									UnityEngine.Vector3 backstabPoint;
									float numB;
									numB = (attacker.GetCollider().bounds.size.x + attacker.GetCollider().bounds.size.z) / 2f;
									numB = Mathf.Clamp(numB, 0.6f, 2f);
									numB *= -1.0f;
									backstabPoint = attacker.transform.position + attacker.transform.forward * numB;
									backstabPoint.y += 0.1f;
									Rigidbody playerBody = sE_Riposte.playerBody;
									playerBody.transform.position = backstabPoint;
									__instance.transform.rotation = attacker.transform.rotation;
									UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Smokeburst"), backstabPoint, UnityEngine.Quaternion.identity);
									UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Shadowburst"), backstabPoint + __instance.transform.up * 0.5f, UnityEngine.Quaternion.LookRotation(__instance.GetLookDir()));
									__instance.GetSEMan().RemoveStatusEffect("SE_VL_Riposte".GetStableHashCode());
									__instance.RaiseSkill(DisciplineSkill, VL_Utility.GetRiposteSkillGain * 2f);
									if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
									{
										__instance.GetSEMan().GetStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()).m_ttl -= 5f;
									}
									if (__instance.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
									{
										__instance.GetSEMan().GetStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()).m_ttl -= 5f;
									}
									if (Class_Duelist.challengedMastery != null && Class_Duelist.challengedMastery.Contains(attacker.GetInstanceID()))
									{
										Class_Duelist.challengedMastery.Remove(attacker.GetInstanceID());
										int coinsSpoiled = Mathf.CeilToInt(Mathf.Sqrt(attacker.GetMaxHealth()) + ((1f + (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f)) * Mathf.Sqrt(attacker.GetMaxHealth())));
										if (coinsSpoiled < EpicMMOSystem.LevelSystem.Instance.getLevel())
										{
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_stack_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
										}
										else
										{
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_pile_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
											UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_pile_destroyed"), __instance.GetCenterPoint(), UnityEngine.Quaternion.identity);
										}
										if (__instance.GetInventory().CanAddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled))
										{
											__instance.GetInventory().AddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled);
										}
										else
										{
											ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, __instance.transform.position, UnityEngine.Quaternion.identity);
										}
										__instance.Message(MessageHud.MessageType.TopLeft, "Spoiled " + coinsSpoiled.ToString("#") + " coins from " + attacker.GetHoverName() + "!");
                                        //ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, localPlayer.transform.position, UnityEngine.Quaternion.identity);
                                    }
                                }
								attacker.Damage(hitData2);
							}
						}
						__result = true;
						return false;
					}
					Class_Valkyrie.isBlocking = false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ItemDrop.ItemData), "GetBaseBlockPower", new Type[] { typeof(int) })]
	public class BaseBlockPower_Bulwark_Patch
	{
		public static void Postfix(ItemDrop.ItemData __instance, ref float __result)
		{
			if (Class_Valkyrie.isBlocking)
			{
				__result += 20f;
			}
			if (vl_player.vl_class == PlayerClass.Monk && __instance.m_shared != null && __instance.m_shared.m_name == "Unarmed")
			{
				__result += Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == DisciplineSkillDef)
					.m_level * VL_GlobalConfigs.c_monkBonusBlock;
			}
			if (Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_BiomeOcean".GetStableHashCode()))
			{
				SE_BiomeOcean sE_BiomeOcean = Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_BiomeOcean".GetStableHashCode()) as SE_BiomeOcean;
				__result *= sE_BiomeOcean.blockModifier;
			}
		}
	}

	[HarmonyPatch(typeof(Character), "CheckDeath")]
	public class OnDeath_Patch
	{
		public static bool Prefix(Character __instance)
		{
			if (!__instance.IsDead() && __instance.GetHealth() <= 0f && vl_player != null)
			{
				Player player = __instance as Player;
				if (player != null && vl_player.vl_class == PlayerClass.Priest && player.GetPlayerName() == vl_player.vl_name)
				{
					if (!__instance.GetSEMan().HaveStatusEffect("SE_VL_DyingLight_CD".GetStableHashCode()))
					{
						StatusEffect statusEffect = (SE_DyingLight_CD)ScriptableObject.CreateInstance(typeof(SE_DyingLight_CD));
						statusEffect.m_ttl = 600f * VL_GlobalConfigs.c_priestBonusDyingLightCooldown;
						__instance.GetSEMan().AddStatusEffect(statusEffect);
						__instance.SetHealth(1f);
						return false;
					}
				}
				else if (vl_player.vl_class == PlayerClass.Shaman)
				{
					Player localPlayer = Player.m_localPlayer;
					if (localPlayer != null && vl_player.vl_name == localPlayer.GetPlayerName() && UnityEngine.Vector3.Distance(localPlayer.transform.position, __instance.transform.position) <= 10f)
					{
						UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_AbsorbSpirit"), localPlayer.GetCenterPoint(), UnityEngine.Quaternion.identity);
						localPlayer.AddStamina(25f * VL_GlobalConfigs.c_shamanBonusSpiritGuide);
						if (Class_Shaman.gotWindfuryCooldown)
						{
							StatusEffect statusEffect = (SE_Windfury_CD)ScriptableObject.CreateInstance(typeof(SE_Windfury_CD));
							localPlayer.GetSEMan().RemoveStatusEffect(statusEffect);
							Class_Shaman.gotWindfuryCooldown = false;
						}
                        if (localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
                        {
                            localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()).m_ttl *= 0.7f;
                        }
                        if (localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
                        {
                            localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()).m_ttl *= 0.7f;
                        }
                        if (localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
                        {
                            localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()).m_ttl *= 0.7f;
                        }
                    }
                }
				else if (vl_player.vl_class == PlayerClass.Duelist)
				{
					Player localPlayer = Player.m_localPlayer;
					if (localPlayer != null && vl_player.vl_name == localPlayer.GetPlayerName() && UnityEngine.Vector3.Distance(localPlayer.transform.position, __instance.transform.position) <= 70f)
					{
						if (Class_Duelist.challengedDeath != null && Class_Duelist.challengedDeath.Contains(__instance.GetInstanceID()))
						{
							Class_Duelist.challengedDeath.Remove(__instance.GetInstanceID());
							int coinsSpoiled = Mathf.CeilToInt(Mathf.Sqrt(__instance.GetMaxHealth()) + ((1f + (EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f)) * Mathf.Sqrt(__instance.GetMaxHealth())));
							if (coinsSpoiled < EpicMMOSystem.LevelSystem.Instance.getLevel())
							{
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_destroyed"), localPlayer.GetCenterPoint(), UnityEngine.Quaternion.identity);
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_stack_destroyed"), localPlayer.GetCenterPoint(), UnityEngine.Quaternion.identity);
							}
							else
							{
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_coins_pile_destroyed"), localPlayer.GetCenterPoint(), UnityEngine.Quaternion.identity);
								UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_coin_pile_destroyed"), localPlayer.GetCenterPoint(), UnityEngine.Quaternion.identity);
							}
							if (localPlayer.GetInventory().CanAddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled))
							{
								localPlayer.GetInventory().AddItem(ValheimLegends.coinsItem.m_itemData.m_dropPrefab, coinsSpoiled);
							}
							else
							{
								ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, localPlayer.transform.position, UnityEngine.Quaternion.identity);
							}
							localPlayer.Message(MessageHud.MessageType.TopLeft, "Spoiled " + coinsSpoiled.ToString("#") + " coins from " + __instance.GetHoverName() + "!");
                            //ItemDrop.DropItem(ValheimLegends.coinsItem.m_itemData, coinsSpoiled, localPlayer.transform.position, UnityEngine.Quaternion.identity);
                            SE_Ability1_CD sE_Ability1_CD = localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()) as SE_Ability1_CD;
							if (localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
							{
                                localPlayer.GetSEMan().RemoveStatusEffect(sE_Ability1_CD);
                            }
                        }
						else if (Class_Duelist.challengedMastery != null && Class_Duelist.challengedMastery.Contains(__instance.GetInstanceID()))
						{
                            SE_Ability1_CD sE_Ability1_CD = localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()) as SE_Ability1_CD;
                            if (localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
							{
                                localPlayer.GetSEMan().RemoveStatusEffect(sE_Ability1_CD);
                            }
                        }
					}
				}
				else if (vl_player.vl_class == PlayerClass.Rogue)
				{
					Player localPlayer = Player.m_localPlayer;
					if (localPlayer != null && vl_player.vl_name == localPlayer.GetPlayerName())
					{
						SE_Rogue sE_Rogue = (SE_Rogue)localPlayer.GetSEMan().GetStatusEffect("SE_VL_Rogue".GetStableHashCode());
						if (sE_Rogue.lastSnatched != null && sE_Rogue.lastSnatched.Contains(__instance.GetInstanceID()))
						{
							sE_Rogue.lastSnatched.Remove(__instance.GetInstanceID());
						}
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(HitData), "BlockDamage")]
	public static class BlockDamage_Patch
	{
		public static bool Prefix(HitData __instance, float damage, HitData.DamageTypes ___m_damage)
		{
			if (vl_player != null)
			{
				if (vl_player.vl_class == PlayerClass.Monk && Class_Monk.PlayerIsUnarmed && Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Monk".GetStableHashCode()))
				{
					if (__instance.GetTotalBlockableDamage() >= damage)
					{
						SE_Monk sE_Monk = (SE_Monk)Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Monk".GetStableHashCode());
						sE_Monk.maxHitCount = 5 + Mathf.RoundToInt(0.4f * Mathf.Sqrt(Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f))));
						sE_Monk.hitCount++;
						sE_Monk.hitCount = Mathf.Clamp(sE_Monk.hitCount, 0, sE_Monk.maxHitCount);
						sE_Monk.refreshed = true;
					}
				}
				else if (vl_player.vl_class == PlayerClass.Valkyrie && Class_Valkyrie.PlayerUsingShield)
				{
					if (__instance.GetTotalBlockableDamage() >= damage)
					{
						SE_Valkyrie sE_Valkyrie = (SE_Valkyrie)Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Valkyrie".GetStableHashCode());
						sE_Valkyrie.maxHitCount = 8 + Mathf.RoundToInt(Mathf.Sqrt(Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f))));
						sE_Valkyrie.hitCount++;
						if (Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Bulwark".GetStableHashCode()))
							sE_Valkyrie.hitCount++;
						sE_Valkyrie.hitCount = Mathf.Clamp(sE_Valkyrie.hitCount, 0, sE_Valkyrie.maxHitCount);
						sE_Valkyrie.refreshed = true;
					}
				}
				else if (vl_player.vl_class == PlayerClass.Enchanter)
				{
					float totalBlockableDamage = __instance.GetTotalBlockableDamage();
					float num = damage / totalBlockableDamage;
					if (num > 0f)
					{
						float num2 = ___m_damage.m_fire * num;
						float num3 = ___m_damage.m_frost * num;
						float num4 = ___m_damage.m_lightning * num;
						float num5 = num2 + num3 + num4;
						if (num5 > 0f)
						{
							Player.m_localPlayer.AddStamina(num5 * VL_GlobalConfigs.c_enchanterBonusElementalBlock);
							Player.m_localPlayer.RaiseSkill(AbjurationSkill, num);
							UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), Player.m_localPlayer.transform.position, UnityEngine.Quaternion.identity);
						}
					}
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Skills), "GetSkillDef")]
	public static class GetSkillDef_Patch
	{
		public static void Postfix(Skills __instance, Skills.SkillType type, List<Skills.SkillDef> ___m_skills, ref Skills.SkillDef __result)
		{
			MethodInfo methodInfo = AccessTools.Method(typeof(Localization), "AddWord");
			if (__result != null || legendsSkills == null)
			{
				return;
			}
			foreach (Skills.SkillDef legendsSkill in legendsSkills)
			{
				if (!___m_skills.Contains(legendsSkill))
				{
					___m_skills.Add(legendsSkill);
					Localization instance = Localization.instance;
					object[] obj = new object[2]
					{
						"skill_" + legendsSkill.m_skill,
						null
					};
					SkillName skill = (SkillName)legendsSkill.m_skill;
					obj[1] = skill.ToString();
					methodInfo.Invoke(instance, obj);
				}
			}
			__result = ___m_skills.FirstOrDefault((Skills.SkillDef x) => x.m_skill == type);
		}
	}

	[HarmonyPatch(typeof(Hud), "UpdateStatusEffects")]
	public static class SkillIcon_Patch
	{
		public static void Postfix(Hud __instance)
		{
			if (!(__instance != null) || !ClassIsValid || !showAbilityIcons.Value)
			{
				return;
			}
			if (abilitiesStatus == null)
			{
				abilitiesStatus = new List<RectTransform>();
				abilitiesStatus.Clear();
			}
			if (abilitiesStatus.Count != 3)
			{
				foreach (RectTransform item in abilitiesStatus)
				{
					UnityEngine.Object.Destroy(item.gameObject);
				}
				abilitiesStatus.Clear();
				VL_Utility.InitiateAbilityStatus(__instance);
			}
			if (abilitiesStatus == null)
			{
				return;
			}
			for (int i = 0; i < abilitiesStatus.Count; i++)
			{
				RectTransform rectTransform = abilitiesStatus[i];
				Image component = rectTransform.Find("Icon").GetComponent<Image>();
				string text = "";
				switch (i)
				{
				case 0:
					component.sprite = Ability1_Sprite;
					if (Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
					{
						component.color = abilityCooldownColor;
						text = StatusEffect.GetTimeString(Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()).GetRemaningTime());
						break;
					}
					component.color = Color.white;
					text = Ability1_Hotkey.Value;
					if (Ability1_Hotkey_Combo.Value != "")
					{
						text = text + " + " + Ability1_Hotkey_Combo.Value;
					}
					break;
				case 1:
					component.sprite = Ability2_Sprite;
					if (Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
					{
						component.color = abilityCooldownColor;
						text = StatusEffect.GetTimeString(Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()).GetRemaningTime());
						break;
					}
					component.color = Color.white;
					text = Ability2_Hotkey.Value;
					if (Ability2_Hotkey_Combo.Value != "")
					{
						text = text + " + " + Ability2_Hotkey_Combo.Value;
					}
					break;
				default:
					component.sprite = Ability3_Sprite;
					if (Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
					{
						component.color = abilityCooldownColor;
						text = StatusEffect.GetTimeString(Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()).GetRemaningTime());
						break;
					}
					component.color = Color.white;
					text = Ability3_Hotkey.Value;
					if (Ability3_Hotkey_Combo.Value != "")
					{
						text = text + " + " + Ability3_Hotkey_Combo.Value;
					}
					break;
				}
				TMP_Text component2 = rectTransform.Find("TimeText").GetComponent<TMP_Text>();
				if (!string.IsNullOrEmpty(text))
				{
					((Component)(object)component2).gameObject.SetActive(value: true);
					component2.text = text;
				}
				else
				{
					((Component)(object)component2).gameObject.SetActive(value: false);
				}
			}
		}
	}

	[HarmonyPatch(typeof(Skills), "IsSkillValid")]
	public static class ValidSkill_Patch
	{
		public static bool Prefix(Skills __instance, Skills.SkillType type, ref bool __result)
		{
			if (type == AlterationSkill || type == AbjurationSkill || type == ConjurationSkill || type == EvocationSkill || type == DisciplineSkill || type == IllusionSkill)
			{
				__result = true;
				return false;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Player), "Update")]
	public static class AbilityInput_Prefix
	{
		public static bool Prefix(Player __instance)
		{
			if (ZInput.GetButtonDown("GP") || ZInput.GetButtonDown("JoyGP"))
			{
				shouldUseGuardianPower = true;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Player), "UpdateDodge", null)]
	public class DodgePatch_Postfix
	{
		private static void Prefix(Player __instance)
		{
			if (__instance.GetSEMan().HaveStatusEffect("SE_VL_BiomePlains".GetStableHashCode()))
			{
				SE_BiomePlains sE_BiomePlains = __instance.GetSEMan().GetStatusEffect("SE_VL_BiomePlains".GetStableHashCode()) as SE_BiomePlains;
				__instance.m_dodgeStaminaUsage *= sE_BiomePlains.dodgeModifier;
			}
		}

		public static void Postfix(Player __instance, float ___m_queuedDodgeTimer)
		{
			if (___m_queuedDodgeTimer < -0.5f && ___m_queuedDodgeTimer > -0.55f && vl_player != null && vl_player.vl_name == __instance.GetPlayerName() && vl_player.vl_class == PlayerClass.Ranger)
			{
				SE_Ranger sE_Ranger = (SE_Ranger)__instance.GetSEMan().GetStatusEffect("SE_VL_Ranger".GetStableHashCode());
				if (sE_Ranger != null)
				{
					sE_Ranger.hitCount = 3f;
				}
			}
			if (__instance.GetSEMan().HaveStatusEffect("SE_VL_BiomePlains".GetStableHashCode()))
			{
				SE_BiomePlains sE_BiomePlains = __instance.GetSEMan().GetStatusEffect("SE_VL_BiomePlains".GetStableHashCode()) as SE_BiomePlains;
				__instance.m_dodgeStaminaUsage /= sE_BiomePlains.dodgeModifier;
			}
		}
	}

	[HarmonyPatch(typeof(Player), "RaiseSkill", null)]
	public static class VinesHit_SkillRaise_Prefix
	{
		public static bool Prefix(Player __instance, Skills.SkillType skill, ref float value)
		{
			if (__instance.IsPlayer() && skill == ConjurationSkill)
			{
				if (value == 0.5f)
				{
					value = 0.1f;
				}
				else if (value == 1f)
				{
					value = 0.5f;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(OfferingBowl), "UseItem", null)]
	public class OfferingForClass_Patch
	{
		public static bool Prefix(OfferingBowl __instance, Humanoid user, ItemDrop.ItemData item, Transform ___m_itemSpawnPoint, EffectList ___m_fuelAddedEffects, ref bool __result)
		{
			if (VL_GlobalConfigs.ConfigStrings["vl_svr_allowAltarClassChange"] != 0f)
			{
				int num = user.GetInventory().CountItems(item.m_shared.m_name);
				bool flag = false;
				if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_shamanItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_shamanItem"] != "" && vl_player.vl_class != PlayerClass.Shaman)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Shaman");
					vl_player.vl_class = PlayerClass.Shaman;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_rangerItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_rangerItem"] != "" && vl_player.vl_class != PlayerClass.Ranger)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Ranger");
					vl_player.vl_class = PlayerClass.Ranger;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_mageItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_mageItem"] != "" && vl_player.vl_class != PlayerClass.Mage)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Mage");
					vl_player.vl_class = PlayerClass.Mage;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_valkyrieItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_valkyrieItem"] != "" && vl_player.vl_class != PlayerClass.Valkyrie)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Valkyrie");
					vl_player.vl_class = PlayerClass.Valkyrie;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_druidItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_druidItem"] != "" && vl_player.vl_class != PlayerClass.Druid)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Druid");
					vl_player.vl_class = PlayerClass.Druid;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_berserkerItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_berserkerItem"] != "" && vl_player.vl_class != PlayerClass.Berserker)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Berserker");
					vl_player.vl_class = PlayerClass.Berserker;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_metavokerItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_metavokerItem"] != "" && vl_player.vl_class != PlayerClass.Metavoker)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Metavoker");
					vl_player.vl_class = PlayerClass.Metavoker;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_priestItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_priestItem"] != "" && vl_player.vl_class != PlayerClass.Priest)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Priest");
					vl_player.vl_class = PlayerClass.Priest;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_monkItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_monkItem"] != "" && vl_player.vl_class != PlayerClass.Monk)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Monk");
					vl_player.vl_class = PlayerClass.Monk;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_duelistItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_duelistItem"] != "" && vl_player.vl_class != PlayerClass.Duelist)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Duelist");
					vl_player.vl_class = PlayerClass.Duelist;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_enchanterItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_enchanterItem"] != "" && vl_player.vl_class != PlayerClass.Enchanter)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Enchanter");
					vl_player.vl_class = PlayerClass.Enchanter;
					flag = true;
				}
				else if (item.m_shared.m_name.Contains(VL_GlobalConfigs.ItemStrings["vl_svr_rogueItem"]) && VL_GlobalConfigs.ItemStrings["vl_svr_rogueItem"] != "" && vl_player.vl_class != PlayerClass.Rogue)
				{
					user.Message(MessageHud.MessageType.Center, "Acquired the powers of a Rogue");
					vl_player.vl_class = PlayerClass.Rogue;
					flag = true;
				}
				if (flag)
				{
					user.GetInventory().RemoveItem(item.m_shared.m_name, 1);
					user.ShowRemovedMessage(item, 1);
					user.RaiseSkill(ValheimLegends.DisciplineSkill, 0.0001f);
					user.RaiseSkill(ValheimLegends.AbjurationSkill, 0.0001f);
					user.RaiseSkill(ValheimLegends.AlterationSkill, 0.0001f);
					user.RaiseSkill(ValheimLegends.ConjurationSkill, 0.0001f);
					user.RaiseSkill(ValheimLegends.EvocationSkill, 0.0001f);
					user.RaiseSkill(ValheimLegends.IllusionSkill, 0.0001f);
					UpdateVLPlayer(Player.m_localPlayer);
					NameCooldowns();
					if ((bool)___m_itemSpawnPoint)
					{
						___m_fuelAddedEffects?.Create(___m_itemSpawnPoint.position, __instance.transform.rotation);
					}
					UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_GP_Activation"), user.GetCenterPoint(), UnityEngine.Quaternion.identity);
					if (abilitiesStatus != null)
					{
						foreach (RectTransform item2 in abilitiesStatus)
						{
							if (item2.gameObject != null)
							{
								UnityEngine.Object.Destroy(item2.gameObject);
							}
						}
						abilitiesStatus.Clear();
					}
					__result = true;
					return false;
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(ZNet), "OnDestroy")]
	public static class RemoveHud_Patch
	{
		public static bool Prefix()
		{
			if (abilitiesStatus != null)
			{
				foreach (RectTransform item in abilitiesStatus)
				{
					if (item.gameObject != null)
					{
						UnityEngine.Object.Destroy(item.gameObject);
					}
				}
				abilitiesStatus.Clear();
				abilitiesStatus = null;
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Player), "OnSpawned", null)]
	public class SetLegendClass_Postfix
	{
		public static void Postfix(Player __instance)
		{
			SetVLPlayer(__instance);
		}
	}

	[HarmonyPatch(typeof(Player), "Update", null)]
	public class BerserkLimitPatch
	{
		public static bool Prefix()
		{
			if (Player.m_localPlayer == null)
			{
				return true;
			}
			Player player = Player.m_localPlayer;
			if (player.GetSEMan().HaveStatusEffect("SE_VL_Berserk".GetStableHashCode()))
			{
				if (player.GetHealth() < Mathf.Clamp(0.10f * player.GetMaxHealth(), 5f, 30f))
				{
					SE_Berserk sE_Berserk = (SE_Berserk)player.GetSEMan().GetStatusEffect("SE_VL_Berserk".GetStableHashCode());
					player.GetSEMan().RemoveStatusEffect(sE_Berserk, quiet: true);
					player.Message(MessageHud.MessageType.Center, "Low health!");
					player.Message(MessageHud.MessageType.TopLeft, "Berserk dissipated due to low health!");
				}
			}
			return true;
		}
	}

	[HarmonyPatch(typeof(Player), "Update", null)]
	public class AbilityInput_Postfix
	{
		public static void Postfix(Player __instance, ref float ___m_maxAirAltitude, ref Rigidbody ___m_body, ref Animator ___m_animator, ref float ___m_lastGroundTouch, float ___m_waterLevel)
		{
			if (VL_Utility.ReadyTime)
			{
				Player localPlayer = Player.m_localPlayer;
				UnityEngine.Vector3 position = __instance.transform.position;
				if (localPlayer != null && playerEnabled && VL_Utility.TakeInput(localPlayer) && !localPlayer.InPlaceMode())
				{
					if (vl_player.vl_class == PlayerClass.Mage)
					{
						Class_Mage.Process_Input(localPlayer, ___m_maxAirAltitude);
					}
					else if (vl_player.vl_class == PlayerClass.Druid)
					{
						Class_Druid.Process_Input(localPlayer, ___m_maxAirAltitude,ref ___m_body);
					}
					else if (vl_player.vl_class == PlayerClass.Shaman)
					{
						Class_Shaman.Process_Input(localPlayer, ref ___m_body, ref ___m_maxAirAltitude, ref ___m_lastGroundTouch, ___m_waterLevel);
					}
					else if (vl_player.vl_class == PlayerClass.Ranger)
					{
						Class_Ranger.Process_Input(localPlayer);
						if (!localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Ranger".GetStableHashCode()))
						{
							SE_Ranger sE_Ranger = (SE_Ranger)ScriptableObject.CreateInstance(typeof(SE_Ranger));
							sE_Ranger.m_ttl = SE_Ranger.m_baseTTL;
							localPlayer.GetSEMan().AddStatusEffect(sE_Ranger, resetTime: true);
						}
					}
					else if (vl_player.vl_class == PlayerClass.Berserker)
					{
						Class_Berserker.Process_Input(localPlayer, ref ___m_maxAirAltitude);
					}
					else if (vl_player.vl_class == PlayerClass.Valkyrie)
					{
						Class_Valkyrie.Process_Input(localPlayer);
						if (!localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Valkyrie".GetStableHashCode()))
						{
							SE_Valkyrie sE_Valkyrie = (SE_Valkyrie)ScriptableObject.CreateInstance(typeof(SE_Valkyrie));
							sE_Valkyrie.m_ttl = SE_Valkyrie.m_baseTTL;
							localPlayer.GetSEMan().AddStatusEffect(sE_Valkyrie, resetTime: true);
						}
					}
					else if (vl_player.vl_class == PlayerClass.Metavoker)
					{
						Class_Metavoker.Process_Input(localPlayer, ref ___m_maxAirAltitude, ref ___m_body);
					}
					else if (vl_player.vl_class == PlayerClass.Priest)
					{
						Class_Priest.Process_Input(localPlayer, ref ___m_maxAirAltitude);
					}
					else if (vl_player.vl_class == PlayerClass.Monk)
					{
						Class_Monk.Process_Input(localPlayer, ref ___m_body, ref ___m_maxAirAltitude, ref ___m_animator);
						if (!localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Monk".GetStableHashCode()))
						{
							SE_Monk sE_Monk = (SE_Monk)ScriptableObject.CreateInstance(typeof(SE_Monk));
							sE_Monk.m_ttl = SE_Monk.m_baseTTL;
							localPlayer.GetSEMan().AddStatusEffect(sE_Monk, resetTime: true);
						}
					}
					else if (vl_player.vl_class == PlayerClass.Duelist)
					{
						Class_Duelist.Process_Input(localPlayer, ref ___m_body);
					}
					else if (vl_player.vl_class == PlayerClass.Enchanter)
					{
						Class_Enchanter.Process_Input(localPlayer, ref ___m_maxAirAltitude);
					}
					else if (vl_player.vl_class == PlayerClass.Rogue)
					{
						Class_Rogue.Process_Input(localPlayer, ref ___m_body, ref ___m_maxAirAltitude);
						if (!localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Rogue".GetStableHashCode()))
						{
							SE_Rogue sE_Rogue = (SE_Rogue)ScriptableObject.CreateInstance(typeof(SE_Rogue));
							sE_Rogue.m_ttl = SE_Rogue.m_baseTTL;
							localPlayer.GetSEMan().AddStatusEffect(sE_Rogue, resetTime: true);
						}
					}
				}
				if (isChargingDash)
				{
					VL_Utility.SetTimer();
					dashCounter++;
					if (vl_player.vl_class == PlayerClass.Berserker && dashCounter >= 12)
					{
						isChargingDash = false;
						Class_Berserker.Execute_Dash(localPlayer, ref ___m_maxAirAltitude, ref ___m_body);
					}
					if (vl_player.vl_class == PlayerClass.Druid && dashCounter >= 12)
					{
						isChargingDash = false;
						var seMan = localPlayer.GetSEMan();
						if (seMan == null) return;

						if (seMan.HaveStatusEffect("SE_VL_DruidFenringForm".GetStableHashCode()))
						{
                            Class_Berserker.Execute_Dash(localPlayer, ref ___m_maxAirAltitude, ref ___m_body);
						}
						else if (seMan.HaveStatusEffect("SE_VL_DruidCultistForm".GetStableHashCode()))
                        {
                            Class_Mage.Execute_Attack(localPlayer);
                        }
                    }
                    else if (vl_player.vl_class == PlayerClass.Valkyrie && dashCounter >= (int)Class_Valkyrie.QueuedAttack)
					{
						isChargingDash = false;
						Class_Valkyrie.Execute_Attack(localPlayer, ref ___m_body, ref ___m_maxAirAltitude);
					}
					else if (vl_player.vl_class == PlayerClass.Duelist && dashCounter >= 10)
					{
						isChargingDash = false;
						isChanneling = false;
                        channelingBlocksMovement = true;
                        Class_Duelist.Execute_Slash(localPlayer);
					}
					else if (vl_player.vl_class == PlayerClass.Rogue && dashCounter >= 16)
					{
						isChargingDash = false;
						Class_Rogue.Execute_Throw(localPlayer);
					}
					else if (vl_player.vl_class == PlayerClass.Mage && dashCounter >= (int)Class_Mage.QueuedAttack)
					{
                        isChargingDash = false;
						Class_Mage.Execute_Attack(localPlayer);
					}
					else if (vl_player.vl_class == PlayerClass.Monk && dashCounter >= (int)Class_Monk.QueuedAttack)
					{
						isChargingDash = false;
						Class_Monk.Execute_Attack(localPlayer, ref ___m_body, ref ___m_maxAirAltitude);
					}
					else if (vl_player.vl_class == PlayerClass.Enchanter && dashCounter >= (int)Class_Enchanter.QueuedAttack)
					{
						isChargingDash = false;
						Class_Enchanter.Execute_Attack(localPlayer, ref ___m_body, ref ___m_maxAirAltitude);
					}
					else if (vl_player.vl_class == PlayerClass.Metavoker && dashCounter >= (int)Class_Metavoker.QueuedAttack)
					{
						isChargingDash = false;
						Class_Metavoker.Execute_Attack(localPlayer, ref ___m_body, ref ___m_maxAirAltitude);
					}
				}
			}
			if (animationCountdown > 0)
			{
				animationCountdown--;
			}
		}
	}

	// Capping damage at around 500 using dimishing returns.
	[HarmonyPatch(typeof(Character), "Damage", null)]
	public class VL_Damage_EpicMMOPatchPost
	{
		public static bool attackIsBackstabbed = false;
		public static float totalMultiplier = 1f;

		public static bool Prefix(Character __instance, ref HitData hit, ref float ___m_backstabTime)
		{
			if (hit != null || hit.GetAttacker() == null || !hit.GetAttacker().IsPlayer())
			{
				return true;
			}
			if (__instance.GetBaseAI() != null && !__instance.GetBaseAI().IsAlerted() && hit.m_backstabBonus > 1f && Time.time - ___m_backstabTime > 300f)
			{
				attackIsBackstabbed = true;
			}
			else
			{
				attackIsBackstabbed = false;

			}
			return true;
		}

		public static void Postfix(Character __instance, ref HitData hit)
		{
			if (hit != null && hit.GetAttacker() != null && hit.GetAttacker().IsPlayer())
			{
				if (attackIsBackstabbed)
				{
					totalMultiplier *= hit.m_backstabBonus;
					if (vl_player.vl_class == PlayerClass.Monk && Class_Monk.PlayerIsUnarmed && Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Monk".GetStableHashCode()))
					{
						SE_Monk sE_Monk = (SE_Monk)Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Monk".GetStableHashCode());
						sE_Monk.maxHitCount = 5 + Mathf.RoundToInt(0.4f * Mathf.Sqrt(Player.m_localPlayer.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.DisciplineSkillDef)
							.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f), 0f, 0.5f))));
						sE_Monk.hitCount++;
						sE_Monk.hitCount = Mathf.Clamp(sE_Monk.hitCount, 0, sE_Monk.maxHitCount);
						sE_Monk.refreshed = true;
					}
				}
				if (__instance.IsStaggering() && !__instance.IsPlayer())
				{
					totalMultiplier *= 2f;
					if (vl_player.vl_class == PlayerClass.Monk && Class_Monk.PlayerIsUnarmed && Player.m_localPlayer.GetSEMan().HaveStatusEffect("SE_VL_Monk".GetStableHashCode()))
					{
						SE_Monk sE_Monk = (SE_Monk)Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Monk".GetStableHashCode());
						sE_Monk.hitCount++;
						sE_Monk.refreshed = true;
					}
				}
				hit.ApplyModifier(1 / (1 + 12 * (1 - Mathf.Exp(-(hit.GetTotalDamage() * totalMultiplier) / 5000))));
				totalMultiplier = 1f;
			}
		}
	}

	[HarmonyPatch(typeof(Player), "OnSpawned")]
	public static class PlayerMuninNotification_Prefix
	{
		public static void Postfix(Player __instance)
		{
			Tutorial.instance.m_texts.RemoveAll(StartsWithVL);
			Tutorial.TutorialText tutorialText = new Tutorial.TutorialText();
			tutorialText.m_label = "Legends Offerings";
			tutorialText.m_name = "VL_Offerings";
			tutorialText.m_text = ("You can inherit legendary powers by placing colored Gemstones on the altar:" +
				"\nBerserker - Spinel Gemstone (purple) " +
				"\nDruid - Emerald Gemstone (green) " +
				"\nDuelist - Sapphire Gemstone (blue)" +
				"\nEnchanter - Ruby Gemstone (red)" +
				"\nMage - Ruby Gemstone (red)" +
				"\nMetavoker - Onyx Gemstone (black)" +
				"\nMonk - Topaz Gemstone (cyan)" +
				"\nPriest - Topaz Gemstone (cyan)" +
				"\nRanger - Sulfur Gemstone (yellow)" +
				"\nRogue - Onyx Gemstone (black)" +
				"\nShaman - Humite Gemstone (orange)" +
				"\nValkyrie - Sapphire Gemstone (blue)" +
				"\n\nSome Gemstones must be offered multiple times to activate the desired class.");
			tutorialText.m_topic = "Token Offering";
			Tutorial.TutorialText item2 = tutorialText;
			if (!Tutorial.instance.m_texts.Contains(item2))
			{
				Tutorial.instance.m_texts.Add(item2);
			}
			Tutorial.TutorialText item3 = new Tutorial.TutorialText
			{
				m_label = "Legend: Mage",
				m_name = "VL_Mage",
				m_text = "The story of a mage centers around one key element - raw power. A mage focuses on harnessing raw, destructive energy." +
				"\n\nSkills: Evocation" +
				"\n\nSuggested Stats: Intellect or Specialization" +
				"\nSacrifice: Ruby Gemstone (red)" +
				"\n\nFireball: creates a ball of fire above the caster that arcs towards the casters target." +
				"\nDamage:" +
				"\n Fire - 10->40 + 2*Evocation" +
				"\n Blunt - 1/2 Fire" +
				"\n AoE - 3m + 1%*Evocation" +
				"\nCooldown: 12s" +
				"\nEnergy: 50 + 0.5*Evocation" +
				"\n*Afflicts targets with burning" +
				"\n\nFrost Nova: point blank area of effect frost damage that slows victims for a short period." +
				"\nDamage:" +
				"\n Ice - 10 + 0.5*Evocation -> 20 + Evocation" +
				"\n AoE - 10m + 1%*Evocation" +
				"\nCooldown: 20s" +
				"\nEnergy: 40" +
				"\n*Slows movement of affected targets by 60% for 4s" +
				"\n**Removes burning effect from caster" +
				"\n\nMeteor: channels energy to call down a meteor storm on the targeted area." +
				"\nDamage (per meteor):" +
				"\n Fire - 30 + 0.5*Evocation -> 50 + Evocation" +
				"\n Blunt - 1/2 Fire" +
				"\n AoE - 8m + 0.5%*Evocation" +
				"\nCooldown: 180s" +
				"\nEnergy: 60 initial + 30 per second channeled" +
				"\n*Afflicts targets with burning" +
				"\n**Press and hold the ability button to channel the spell to create multiple meteors" +
				"\n**Cast Meteor while blocking to cast Meditate. Meditate will burn your stamina and recover your Eitr instantly. Cooldown and cost is proportional to the amount of Eitr recovered." +
				"\n***Jump or dodge to cancel ability" +
				"\n\nBonus skills:" +
				"\n - Inferno - alternate attack to Frost Nova; press the button assigned to Frost Nova while holding block to create a high powered fire blast around the caster" +
				"\n - Ice Daggers - alternate attack to Fireball; press the button assigned to Fireball while holding block to throw a short range dagger made of razor sharp ice",
				m_topic = "Legend Mage"
			};
			if (!Tutorial.instance.m_texts.Contains(item3))
			{
				Tutorial.instance.m_texts.Add(item3);
			}
			Tutorial.TutorialText item4 = new Tutorial.TutorialText
			{
				m_label = "Legend: Berserker",
				m_name = "VL_Berserker",
				m_text = "Berserkers harness their rage into physical carnage and will sacrifice their own health to fuel their rage." +
				"\n\nSkills: Discipline and Alteration" +
				"\n\nSuggested Stats: Strength or Dexterity, Specialization or Intellect" +
				"\nSacrifice: Spinel Gemstone (purple)" +
				"\n\nExecute: empower the next several physical attacks to deal extra damage." +
				"\nDamage:" +
				"\n Physical bonus (blunt/slash/pierce) - + 40% + 0.5% * Discipline" +
				"\n Stagger - 50% + 0.5% * Discipline" +
				"\nCharges: 3 + 0.04*Discipline" +
				"\nCooldown: 60s" +
				"\nEnergy: 60" +
				"\n\nBerserk: sacrifices health to increase movement speed, attack power, remove stamina regeneration delay and gain renewed energy through combat." +
				"\nDamage:" +
				"\n Bonus +20% + 0.5%*Alteration" +
				"\nMovement Speed - +20% + 0.5%*Alteration" +
				"\nCooldown: 60s" +
				"\nEnergy: 0" +
				"\n*Absorbs 15%+0.2%*Alteration of total incflicted damage as stamina" +
				"\n\nDash: dash forward in the blink of an eye, cutting through enemies in your way." +
				"\nDamage:" +
				"\n 80% + 0.5%*Discipline of equipped weapon damage" +
				"\nCooldown: 10s" +
				"\nEnergy: 70" +
				"\n*10m dash distance" +
				"\n\nBonus skills:" +
				"\n - 2H Specialist - 30% reduction in stamina use when swinging 2H weapons" +
				"\n - Dual Wield Specialist - 50% reduction in stamina use dual wielding weapons" +
				"\n - Blood Rage - gain progressive bonus damage for missing health, up to 2x damage with 25% health",
				m_topic = "Legend Berserker"
			};
			if (!Tutorial.instance.m_texts.Contains(item4))
			{
				Tutorial.instance.m_texts.Add(item4);
			}
			Tutorial.TutorialText item5 = new Tutorial.TutorialText
			{
				m_label = "Legend: Druid",
				m_name = "VL_Druid",
				m_text = "Druid's are the embodiment of nature's resilience, cunning, and fury and act as a conduit of its will." +
				"\n\nSkills: Conjuration and Alteration" +
				"\n\nSuggested Stats: Intellect, or Endurance and Specialization" +
				"\nSacrifice: Emerald Gemstone (green)" +
				"\n\nRegeneration: applies a heal over time to the caster and all nearby allies." +
				"\nHealing:" +
				"\n Self - 0.5 + 0.4*Alteration" +
				"\n Other - 2 + 0.25*Average Skill Level" +
				"\nDuration: Heals every 2s for 20s" +
				"\nCooldown: 60s" +
				"\nEnergy: 60" +
				"\n\nNature's Defense: calls upon nature to defend an area." +
				"\nSummon:" +
				"\n Duration - 24s + 0.3s*Conjuration" +
				"\n 3x Root defenders" +
				"\n 2x + 0.05*Conjuration Drusquitos" +
				"\nCooldown: 120s" +
				"\nEnergy: 80" +
				"\n*Defender's health and attack power increase with Conjuration" +
				"\n**Each Root defender restores stamina to the caster as long as the caster remains near the point Nature's Defense was activated" +
                "\n***Casting Nature's Defense while blocking shapeshifts the Druid in a Fenring. See more below." +
                "\n\nVines: create vines that grow at an alarming speed." +
				"\nDamage:" +
				"\n Piercing - 10 + 0.6*Conjuration -> 15 + 1.2*Conjuration per vine" +
				"\nCooldown: 20s" +
				"\nEnergy: 30 initial + 9 every .5s" +
				"\n*Vines are a channeled ability, press and hold the ability button to continuously project vines" +
                "\n\nFenring Form:" +
                "\nA Druid can shapeshift in a Fenring temporarily. Unarmed damage is increased and double jump is possible. Health and Stamina regen increased (the more damaged/tired the druid is, the more it recovers)." +
                "\nAfter transforming there is a grave period before Eitr is drained from the druid. Drain is reduced by Alteration skill. Shapeshifting imposes a cooldown before the druid can shapeshift again, reduced by Intelect and Alteration Skill." +
                "\nRunning out of Eitr will forcibly return the druid to human form. This forceful transformation causes a greater cooldown before the druid can shapeshift again, also reduced by Intelect and Alteration Skill." +
                "\nWhile in Fenring Form, the Druid's skillset changes to:" +
                "\n\nDash: dash forward in the blink of an eye, cutting through enemies in your way." +
                "\nDamage:" +
                "\n 80% + 0.5%*Discipline of weapon damage" +
                "\nCooldown: 10s" +
                "\nEnergy: 50" +
                "\n*10m dash distance" +
				"\n\nStagger: send forth a shock wave that staggers all nearby enemies." +
				"\nAoE: 6m" +
				"\nCooldown: 10s" +
				"\nEnergy: 40" +
                "\n\nShadow Stalk: fade into the shadows gaining a burst of speed and augmenting stealth." +
                "\nAugment:" +
                "\n All movement speed increased by 50% + 1%*Discipline for 3s + 0.03s*Discipline" +
                "\n Stealth movement speed increased by 50% + 1%*Discipline" +
                "\nDuration: 20s + 0.9s*Discipline" +
                "\nCooldown: 45s" +
                "\nEnergy: 40" +
                "\n*Shadow stalk causes enemies to lose track of the druid" +
                "\n\nBonus skills:" +
				"\n - Prepare Spirit Binding Vial: cast Regeneration while blocking to turn 1 Ancient Seed into a Spirit Binding Vial, that can be used to resurrect a fallen ally" +
				"\n - Natures Restoration - consume ancient seeds, pine cones, fir cones, beech seeds or birch seeds to quickly restore stamina and eitr; seeds may be consumed similar to any food item",
				m_topic = "Legend Druid"
			};
			if (!Tutorial.instance.m_texts.Contains(item5))
			{
				Tutorial.instance.m_texts.Add(item5);
			}
			Tutorial.TutorialText item6 = new Tutorial.TutorialText
			{
				m_label = "Legend: Metavoker",
				m_name = "VL_Metavoker",
				m_text = "Metavoker's manipulate energy in a manner that affects light, space, and potential" +
				"\n\nSkills: Illusion, Evocation and Abjuration" +
				"\n\nSuggested Stats: Intellect and Endurance, benefits from Dexterity and Specialization too." +
				"\nSacrifice: Onyx Gemstone (black)" +
				"\nLight: creates a light that follows the caster and illuminates a large area" +
				"\nDamage:" +
				"\n Lightning - 2 + 0.25*Illusion -> 5 + 0.5*Illusion" +
				"\n Force - 100 + Illusion" +
				"\nDuration: 5m (or until directed)" +
				"\nCooldown: 20s" +
				"\nEnergy: 50" +
				"\n*Use the ability once to summon the mage light for illumination" +
				"\n**Use the ability with a mage light active to direct the light as a projectile" +
				"\n\nReplica: bends light and energy to create reinforced illusions of every nearby enemy." +
				"\nSummon:" +
				"\n Duration - 8s + 0.2s*Illusion" +
				"\nCooldown: 30s" +
				"\nEnergy: 70" +
				"\n*Replica's health and attack power increase with Illusion" +
				"\n\nWarp: collects the energy of the caster and projects it to a target location; any excess energy is released at the exit point." +
				"\nDamage:" +
				"\n Lightning - excess distance * (0.033*Evocation -> 0.05*Evocation)" +
				"\nCooldown: 6s" +
				"\nEnergy: 40 initial + 60 every 1s" +
				"\n*Tap ability button to instantly warp towards the target" +
				"\n**Press and hold the ability button to collect energy to warp longer distances or warp with excess energy" +
				"\n**Hold block while casting Light to cast Reactive Armor." +
				"\n**Hold block while casting Warp to cancel Reactive Armor." +
				"\n\nReactive Armor: create an energy shield that absorbs damage and staggers attackers." +
				"\nThe shield is external to you. Be mindful of using it when blocking, since stamina will be drained before blocking occurs." +
				"\nDamage taken in Stamina instead of Health. The amount of stamina per damage is reduced from 2 x damage up to 1x damage, according to Abjuration." +
				"\nMelee attackers that hit the shield are staggered." +
				"\nCancelling the shield (Block + Warp) consumes the charges left to stagger enemies around (one for each charge). If any charges are left, it restores some of the cooldown according with the charges left." +
				"\nCooldown: 120s + 60s after breaking/cancelling." +
				"\nCharges: 3 + square root of (2*Abjuration)" +
				"\nEnergy: 40 initial + 2 * damage absorbed * (1 - Abjuration modifier / 300)" +
				"\n\nBonus skills:" +
				"\n - Safe Fall - press and hold jump to slow your descent; this ability requires stamina to maintain" +
				"\n - Force Wave - pressing attack while holding block will create a powerful wave of energy that knocks enemies back; shares a cooldown with Replica",
				m_topic = "Legend Metavoker"
			};
			if (!Tutorial.instance.m_texts.Contains(item6))
			{
				Tutorial.instance.m_texts.Add(item6);
			}
			Tutorial.TutorialText item7 = new Tutorial.TutorialText
			{
				m_label = "Legend: Duelist",
				m_name = "VL_Duelist",
				m_text = "Duelist's specialize in offensive combat techniques that exploit openings in an opponent's defense." +
				"\n\nSkills: Discipline" +
				"\n\nSuggested Stats: Strength or Dexterity, and Specialization of extra crits" +
				"\nSacrifice: Emerald Gemstone (green)" +
				"\n\nCoin Shot: snaps a high velocity coin into the target." +
				"\nDamage:" +
				"\n Pierce - 5->30 + Discipline" +
				"\nCooldown: 10s" +
				"\nEnergy: 25" +
				"\n\nChallenge to Duel: (Block + Snap Coin) challenges the target for a duel. It costs some coins, but winning the challenge will get you back double coins (plus up to 50% based on Specialization)." +
				"\nThere can be two kinds of duels (picked at random):" +
				"\nDuel to the Death: the challenge is won by defeating the challenged enemy." +
				"\nDuel of Master: the challenge is won by overcoming the challenged enemy in skill. Either perform a successful Seismic Slash, Riposte or attack the enemy when it is staggering to win the challenge." +
				"\nCoin cost:" +
				"\n Square root of the mob's max health." +
				"\nCoin prize:" +
				"\n Coin cost + (Coin cost) * up to 50% bonus from Specialization." +
				"\nCooldown: 40s" +
				"\nEnergy: 25" +
				"\n\nRiposte: turns the energy of an attack into a devastating counter-attack" +
				"\nDamage:" +
				"\n returns 50%+1%*Discipline of the damage upon the attacker and quickly launches an attack maneuver that deals damage based on the equipped weapon" +
				"\nCooldown: 6s" +
				"\nEnergy: 30" +
				"\n*Riposte must be timed well to be effective. Block amount and parry force is increased 10x while riposte is active and the player executes a perfect block." +
				"\n**Riposte can only be used with a weapon equipped and without a shield." +
				"\n***Riposte will not deal damage, but will teleport you to the back of a ranged attacker instead." +
				"\n\nSeismic Slash: a combat technique that compresses energy and releases it in a tight arc as a razor thin burst." +
				"\nDamage:" +
				"\n 60%+0.06%*Discipline of weapon damage" +
				"\nForce: 25+0.1*Discipline" +
				"\nCooldown: 30s" +
				"\nEnergy: 60" +
				"\n*Deals damage to all targets in a 25 degree cone in front of the caster" +
				"\n\nBonus skills:" +
				"\n - Sharpen weapon - (Block + Seismic Slash) repairs durability of the wielded bladed single one-handed weapon (swords, knives, axes or spears). Cooldown and cost depends on durability recovered." +
				"\n - Spoil - get up to 25% chance (5% + Specialization * 0.20%) to spoil some coins from defeating staggered enemies." +
				"\n - Weapon Master - gain a bonus to block and parry based on Discipline while wielding a bladed single one-handed weapon (swords, knives, axes or spears)" +
				"\n - Single-wield Specialization - get up to 25% chance (5% + Specialization * 0.20%) of critical hits using single one-handed weapons" +
				"\n - Energy Conversion - redirect the energy from a parried attack to reduce the cooldown of Hip Shot and S. Slash",
				m_topic = "Legend Duelist"
			};
			if (!Tutorial.instance.m_texts.Contains(item7))
			{
				Tutorial.instance.m_texts.Add(item7);
			}
			Tutorial.TutorialText item8 = new Tutorial.TutorialText
			{
				m_label = "Legend: Rogue",
				m_name = "VL_Rogue",
				m_text = "Rogue's are infamous for their dirty fighting and ruthless cunning." +
				"\n\nSkills: Discipline, Alteration and Illusion" +
				"\n\nSuggested Stats: Strength or Dexterity, and Specialization" +
				"\nSacrifice: Onyx Gemstone (black)" +
				"\n\nPoison Bomb: throw a vial of highly caustic poison that affects an area for a short time." +
				"\nDamage:" +
				"\n Poison DoT - 10 + alteration" +
				"\nCooldown: 30s" +
				"\nEnergy: 50" +
				"\n*Duration and hit frequency increase with Alteration" +
				"\n*Using Poison Bomb ability while crouching changes it to Smoke Bomb, which will make all monsters to lose your track." +
				"\n\nFade: returns the rogue to a previous point and adds a supply to the bag of tricks" +
				"\nCooldown: 15s" +
				"\nEnergy: 10" +
				"\n*Set the fade point by using the ability. While fade is on cooldown, use the ability again to instantly return to the fade." +
				"\n\nBackstab: instantly move behind the target and strike a critical blow" +
				"\nDamage:" +
				"\n 70% + 0.5%*Discipline of weapon damage" +
				"\nForce: 10 + 0.5*Discipline" +
				"\nCooldown: 20s" +
				"\nEnergy: 60" +
				"\n\nBonus skills:" +
				"\n - Bag of Tricks - prepare class charges every 20s to use bonus skills" +
				"\n - Pickpocket - will snatch some coins when attacking a target unarmed (consumes 1 rogue charge)." +
				"\n - Stealthy - gain a passive bonus to move speed while crouched" +
				"\n - Dagger Mastery - gain bonus poison damage based on Intellect and Agility while using daggers (offhand shield or torches are not allowed)" +
				"\n - Throwing Knives - quickly throw a small dagger using a rogue charge; this skill is activated by pressing attack while holding block" +
				"\n - Double Jump - leap to extraordinary heights by stepping on seemingly invisible footholds; activate this skill by pressing jump while in the air" +
				"\n*Double Jump may only be activated once, after leaving the ground" +
				"\n\n",
				m_topic = "Legend Rogue"
			};
			if (!Tutorial.instance.m_texts.Contains(item8))
			{
				Tutorial.instance.m_texts.Add(item8);
			}
			Tutorial.TutorialText item9 = new Tutorial.TutorialText
			{
				m_label = "Legend: Priest",
				m_name = "VL_Priest",
				m_text = "Priest's command a balanced set of offensive and healing abilities that makes them a formidable ally, or foe." +
				"\n\nSkills: Alteration and Evocation" +
				"\n\nSuggested Stats: Intellect or Specialization" +
				"\nSacrifice: Topaz Gemstone (cyan)" +
				"\n\nSanctify: calls down the fiery hammer of Ragnarök to purify a target area." +
				"\nDamage:" +
				"\n Blunt - (10 + 0.5*Evocation)->(20 + 0.75*Evocation)" +
				"\n Fire - (10 + 0.5*Evocation)->(20 + 0.75*Evocation)" +
				"\n Spirit - (10 + 0.5*Evocation)->(20 + 0.75*Evocation)" +
				"\nAoE: 8m + 0.04m*Evocation" +
				"\nCooldown: 45s" +
				"\nEnergy: 70" +
				"\n\nPurge: release a burst of power around the caster that burns enemies and heals allies" +
				"\nDamage:" +
				"\n Fire - (4 + 0.4*Evocation)->(8 + 0.8*Evocation)" +
				"\n Spirit - (4 + 0.4*Evocation)->(8 + 0.8*Evocation)" +
				"\nAoE: 20m + 0.2m*Evocation" +
				"\nHealing: 0.5 + 0.5*Alteration in a 20m + 0.2m*Alteration around the caster" +
				"\nCooldown: 15s" +
				"\nEnergy: 50" +
				"\n\nHeal: a channeled ability that increases heal rate the longer its channeled." +
				"\nHealing:" +
				"\n Initial - 10 + Alteration" +
				"\n Continuous - 2x (pulse count + 0.3*Alteration)" +
				"\nCooldown: 30s" +
				"\nEnergy: 40 (initial), 22.5 per pulse" +
				"\n*Press and hold the ability button to provide continuous healing waves" +
				"\n**Each healing pulse occurs every .5s" +
				"\n***Initial pulse removes 1x negative status effect (poison, burning, smoked, wet, frost)" +
				"\n\nBonus skills:" +
				"\n - Prepare Spirit Binding Vial: cast Heal while blocking to turn 1 Bone Fragment into a Spirit Binding Vial, that can be used to resurrect a fallen ally" +
				"\n - Dying Light - any hit that would kill the priest reduces HP to 1 instead; can only trigger once every 10m" +
				"\n - Clubs Specialization - Causes extra spirit damage with Clubs that scales with Discipline and Character Level",
				m_topic = "Legend Priest"
			};
			if (!Tutorial.instance.m_texts.Contains(item9))
			{
				Tutorial.instance.m_texts.Add(item9);
			}
			Tutorial.TutorialText item10 = new Tutorial.TutorialText
			{
				m_label = "Legend: Enchanter",
				m_name = "VL_Enchanter",
				m_text = "Enchanters use a variety of indirect abilities to shape the situation in their favor." +
				"\n\nSkills: Alteration, Abjuration and Evocation" +
				"\n\nSuggested Stats: Intellect and Endurance, or Specialization and Dexterity and Vigour" +
				"\nSacrifice: Ruby Gemstone (red)" +
				"\n\nWeaken: weakens all enemies in a target area." +
				"\nAoE: 5m + 0.01m * Alteration" +
				"\nDebuff:" +
				"\n Movement Speed -20% + 0.1% * Alteration * (1 + Epic MMO Level / 60) (ranges from -20% to -50%)" +
				"\n Attack Power -15% + 0.015% * Alteration * (1 + Epic MMO Level / 60) (ranges from -15% to -60%)" +
				"\nCooldown: 60s" +
				"\nEnergy: 40" +
				"\n*10% of the damage dealt to a weakened enemy is returned as stamina to the attacker" +
				"\n\nCharm: turn enemies into allies for a short time. Have a chance to break when the charmed creature attacks or take a hit, depending on the Enchanter's skill and the creature's hitpoints." +
				"\nUpon breaking, the creature will become immune to new Charms for a time that depends on creature's hitpoints and the charmer's Illusion skill and character level." +
				"\nDuration: 30s" +
				"\nCooldown: 60s" +
				"\nEnergy: 50" +
				"\n*Charm does not work on boss enemies" +
				"\n\nZone(Biome) Buff: renders a unique, long lasting boon to all nearby allies that differs in each biome." +
				"\nCooldown: 10 minutes" +
				"\nEnergy: 40 (initial) + 60 per second channeled" +
				"\n*Press and hold the ability button to increase the duration and power of the boon" +
				"\n***The enchanter may 'burn' an active zone buff by pressing the ability button + block while a zone buff is active; this creates a burst of electric energy from the casters hands that deals heavy damage based on the time remaining on the zone buff" +
				"\n\nThe benefits of each biome are:" +
				"\n Meadows - Lifesteal of hit; Health regen every 5s; carry capacity increased" +
				"\n\n Black Forest - Chance to deal backstab damage; chance to steal coins with melee attacks; reflects part of the received damage; always under cover" +
				"\n\n Swamp - Poison damage on attack, poison resistance increased; always dry, unless when swimming" +
				"\n\n Mountain - Frost damage on attack, frost resistance increased; cold immunity" +
				"\n\n Plains - Chance to reduce skills cooldown on attack; dodge cost reduced; run speed increased" +
				"\n\n Ocean - Spirit damage on attack; block cost reduced; physical resist increased; swim speed increased" +
				"\n\n Mistland - Lightning damage on attack; lightning resistance increased; eitr or stamina regen every 5s" +
				"\n\n Ashland - Fire damage on attack; fire resistance increased; emits a small amout of light around you" +
				"\n\nBonus skills:" +
				"\n - Elemental Absorption - blocked elemental damage is absorbed by the enchanter as stamina" +
                "\n - Enchant Weapon - adds elemental damage to the Enchanter's attacks according to their element. Activate affinity using Block + Charm. Each hit increase chance of triggering charges." +
				"\n -> Flame Weapon - adds minor fire damage to every attacks (scales with character level and Evocation skill). Triggering charges heals friendly targets nearby and burns nearby foes." +
                "\n -> Ice Weapon - adds minor frost damage and slowness to every attacks (damage, duration and slowness scale with character level and Evocation skill). Triggering charges  restores Eitr to friendly targets nearby and freezs nearby foes." +
                "\n -> Thunder Weapon - adds minor lightning damage to every attacks that jumps to nearby targets (scales with character level and Evocation skill). Triggering charges restores Stamina to friendly targets nearby and shocks targets in a large area." +
                "\n Triggering charges  of any weapon will lower nearby targets skills cooldown by 10%." +
                "\n - Enchant Armor - adds elemental protection to the Enchanter according to their element. Activate affinity using Block + Weaken." +
				"\n -> Flame Armor - Regenerate Health over time, immune to cold, magic resist increased." +
				"\n -> Frost Armor - Chance to cast a frost nova when hit, Slows down melee attackers. Physical resist increased." +
				"\n -> Thunder Armor - Chance to cast a chain lightning in all nearby enemies when hit. Chance to avoid projectiles and stun melee attackers. Movement speed increased." +
				"\n * One Enchant Weapon and one Enchant Armor can be activate at a time. Getting hit while using the same armor element as the weapon will increase weapon charges." +
                "\n Magic weapons specialist: sit down and active Zone Charge to repair your wielded Elemental Magic or Blood Magic Weapon.",
                m_topic = "Legend Enchanter"
			};
			if (!Tutorial.instance.m_texts.Contains(item10))
			{
				Tutorial.instance.m_texts.Add(item10);
			}
			Tutorial.TutorialText item11 = new Tutorial.TutorialText
			{
				m_label = "Legend: Monk",
				m_name = "VL_Monk",
				m_text = "Monks are masters of unarmed combat, turning their body into a living weapon." +
				"\n\nSkills: Discipline" +
				"\n\nSuggested Stats: Strength or Dexterity" +
				"\nSacrifice: Topaz Gemstone (cyan)" +
				"\n\nChi strike: attack with a blow so powerful it creates a shockwave." +
				"\nDamage:" +
				"\n Blunt - 12 + 0.5*Discipline -> 24 + Discipline" +
				"\nCooldown: 1s" +
				"\nEnergy: 3 chi" +
                "\n*Uses chi instead of stamina; build chi through unarmed combat (max stackable Chi charge = 5 to 10, depending on Discipline)" +
				"\n**Activate while on the ground to create a powerful frontal attack; use from sufficient height to propel the monk to the ground, creating a powerful AoE attack" +
				"\n**Active while blocking to Power-Up a Chi charge by spending Stamina (max stackable Chi charge = 1 to 3, depending on Discipline)." +
				"\n\nFlying Kick: launches into a flying whirlwind kick." +
				"\nDamage:" +
				"\n Blunt - 80% + 0.5% of unarmed damage per hit" +
				"\nCooldown: 6s" +
				"\nEnergy: 50" +
				"\n*Can strike multiple times - attack past or above targets to land multiple hits" +
				"\n**Attack directly at the target for an assured strike that will rebound the monk into the air (hint: combo with Chi Strike)" +
				"\n\nChi Bolt: projects condensed energy that detonates on impact." +
				"\nDamage:" +
				"\n Blunt - (10 + Discipline) -> (40 + 2*Discipline)" +
				"\n Spirit - (10 -> 20) + Discipline" +
				"\nAoE - 3m" +
				"\nCooldown: 1s" +
				"\nEnergy: 5 chi" +
				"\n\nBonus Skills:" +
				"\n - Chi - each unarmed attack that hits and each fully blocked attack generates a charge of chi" +
				"\n - Living Weapon - unarmed attacks deal extra spirit damage (Intellect*0.4% + Strength*0.2%). Raw unarmed damage increases with Level (+0.5 * level) or using monk weapons." +
				"\n - Strong Body - unarmed block amount is increased by 1 for each level in Discipline and monks can fall from over double the height before taking damage",
				m_topic = "Legend Monk"
			};
			if (!Tutorial.instance.m_texts.Contains(item11))
			{
				Tutorial.instance.m_texts.Add(item11);
			}
			Tutorial.TutorialText item12 = new Tutorial.TutorialText
			{
				m_label = "Legend: Shaman",
				m_name = "VL_Shaman",
				m_text = "Shaman's are known and respected for their ability to inspire their allies to greatness." +
				"\n\nSkills: Abjuration, Alteration and Evocation" +
				"\n\nSuggested Stats: Intellect and Endurance, or Vigour and Specialization" +
				"\nSacrifice: Humite Gemstone (orange)" +
				"\n\nEnrage: incite allies into a frenzied rage that increases movement and endurance." +
				"\nAugment:" +
				"\n Speed - 120% + 0.2%*Alteration" +
				"\n Stamina Regeneration - 5 + 0.1*Alteration per second" +
				"\nAoE: 30m" +
				"\nDuration: 16s + 0.2s*Alteration" +
				"\nCooldown: 60s" +
				"\nEnergy: 60" +
				"\n*Skill bonus is calculated as the average of all skills for allies, and Alteration skill for the caster" +
				"\n\nShell: surround allies in a protection shell that resists elemental attacks and augments attacks with spirit damage." +
				"\nDamage:" +
				"\n Spirit - 6 + 0.3 * Abjuration added to each attack" +
				"\nBuff: reduces all elemental damage by 40% + 0.6%*Abjuration" +
				"\nAoE: 30m" +
				"\nDuration: 25s + 0.3*Abjuration" +
				"\nCooldown: 60s" +
				"\nEnergy: 80" +
				"\n\nSpirit Shock: generate a powerful blast that shocks all nearby enemies" +
				"\nDamage:" +
				"\n Lightning - 6 + 0.4*Evocation -> 12 + 0.6*Evocation" +
				"\n Spirit - 6 + 0.4*Evocation -> 12 + 0.6*Evocation" +
				"\nAoE: 11m + 0.05m*Evocation" +
				"\nCooldown: 30s" +
				"\nEnergy: 80" +
				"\n* Chain Healing: cast while blocking to heal allys nearby. Healing effectiveness decreased by 30% per extra target." +
				"\n Base Healing: - 15 + 1.5 * Alteration" +
				"\n\nBonus skills:" +
				"\n - Prepare Spirit Binding Vial: cast Shell while blocking to turn 1 Thunderstone into a Spirit Binding Vial, that can be used to resurrect a fallen ally" +
				"\n - Water Glide - press and hold jump to quickly glide across water; rapidly consumes stamina" +
				"\n - Windfury - passive chance to make a triple attack that scales with Alteration skill" +
                "\n - Spirit Guide - any time a creature dies nearby the shaman gains 25 stamina, resets Windfury cooldown and lowers all skills cooldown by 30%",
				m_topic = "Legend Shaman"
			};
			if (!Tutorial.instance.m_texts.Contains(item12))
			{
				Tutorial.instance.m_texts.Add(item12);
			}
			Tutorial.TutorialText item13 = new Tutorial.TutorialText
			{
				m_label = "Legend: Valkyrie",
				m_name = "VL_Valkyrie",
				m_text = "Valkyrie's are a versatile class focused on defense and movement." +
				"\nSacrifice: Sapphire Gemstone (blue)" +
				"\n\nSuggested Stats: Strength or Dexterity, Vigour or Endurance" +
				"\n\nSkills: Discipline and Abjuration" +
				"\n\nBulwark: manifest a powerful shield that reduces all damage to the valkyrie. Blocking while under effects of Bulwark will grant an extra Valkyrie Charge." +
				"\nAugment: damage reduced by 25% + 0.5%*Abjuration" +
				"\nDuration: 12s + 0.2s*Alteration" +
				"\nCooldown: 60s" +
				"\nEnergy: 60" +
				"\n\nStagger: send forth a shock wave that staggers all nearby enemies." +
				"\nAoE: 6m" +
				"\nCooldown: 20s" +
				"\nEnergy: 40" +
				"\n\nLeap: jump high into the air to come crashing down on your enemies." +
				"\nDamage:" +
				"\n Blunt - 2*Discipline -> 3*Discipline + velocity bonus" +
				"\nAoE: 6m + 0.05m*Discipline" +
				"\nCooldown: 15s" +
				"\nEnergy: 50" +
				"\n*Velocity bonus is calculated based on the max height reached above ground" +
				"\n**Leap multiplies existing velocity; triggering leap while running and jumping will produce the longest jumps" +
				"\n\nBonus skills:" +
				"\n - Aegis - successful blocks store energy charges that can be released all at once as" +
				"\n -> Icy Wave: (Block + Leap when using a shield) a wave that extends from the Valkyrie that encases nearby struck enemies in ice" +
				"\n -> Harpoon: (Block + attack) throw a spear that encases a struck enemy in ice",
				m_topic = "Legend Valkyrie"
			};
			if (!Tutorial.instance.m_texts.Contains(item13))
			{
				Tutorial.instance.m_texts.Add(item13);
			}
			Tutorial.TutorialText item14 = new Tutorial.TutorialText
			{
				m_label = "Legend: Ranger",
				m_name = "VL_Ranger",
				m_text = "The Ranger fearless warriors with peerless survival techniques" +
				"\n\nSkills: Discipline and Conjuration" +
				"\n\nSuggested Stats: Strength or Dexterity, Endurance or Intellect, and Specialization for extra crits" +
				"\nSacrifice: Sulfur Gemstone (yellow)" +
				"\n\nShadow Stalk: fade into the shadows gaining a burst of speed and augmenting stealth." +
				"\nAugment:" +
				"\n All movement speed increased by 50% + 1%*Discipline for 3s + 0.03s*Discipline" +
				"\n Stealth movement speed increased by 50% + 1%*Discipline" +
				"\nDuration: 20s + 0.9s*Discipline" +
				"\nCooldown: 45s" +
				"\nEnergy: 40" +
				"\n*Shadow stalk causes enemies to lose track of the ranger" +
				"\n\nShadow Wolf: call a trained shadow wolf to fight by your side." +
				"\nDamage:" +
				"\n Slash - 70 * (0.05 + 0.01*Conjuration)" +
				"\nHealth: 25 + 9*Conjuration" +
				"\nHealth Regeneration: 1 + 0.1*Conjuration every 5s" +
				"\nCooldown: 10m" +
				"\nEnergy: 75" +
				"\n*Shadow wolves will vanish when the player logs out or after the duration expires" +
				"\n*Using it on cooldown will consume any wolf food in your inventory to heal your companion (healing done and stamina cost based on food tier, food priority in inventory follows the same rule of arrows)." +
				"\n*(Neck Tail: 10% health, Boar Meat: 15% health, Deer Meat or Raw Fish: 20% health, Sausages or Lox Meat: 25% health)." +
				"\n*Using it while blocking will dismiss your current Shadow Wolf and lower the cooldown drastically." +
				"\n\nPower Shot: charge the next few projectiles with great velocity and damage." +
				"\nDamage:" +
				"\n Bonus - 40% + 1.5%*Discipline" +
				"\nVelocity doubled" +
				"\nCharge Count: 3 + 0.05*Discipline" +
				"\nCooldown: 60s" +
				"\nEnergy: 60" +
				"\n*Bonus damage applies to all projectiles created by the player (not just arrows)" +
				"\n**Using Power Shot while the buff is still active will refresh the number of charges" +
				"\n*Using Power Shot while blocking will craft Wooden Arrows (consumes 2 Wood to craft 5 arrows)." +
				"\n\nBonus skills:" +
				"\n - Woodland Stride - passive skill that reduces stamina used while running by 25%" +
				"\n - Poison resistance - passive skill that reduces poison damage by 25%" +
				"\n - QuickShot - shoot arrows with 90% draw for 2s following a dodge roll" +
				"\n - Bow/Crossbow Specialization - get up to 25% chance (Specialization * 0.25%) of critical hits using bows or crossbows",
				m_topic = "Legend Ranger"
			};
			if (!Tutorial.instance.m_texts.Contains(item14))
			{
				Tutorial.instance.m_texts.Add(item14);
			}
			Tutorial.TutorialText item15 = new Tutorial.TutorialText
			{
				m_label = "Loot and Experience",
				m_name = "EpicMMO_experience",
				m_text = "Level up by earning experience vanquishing creatures or consuming the trophies you earned. You will get less experience from a creature 15+ levels lower than you. You will deal decreased damage when fighting a creature 15 + levels higher than you, so be careful! You will not get loot of a creature 15 + levels higher than you.",
				m_topic = "Loot and Experience"
			};
			if (!Tutorial.instance.m_texts.Contains(item15))
			{
				Tutorial.instance.m_texts.Add(item15);
			}
			__instance.ShowTutorial("EpicMMO_experience");
			Tutorial.TutorialText item16 = new Tutorial.TutorialText
			{
				m_label = "Professions",
				m_name = "Professions",
				m_text = "You will get to choose a limited number of professions from Alchemist, Blacksmith, Builder, Cook, Farmer, Forager, Jeweler, Lumberjack, Miner, Rancher or Sailor." +
				"\nForgetting a profession will reset your skill back to zero. Be careful before switching carreers!.",
				m_topic = "Professions"
			};
			if (!Tutorial.instance.m_texts.Contains(item16))
			{
				Tutorial.instance.m_texts.Add(item16);
			}
			__instance.ShowTutorial("Valheim Legends skills");
			Tutorial.TutorialText item17 = new Tutorial.TutorialText
			{
				m_label = "Skills and Status",
				m_name = "StatsAndSkills",
				m_text = "Valheim Legends skills get up to 50% bonus from:" +
				"\n- Discipline: Strength or Dexterity" +
				"\n- Abjuration: Vigour or Endurance" +
				"\n- Alteration: Specialization or Intellect" +
				"\n- Conjuration: Endurance or Intellect" +
				"\n- Evocation: Intellect or Specialization" +
				"\n- Illusion: Dexterity or Intellect" +
				"\n\nIntellect will lower the cooldown of all skills, up to 50%" +
				"\n Dexterity will lower the stamina cost of all skills, up to 30%",
				m_topic = "Skills and Status"
			};
			if (!Tutorial.instance.m_texts.Contains(item17))
			{
				Tutorial.instance.m_texts.Add(item17);
			}
			__instance.ShowTutorial("StatsAndSkills");
			Tutorial.TutorialText item18 = new Tutorial.TutorialText
			{
				m_label = "More skills",
				m_name = "MoreSkills",
				m_text = "- Tenacity: reduces the damage you take. Increase it taking damage." +
				"\n- Vitality: increases your health. Increase it by eating food." +
				"\n- Pack Horse: increases your carry weight. Increase it by packing your inventory nearly or past its weight limit." +
				"\nAlso, each weapon have a corresponding Dual Wield skills that increases the offhand damage." +
				"\nTalking about skills, did you know vikings can dive underwater? (press its hotkey while swimming, default: left control)",
				m_topic = "More Skills"
			};
			if (!Tutorial.instance.m_texts.Contains(item18))
			{
				Tutorial.instance.m_texts.Add(item18);
			}
			__instance.ShowTutorial("MoreSkills");
			Tutorial.TutorialText item19 = new Tutorial.TutorialText
			{
				m_label = "World Map",
				m_name = "WorldMap",
				m_text = "Hi, explorer! You need a cartography table to visualize the world map." +
				"\nAfter visiting it once, you will be able to open the world map whenever you are in Resting state." +
				"\nSome say an artifact that detects secrets allow you to visualize the map whenever you want..." +
				"\nThe radius you discover on a map increases with the altitude you are at. Also socketing green magic gemstones in a helmet can increase this discovery radius." +
				"\nIt is also affected by the climate or your Sailing skill if you are sailing." +
				"\nTalking about sailing... hold left control and aim on your ship's mast to set a texture for your ship banner!",
				m_topic = "World Map"
			};
			if (!Tutorial.instance.m_texts.Contains(item19))
			{
				Tutorial.instance.m_texts.Add(item19);
			}
			__instance.ShowTutorial("WorldMap");
			Tutorial.TutorialText item20 = new Tutorial.TutorialText
			{
				m_label = "Hearthstone",
				m_name = "Hearthstone",
				m_text = "You can craft a Hearthstone in a Magic Crystal table. Use it to return to your marked bed. Maybe a Marketstone can be crafted too, and it will take you to the nearest market, if you already found one." +
				"\nTo use any of those you must be in Resting state, and unencumbered.",
				m_topic = "Hearthstone"
			};
			if (!Tutorial.instance.m_texts.Contains(item20))
			{
				Tutorial.instance.m_texts.Add(item20);
			}
			__instance.ShowTutorial("Hearthstone");
		}

		private static bool StartsWithVL(Tutorial.TutorialText s)
		{
			return s.m_name.ToLower().StartsWith("vl_");
		}
	}


	[HarmonyPatch(typeof(RuneStone), "Interact")]
	public static class ClassOfferingTutorial_Patch
	{
		public static void Postfix()
		{
			Player.m_localPlayer.ShowTutorial("VL_Offerings");
		}
	}

	[HarmonyPatch(typeof(PlayerProfile), "LoadPlayerData")]
	public static class LoadSkillsPatch
	{
		public static void Postfix(PlayerProfile __instance, Player player)
		{
			VL_SkillData vL_SkillData = __instance.LoadModData<VL_SkillData>();
			if (player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == DisciplineSkillDef) == null)
			{
				Skills.Skill skill = (Skills.Skill)AccessTools.Method(typeof(Skills), "GetSkill").Invoke(player.GetSkills(), new object[1] { DisciplineSkill });
				skill.m_level = vL_SkillData.level;
				skill.m_accumulator = vL_SkillData.accumulator;
			}
			if (player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == AbjurationSkillDef) == null)
			{
				Skills.Skill skill2 = (Skills.Skill)AccessTools.Method(typeof(Skills), "GetSkill").Invoke(player.GetSkills(), new object[1] { AbjurationSkill });
				skill2.m_level = vL_SkillData.level;
				skill2.m_accumulator = vL_SkillData.accumulator;
			}
			if (player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == AlterationSkillDef) == null)
			{
				Skills.Skill skill3 = (Skills.Skill)AccessTools.Method(typeof(Skills), "GetSkill").Invoke(player.GetSkills(), new object[1] { AlterationSkill });
				skill3.m_level = vL_SkillData.level;
				skill3.m_accumulator = vL_SkillData.accumulator;
			}
			if (player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ConjurationSkillDef) == null)
			{
				Skills.Skill skill4 = (Skills.Skill)AccessTools.Method(typeof(Skills), "GetSkill").Invoke(player.GetSkills(), new object[1] { ConjurationSkill });
				skill4.m_level = vL_SkillData.level;
				skill4.m_accumulator = vL_SkillData.accumulator;
			}
			if (player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == EvocationSkillDef) == null)
			{
				Skills.Skill skill5 = (Skills.Skill)AccessTools.Method(typeof(Skills), "GetSkill").Invoke(player.GetSkills(), new object[1] { EvocationSkill });
				skill5.m_level = vL_SkillData.level;
				skill5.m_accumulator = vL_SkillData.accumulator;
			}
			if (player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == IllusionSkillDef) == null)
			{
				Skills.Skill skill6 = (Skills.Skill)AccessTools.Method(typeof(Skills), "GetSkill").Invoke(player.GetSkills(), new object[1] { IllusionSkill });
				skill6.m_level = vL_SkillData.level;
				skill6.m_accumulator = vL_SkillData.accumulator;
			}
		}
	}

	[HarmonyPatch(typeof(ZNetScene), "Awake")]
	public static class ZNetScene_Awake_Patch
	{
		public static void Prefix(ZNetScene __instance)
		{
			if (!(__instance == null))
			{
				__instance.m_prefabs.Add(VL_Deathsquit);
				__instance.m_prefabs.Add(VL_ShadowWolf);
				__instance.m_prefabs.Add(VL_DemonWolf);
				__instance.m_prefabs.Add(VL_Light);
				__instance.m_prefabs.Add(VL_PoisonBomb);
				__instance.m_prefabs.Add(VL_PoisonBombExplosion);
				__instance.m_prefabs.Add(VL_SanctifyHammer);
				__instance.m_prefabs.Add(VL_ThrowingKnife);
				__instance.m_prefabs.Add(VL_PsiBolt);
				__instance.m_prefabs.Add(VL_Charm);
				__instance.m_prefabs.Add(VL_FrostDagger);
				__instance.m_prefabs.Add(VL_ValkyrieSpear);
				__instance.m_prefabs.Add(VL_ShadowWolfAttack);
				__instance.m_prefabs.Add(fx_VL_Lightburst);
				__instance.m_prefabs.Add(fx_VL_ParticleLightburst);
				__instance.m_prefabs.Add(fx_VL_ParticleLightSuction);
				__instance.m_prefabs.Add(fx_VL_ReverseLightburst);
				__instance.m_prefabs.Add(fx_VL_BlinkStrike);
				__instance.m_prefabs.Add(fx_VL_QuickShot);
				__instance.m_prefabs.Add(fx_VL_HealPulse);
				__instance.m_prefabs.Add(fx_VL_Purge);
				__instance.m_prefabs.Add(fx_VL_Smokeburst);
				__instance.m_prefabs.Add(fx_VL_Shadowburst);
				__instance.m_prefabs.Add(fx_VL_Shockwave);
				__instance.m_prefabs.Add(fx_VL_FlyingKick);
				__instance.m_prefabs.Add(fx_VL_MeteorSlam);
				__instance.m_prefabs.Add(fx_VL_Weaken);
				__instance.m_prefabs.Add(fx_VL_WeakenStatus);
				__instance.m_prefabs.Add(fx_VL_Shock);
				__instance.m_prefabs.Add(fx_VL_ParticleTailField);
				__instance.m_prefabs.Add(fx_VL_ParticleFieldBurst);
				__instance.m_prefabs.Add(fx_VL_HeavyCrit);
				__instance.m_prefabs.Add(fx_VL_ChiPulse);
				__instance.m_prefabs.Add(fx_VL_Replica);
				__instance.m_prefabs.Add(fx_VL_ReplicaCreate);
				__instance.m_prefabs.Add(fx_VL_ForwardLightningShock);
				__instance.m_prefabs.Add(fx_VL_Flames);
				__instance.m_prefabs.Add(fx_VL_FlameBurst);
				__instance.m_prefabs.Add(fx_VL_AbsorbSpirit);
				__instance.m_prefabs.Add(fx_VL_ForceWall);
				__instance.m_prefabs.Add(fx_VL_ShieldRelease);
			}
		}
	}

	[HarmonyPatch(typeof(ObjectDB), "CopyOtherDB")]
	public static class ObjectDB_CopyOtherDB_Patch
	{
		public static void Postfix()
		{
			Add_VL_Assets();
		}
	}

	[HarmonyPatch(typeof(ObjectDB), "Awake")]
	public static class ObjectDB_Awake_Patch
	{
		public static void Postfix()
		{
			Add_VL_Assets();
		}
	}

	public static Harmony _Harmony;

	public const string Version = "0.5.0";

	public const float VersionF = 0.5f;

	public const string ModName = "Valheim Legends";

	public static bool playerEnabled = true;

	public static List<VL_Player> vl_playerList;

	public static VL_Player vl_player;

	public static Sprite RiposteIcon;

	public static Sprite RogueIcon;

	public static Sprite MonkIcon;

	public static Sprite RangerIcon;

	public static Sprite ValkyrieIcon;

	public static Sprite WeakenIcon;

	public static Sprite BiomeMeadowsIcon;

	public static Sprite BiomeBlackForestIcon;

	public static Sprite BiomeSwampIcon;

	public static Sprite BiomeMountainIcon;

	public static Sprite BiomePlainsIcon;

	public static Sprite BiomeOceanIcon;

	public static Sprite BiomeMistIcon;

	public static Sprite BiomeAshIcon;

	public static ConfigEntry<bool> modEnabled;

	public static ConfigEntry<string> Ability1_Hotkey;

	public static ConfigEntry<string> Ability1_Hotkey_Combo;

	public static ConfigEntry<string> Ability2_Hotkey;

	public static ConfigEntry<string> Ability2_Hotkey_Combo;

	public static ConfigEntry<string> Ability3_Hotkey;

	public static ConfigEntry<string> Ability3_Hotkey_Combo;

	public static ConfigEntry<float> vl_svr_energyCostMultiplier;

	public static ConfigEntry<float> vl_svr_cooldownMultiplier;

	public static ConfigEntry<float> vl_svr_abilityDamageMultiplier;

	public static ConfigEntry<float> vl_svr_skillGainMultiplier;

	public static ConfigEntry<float> vl_svr_unarmedDamageMultiplier;

	public static ConfigEntry<float> icon_X_Offset;

	public static ConfigEntry<float> icon_Y_Offset;

	public static ConfigEntry<bool> showAbilityIcons;

	public static ConfigEntry<string> iconAlignment;

	public static ConfigEntry<string> chosenClass;

	public static ConfigEntry<bool> vl_svr_allowAltarClassChange;

	public static ConfigEntry<bool> vl_svr_enforceConfigClass;

	public static ConfigEntry<bool> vl_svr_aoeRequiresLoS;

	public static readonly Color abilityCooldownColor = new Color(1f, 0.3f, 0.3f, 0.5f);

	public static ConfigEntry<float> vl_svr_berserkerDash;

	public static ConfigEntry<float> vl_svr_berserkerBerserk;

	public static ConfigEntry<float> vl_svr_berserkerExecute;

	public static ConfigEntry<float> vl_svr_berserkerBonusDamage;

	public static ConfigEntry<float> vl_svr_berserkerBonus2h;

	public static ConfigEntry<string> vl_svr_berserkerItem;

	public static ConfigEntry<float> vl_svr_druidVines;

	public static ConfigEntry<float> vl_svr_druidRegen;

	public static ConfigEntry<float> vl_svr_druidDefenders;

	public static ConfigEntry<float> vl_svr_druidBonusSeeds;

	public static ConfigEntry<string> vl_svr_druidItem;

	public static ConfigEntry<float> vl_svr_duelistSeismicSlash;

	public static ConfigEntry<float> vl_svr_duelistRiposte;

	public static ConfigEntry<float> vl_svr_duelistHipShot;

	public static ConfigEntry<float> vl_svr_duelistBonusParry;

	public static ConfigEntry<string> vl_svr_duelistItem;

	public static ConfigEntry<float> vl_svr_enchanterWeaken;

	public static ConfigEntry<float> vl_svr_enchanterCharm;

	public static ConfigEntry<float> vl_svr_enchanterBiome;

	public static ConfigEntry<float> vl_svr_enchanterBiomeShock;

	public static ConfigEntry<float> vl_svr_enchanterBonusElementalBlock;

	public static ConfigEntry<float> vl_svr_enchanterBonusElementalTouch;

	public static ConfigEntry<string> vl_svr_enchanterItem;

	public static ConfigEntry<float> vl_svr_mageFireball;

	public static ConfigEntry<float> vl_svr_mageFrostDagger;

	public static ConfigEntry<float> vl_svr_mageFrostNova;

	public static ConfigEntry<float> vl_svr_mageInferno;

	public static ConfigEntry<float> vl_svr_mageMeteor;

	public static ConfigEntry<string> vl_svr_mageItem;

	public static ConfigEntry<float> vl_svr_metavokerLight;

	public static ConfigEntry<float> vl_svr_metavokerReplica;

	public static ConfigEntry<float> vl_svr_metavokerWarpDamage;

	public static ConfigEntry<float> vl_svr_metavokerWarpDistance;

	public static ConfigEntry<float> vl_svr_metavokerBonusSafeFallCost;

	public static ConfigEntry<float> vl_svr_metavokerBonusForceWave;

	public static ConfigEntry<string> vl_svr_metavokerItem;

	public static ConfigEntry<float> vl_svr_monkChiPunch;

	public static ConfigEntry<float> vl_svr_monkChiSlam;

	public static ConfigEntry<float> vl_svr_monkChiBlast;

	public static ConfigEntry<float> vl_svr_monkFlyingKick;

	public static ConfigEntry<float> vl_svr_monkBonusBlock;

	public static ConfigEntry<float> vl_svr_monkSurge;

	public static ConfigEntry<float> vl_svr_monkChiDuration;

	public static ConfigEntry<string> vl_svr_monkItem;

	public static ConfigEntry<float> vl_svr_priestHeal;

	public static ConfigEntry<float> vl_svr_priestPurgeHeal;

	public static ConfigEntry<float> vl_svr_priestPurgeDamage;

	public static ConfigEntry<float> vl_svr_priestSanctify;

	public static ConfigEntry<float> vl_svr_priestBonusDyingLightCooldown;

	public static ConfigEntry<string> vl_svr_priestItem;

	public static ConfigEntry<float> vl_svr_rangerPowerShot;

	public static ConfigEntry<float> vl_svr_rangerShadowWolf;

	public static ConfigEntry<float> vl_svr_rangerShadowStalk;

	public static ConfigEntry<float> vl_svr_rangerBonusPoisonResistance;

	public static ConfigEntry<float> vl_svr_rangerBonusRunCost;

	public static ConfigEntry<string> vl_svr_rangerItem;

	public static ConfigEntry<float> vl_svr_rogueBackstab;

	public static ConfigEntry<float> vl_svr_rogueFadeCooldown;

	public static ConfigEntry<float> vl_svr_roguePoisonBomb;

	public static ConfigEntry<float> vl_svr_rogueBonusThrowingDagger;

	public static ConfigEntry<float> vl_svr_rogueTrickCharge;

	public static ConfigEntry<string> vl_svr_rogueItem;

	public static ConfigEntry<float> vl_svr_shamanSpiritShock;

	public static ConfigEntry<float> vl_svr_shamanEnrage;

	public static ConfigEntry<float> vl_svr_shamanShell;

	public static ConfigEntry<float> vl_svr_shamanBonusSpiritGuide;

	public static ConfigEntry<float> vl_svr_shamanBonusWaterGlideCost;

	public static ConfigEntry<string> vl_svr_shamanItem;

	public static ConfigEntry<float> vl_svr_valkyrieLeap;

	public static ConfigEntry<float> vl_svr_valkyrieStaggerCooldown;

	public static ConfigEntry<float> vl_svr_valkyrieBulwark;

	public static ConfigEntry<float> vl_svr_valkyrieBonusChillWave;

	public static ConfigEntry<float> vl_svr_valkyrieBonusIceLance;

	public static ConfigEntry<float> vl_svr_valkyrieChargeDuration;

	public static ConfigEntry<string> vl_svr_valkyrieItem;

	public static long ServerID;

	private static readonly Type patchType = typeof(ValheimLegends);

	public static Sprite Ability1_Sprite;

	public static Sprite Ability2_Sprite;

	public static Sprite Ability3_Sprite;

	public static Sprite DyingLight_Sprite;

	public static string Ability1_Name;

	public static string Ability2_Name;

	public static string Ability3_Name;

	public static string Ability1_Description;

	public static string Ability2_Description;

	public static string Ability3_Description;

	public static List<RectTransform> abilitiesStatus = new List<RectTransform>();

	public static bool shouldUseGuardianPower = true;

	public static bool shouldValkyrieImpact = false;

	public static bool isChanneling = false;

    public static bool channelingBlocksMovement = true;

    public static int channelingCancelDelay = 0;

	public static bool isChargingDash = false;

	public static int dashCounter = 0;

	public static int logCheck = 0;

	public static int animationCountdown = 0;

	public static readonly int DisciplineSkillID = 781;

	public static readonly int AbjurationSkillID = 791;

	public static readonly int AlterationSkillID = 792;

	public static readonly int ConjurationSkillID = 793;

	public static readonly int EvocationSkillID = 794;

	public static readonly int IllusionSkillID = 795;

	public static Skills.SkillType DisciplineSkill = (Skills.SkillType)DisciplineSkillID;

	public static Skills.SkillType AbjurationSkill = (Skills.SkillType)AbjurationSkillID;

	public static Skills.SkillType AlterationSkill = (Skills.SkillType)AlterationSkillID;

	public static Skills.SkillType ConjurationSkill = (Skills.SkillType)ConjurationSkillID;

	public static Skills.SkillType EvocationSkill = (Skills.SkillType)EvocationSkillID;

	public static Skills.SkillType IllusionSkill = (Skills.SkillType)IllusionSkillID;

	public static Skills.SkillDef DisciplineSkillDef;

	public static Skills.SkillDef AbjurationSkillDef;

	public static Skills.SkillDef AlterationSkillDef;

	public static Skills.SkillDef ConjurationSkillDef;

	public static Skills.SkillDef EvocationSkillDef;

	public static Skills.SkillDef IllusionSkillDef;

	public static List<Skills.SkillDef> legendsSkills = new List<Skills.SkillDef>();

	private static int Script_WolfAttackMask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_noenv", "character_trigger");

	private static GameObject VL_Deathsquit;

	private static GameObject VL_ShadowWolf;

	private static GameObject VL_DemonWolf;

	private static GameObject VL_Light;

	private static GameObject VL_SanctifyHammer;

	private static GameObject VL_PoisonBomb;

	private static GameObject VL_PoisonBombExplosion;

	private static GameObject VL_ThrowingKnife;

	private static GameObject VL_PsiBolt;

	private static GameObject VL_Charm;

	private static GameObject VL_FrostDagger;

	private static GameObject VL_ValkyrieSpear;

	private static GameObject VL_ShadowWolfAttack;

	private static GameObject fx_VL_Lightburst;

	private static GameObject fx_VL_ParticleLightburst;

	private static GameObject fx_VL_ParticleLightSuction;

	private static GameObject fx_VL_ReverseLightburst;

	private static GameObject fx_VL_BlinkStrike;

	private static GameObject fx_VL_QuickShot;

	private static GameObject fx_VL_Purge;

	private static GameObject fx_VL_Smokeburst;

	private static GameObject fx_VL_Shadowburst;

	private static GameObject fx_VL_Shockwave;

	private static GameObject fx_VL_FlyingKick;

	private static GameObject fx_VL_MeteorSlam;

	private static GameObject fx_VL_Weaken;

	private static GameObject fx_VL_WeakenStatus;

	private static GameObject fx_VL_Shock;

	private static GameObject fx_VL_ParticleTailField;

	private static GameObject fx_VL_ParticleFieldBurst;

	private static GameObject fx_VL_ChiPulse;

	private static GameObject fx_VL_HeavyCrit;

	private static GameObject fx_VL_HealPulse;

	private static GameObject fx_VL_Replica;

	private static GameObject fx_VL_ReplicaCreate;

	private static GameObject fx_VL_ForwardLightningShock;

	private static GameObject fx_VL_Flames;

	private static GameObject fx_VL_FlameBurst;

	private static GameObject fx_VL_AbsorbSpirit;

	private static GameObject fx_VL_ForceWall;

	private static GameObject fx_VL_ShieldRelease;

	public static AnimationClip anim_player_float;

	public static int GetPlayerClassNum
	{
		get
		{
			if (vl_player.vl_class == PlayerClass.Berserker)
			{
				return 1;
			}
			if (vl_player.vl_class == PlayerClass.Druid)
			{
				return 2;
			}
			if (vl_player.vl_class == PlayerClass.Metavoker)
			{
				return 3;
			}
			if (vl_player.vl_class == PlayerClass.Mage)
			{
				return 4;
			}
			if (vl_player.vl_class == PlayerClass.Priest)
			{
				return 5;
			}
			if (vl_player.vl_class == PlayerClass.Monk)
			{
				return 7;
			}
			if (vl_player.vl_class == PlayerClass.Ranger)
			{
				return 8;
			}
			if (vl_player.vl_class == PlayerClass.Duelist)
			{
				return 9;
			}
			if (vl_player.vl_class == PlayerClass.Enchanter)
			{
				return 10;
			}
			if (vl_player.vl_class == PlayerClass.Rogue)
			{
				return 11;
			}
			if (vl_player.vl_class == PlayerClass.Shaman)
			{
				return 16;
			}
			if (vl_player.vl_class == PlayerClass.Valkyrie)
			{
				return 32;
			}
			return 0;
		}
	}

	public static bool ClassIsValid
	{
		get
		{
			if (vl_player != null)
			{
				return vl_player.vl_class != PlayerClass.None;
			}
			return false;
		}
	}

	private static void RemoveSummonedWolf()
	{
		foreach (Character allCharacter in Character.GetAllCharacters())
		{
			if (!(allCharacter != null) || allCharacter.GetSEMan() == null)
			{
				continue;
			}
			if (allCharacter.GetSEMan().HaveStatusEffect("SE_VL_Companion".GetStableHashCode()))
			{
				SE_Companion sE_Companion = allCharacter.GetSEMan().GetStatusEffect("SE_VL_Companion".GetStableHashCode()) as SE_Companion;
				if (sE_Companion.summoner == Player.m_localPlayer)
				{
					MonsterAI component = allCharacter.GetComponent<MonsterAI>();
					if (component != null)
					{
						component.SetFollowTarget(null);
					}
					SE_Ability2_CD sE_Ability2_CD = Player.m_localPlayer.GetSEMan().GetStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()) as SE_Ability2_CD;
					float new_mTTL = Mathf.Min(sE_Ability2_CD.m_ttl, Mathf.Sqrt(sE_Ability2_CD.m_ttl / allCharacter.GetHealthPercentage()));
					Player.m_localPlayer.GetSEMan().RemoveStatusEffect(sE_Ability2_CD);
					StatusEffect statusEffect3 = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
					statusEffect3.m_ttl = new_mTTL;
					Player.m_localPlayer.GetSEMan().AddStatusEffect(statusEffect3);
					allCharacter.m_faction = Character.Faction.MountainMonsters;
					HitData hitData = new HitData();
					hitData.m_damage.m_slash = 9999f;
					allCharacter.Damage(hitData);
				}
			}
			else if (allCharacter.GetSEMan().HaveStatusEffect("SE_VL_Charm".GetStableHashCode()))
			{
				SE_Charm sE_Charm = (SE_Charm)allCharacter.GetSEMan().GetStatusEffect("SE_VL_Charm".GetStableHashCode());
				float charmPower = sE_Charm.charmPower;
				allCharacter.m_faction = sE_Charm.originalFaction;
				allCharacter.SetTamed(tamed: false);
				StatusEffect statusEffect = (SE_CharmImmunity)ScriptableObject.CreateInstance(typeof(SE_CharmImmunity));
				statusEffect.m_ttl = Mathf.Clamp(allCharacter.GetHealthPercentage() * VL_GlobalConfigs.g_CooldownModifer * 60f, 5f, 300f);
				allCharacter.GetSEMan().AddStatusEffect(statusEffect);
				UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_Lightburst"), allCharacter.GetEyePoint(), UnityEngine.Quaternion.identity);
			}
		}
	}

	private static bool IsValidTarget(IDestructible destr, ref bool hitCharacter, Character owner, bool m_dodgeable)
	{
		Character character = destr as Character;
		if ((bool)character)
		{
			if (character == owner)
			{
				return false;
			}
			if (owner != null && !owner.IsPlayer() && !BaseAI.IsEnemy(owner, character))
			{
				return false;
			}
			if (m_dodgeable && character.IsDodgeInvincible())
			{
				return false;
			}
			hitCharacter = true;
		}
		return true;
	}

	private void Awake()
	{
		chosenClass = base.Config.Bind("General", "chosenClass", "None", "Assigns a class to the player if no class is assigned.\nThis will not overwrite an existing class selection.\nA value of None will not attempt to assign any class.");
		vl_svr_allowAltarClassChange = base.Config.Bind("General", "vl_svr_allowAltarClassChange", defaultValue: true, "Allows class changing at the altar; if disabled, the only way to change class will be via console or the mod configs.");
		vl_svr_enforceConfigClass = base.Config.Bind("General", "vl_svr_enforceConfigClass", defaultValue: false, "True - always sets the player class to this value when the player logs in. False - uses player profile to determine class\nDoes not apply if the chosen class is None.");
		vl_svr_aoeRequiresLoS = base.Config.Bind("General", "vl_svr_aoeRequiresLoS", defaultValue: true, "True - all AoE attacks require Line of Sight to the impact point.\nFalse - uses default game behavior for AoE attacks.");
		showAbilityIcons = base.Config.Bind("Display", "showAbilityIcons", defaultValue: true, "Displays Icons on Hud for each ability");
		iconAlignment = base.Config.Bind("Display", "iconAlignment", "horizontal", "Aligns icons horizontally or vertically off the guardian power icon; options are horizontal or vertical");
		icon_X_Offset = base.Config.Bind("Display", "icon_X_Offset", 0f, "Offsets the icon bar horizontally. The icon bar is anchored to the Guardian power icon.");
		icon_Y_Offset = base.Config.Bind("Display", "icon_Y_Offset", 0f, "Offsets the icon bar vertically. The icon bar is anchored to the Guardian power icon.");
		Ability1_Hotkey = base.Config.Bind("Keybinds", "Ability1_Hotkey", "Z", "Ability 1 Hotkey\nUse mouse # to bind an ability to a mouse button\nThe # represents the mouse button; mouse 0 is left click, mouse 1 right click, etc");
		Ability1_Hotkey_Combo = base.Config.Bind("Keybinds", "Ability1_Hotkey_Combo", "", "Ability 1 Combination Key - entering a value will trigger the ability only when both the Hotkey and Hotkey_Combo buttons are pressed\nAllows input from a combination of keys when a value is entered for the combo key\nIf only one key is used, leave the combo key blank\nExamples: space, Q, left shift, left ctrl, right alt, right cmd");
		Ability2_Hotkey = base.Config.Bind("Keybinds", "Ability2_Hotkey", "X", "Ability 2 Hotkey");
		Ability2_Hotkey_Combo = base.Config.Bind("Keybinds", "Ability2_Hotkey_Combo", "", "Ability 2 Combination Key");
		Ability3_Hotkey = base.Config.Bind("Keybinds", "Ability3_Hotkey", "C", "Ability 3 Hotkey");
		Ability3_Hotkey_Combo = base.Config.Bind("Keybinds", "Ability3_Hotkey_Combo", "", "Ability 3 Combination Key");
		vl_svr_energyCostMultiplier = base.Config.Bind("Modifiers", "vl_svr_energyCostMultiplier", 100f, "Ability modifiers are always enforced by the server host\nThis value multiplied on overall ability use energy cost");
		vl_svr_cooldownMultiplier = base.Config.Bind("Modifiers", "vl_svr_cooldownMultiplier", 100f, "This value multiplied on overall cooldown time of abilities");
		vl_svr_abilityDamageMultiplier = base.Config.Bind("Modifiers", "vl_svr_abilityDamageMultiplier", 100f, "This value multiplied on overall ability power");
		vl_svr_skillGainMultiplier = base.Config.Bind("Modifiers", "vl_svr_skillGainMultiplier", 100f, "This value modifies the amount of skill experience gained after using an ability");
		vl_svr_unarmedDamageMultiplier = base.Config.Bind("Modifiers", "vl_svr_unarmedDamageMultiplier", 100f, "This value modifies unarmed damage increased by unarmed skill\nOnly use unarmed damage modifiers from a single mod");
		vl_svr_berserkerDash = base.Config.Bind("Class Modifiers", "vl_svr_berserkerDash", 100f, "Modifies the damage dealt by Dash");
		vl_svr_berserkerBerserk = base.Config.Bind("Class Modifiers", "vl_svr_berserkerBerserk", 100f, "Modifies the damage bonus from Berserk");
		vl_svr_berserkerExecute = base.Config.Bind("Class Modifiers", "vl_svr_berserkerExecute", 100f, "Modifies the damage bonus from Execute");
		vl_svr_berserkerBonusDamage = base.Config.Bind("Class Modifiers", "vl_svr_berserkerBonusDamage", 100f, "Modifies the damage Bonus gained from missing health");
		vl_svr_berserkerBonus2h = base.Config.Bind("Class Modifiers", "vl_svr_berserkerBonus2h", 100f, "Decreases the stamina cost when using 2h weapons");
		vl_svr_berserkerItem = base.Config.Bind("Class Modifiers", "vl_svr_berserkerItem", "item_bonefragments", "Sacrifice this item at Eikthyr's altar to become a berserker");
		vl_svr_druidVines = base.Config.Bind("Class Modifiers", "vl_svr_druidVines", 100f, "Modifies the damage of Vines");
		vl_svr_druidRegen = base.Config.Bind("Class Modifiers", "vl_svr_druidRegen", 100f, "Modifies the amount healed by Regenerate");
		vl_svr_druidDefenders = base.Config.Bind("Class Modifiers", "vl_svr_druidDefenders", 100f, "Modifies the damage of summoned Defenders");
		vl_svr_druidBonusSeeds = base.Config.Bind("Class Modifiers", "vl_svr_druidBonusSeeds", 100f, "Modifies the stamina regeneration from consuming seeds");
		vl_svr_druidItem = base.Config.Bind("Class Modifiers", "vl_svr_druidItem", "item_dandelion", "Sacrifice this item at Eikthyr's altar to become a druid");
		vl_svr_duelistSeismicSlash = base.Config.Bind("Class Modifiers", "vl_svr_duelistSeismicSlash", 100f, "Modifies the damage dealt by Seismic Slash");
		vl_svr_duelistRiposte = base.Config.Bind("Class Modifiers", "vl_svr_duelistRiposte", 100f, "Modifies the damage dealt by Riposte");
		vl_svr_duelistHipShot = base.Config.Bind("Class Modifiers", "vl_svr_duelistHipShot", 100f, "Modifies the damage dealt by Hip Shot");
		vl_svr_duelistBonusParry = base.Config.Bind("Class Modifiers", "vl_svr_duelistBonusParry", 100f, "Modifies the parry bonus");
		vl_svr_duelistItem = base.Config.Bind("Class Modifiers", "vl_svr_duelistItem", "item_thistle", "Sacrifice this item at Eikthyr's altar to become a duelist");
		vl_svr_enchanterWeaken = base.Config.Bind("Class Modifiers", "vl_svr_enchanterWeaken", 100f, "Modifies the power of Weaken");
		vl_svr_enchanterCharm = base.Config.Bind("Class Modifiers", "vl_svr_enchanterCharm", 100f, "Modifies the duration of Charm");
		vl_svr_enchanterBiome = base.Config.Bind("Class Modifiers", "vl_svr_enchanterBiome", 100f, "Modifies the duration of Biome buffs");
		vl_svr_enchanterBiomeShock = base.Config.Bind("Class Modifiers", "vl_svr_enchanterBiomeShock", 100f, "Modifies the damage dealt by Biome Shock");
		vl_svr_enchanterBonusElementalBlock = base.Config.Bind("Class Modifiers", "vl_svr_enchanterBonusElementalBlock", 100f, "Modifies the amount of stamina gained when blocking elemental damage");
		vl_svr_enchanterBonusElementalTouch = base.Config.Bind("Class Modifiers", "vl_svr_enchanterBonusElementalTouch", 100f, "Modifies the damage of elemental attacks caused by elemental touch");
		vl_svr_enchanterItem = base.Config.Bind("Class Modifiers", "vl_svr_enchanterItem", "item_resin", "Sacrifice this item at Eikthyr's altar to become an enchanter");
		vl_svr_mageFireball = base.Config.Bind("Class Modifiers", "vl_svr_mageFireball", 100f, "Modifies the damage and speed of Fireball");
		vl_svr_mageFrostDagger = base.Config.Bind("Class Modifiers", "vl_svr_mageFrostDagger", 100f, "Modifies the damage of Frost Daggers");
		vl_svr_mageFrostNova = base.Config.Bind("Class Modifiers", "vl_svr_mageFrostNova", 100f, "Modifies the damage of Frost Nova");
		vl_svr_mageInferno = base.Config.Bind("Class Modifiers", "vl_svr_mageInferno", 100f, "Modifies the damage of Inferno");
		vl_svr_mageMeteor = base.Config.Bind("Class Modifiers", "vl_svr_mageMeteor", 100f, "Modifies the damage of Meteors");
		vl_svr_mageItem = base.Config.Bind("Class Modifiers", "vl_svr_mageItem", "item_coal", "Sacrifice this item at Eikthyr's altar to become a mage");
		vl_svr_metavokerLight = base.Config.Bind("Class Modifiers", "vl_svr_metavokerLight", 100f, "Modifies the damage and force of Light");
		vl_svr_metavokerReplica = base.Config.Bind("Class Modifiers", "vl_svr_metavokerReplica", 100f, "Modifies the damage dealt by Replicas");
		vl_svr_metavokerWarpDamage = base.Config.Bind("Class Modifiers", "vl_svr_metavokerWarpDamage", 100f, "Modifies the damage dealt by excess Warp energy");
		vl_svr_metavokerWarpDistance = base.Config.Bind("Class Modifiers", "vl_svr_metavokerWarpDistance", 100f, "Modifies the distance travelled when warping\n**WARNING: excessive warp distance can cause unpredictable results");
		vl_svr_metavokerBonusSafeFallCost = base.Config.Bind("Class Modifiers", "vl_svr_metavokerBonusSafeFallCost", 100f, "Modifies the stamina cost of safe fall");
		vl_svr_metavokerBonusForceWave = base.Config.Bind("Class Modifiers", "vl_svr_metavokerBonusForceWave", 100f, "Modifies the force and damage of Force Wall");
		vl_svr_metavokerItem = base.Config.Bind("Class Modifiers", "vl_svr_metavokerItem", "item_raspberries", "Sacrifice this item at Eikthyr's altar to become a metavoker");
		vl_svr_monkChiPunch = base.Config.Bind("Class Modifiers", "vl_svr_monkChiPunch", 100f, "Modifies the power of Chi Punch");
		vl_svr_monkChiSlam = base.Config.Bind("Class Modifiers", "vl_svr_monkChiSlam", 100f, "Modifies the power of Chi Slam");
		vl_svr_monkChiBlast = base.Config.Bind("Class Modifiers", "vl_svr_monkChiBlast", 100f, "Modifies the power of Chi Blast");
		vl_svr_monkFlyingKick = base.Config.Bind("Class Modifiers", "vl_svr_monkFlyingKick", 100f, "Modifies the power of Flying Kick");
		vl_svr_monkBonusBlock = base.Config.Bind("Class Modifiers", "vl_svr_monkBonusBlock", 100f, "Modifies the block bonus while unarmed");
		vl_svr_monkSurge = base.Config.Bind("Class Modifiers", "vl_svr_monkSurge", 100f, "Modifies the health and stamina restored while Chi surging");
		vl_svr_monkChiDuration = base.Config.Bind("Class Modifiers", "vl_svr_monkChiDuration", 100f, "Modifies how quickly chi decreases\nLower is faster");
		vl_svr_monkItem = base.Config.Bind("Class Modifiers", "vl_svr_monkItem", "item_wood", "Sacrifice this item at Eikthyr's altar to become a monk");
		vl_svr_priestHeal = base.Config.Bind("Class Modifiers", "vl_svr_priestHeal", 100f, "Modifies the power of Heal");
		vl_svr_priestPurgeHeal = base.Config.Bind("Class Modifiers", "vl_svr_priestPurgeHeal", 100f, "Modifies the healing amount of Purge");
		vl_svr_priestPurgeDamage = base.Config.Bind("Class Modifiers", "vl_svr_priestPurgeDamage", 100f, "Modifies the damage amount of Purge");
		vl_svr_priestSanctify = base.Config.Bind("Class Modifiers", "vl_svr_priestSanctify", 100f, "Modifies the power of Sanctify");
		vl_svr_priestBonusDyingLightCooldown = base.Config.Bind("Class Modifiers", "vl_svr_priestBonusDyingLightCooldown", 100f, "Modifies the cooldown of Dying Light");
		vl_svr_priestItem = base.Config.Bind("Class Modifiers", "vl_svr_priestItem", "item_stone", "Sacrifice this item at Eikthyr's altar to become a priest");
		vl_svr_rangerPowerShot = base.Config.Bind("Class Modifiers", "vl_svr_rangerPowerShot", 100f, "Modifies the damage bonus of Power Shot");
		vl_svr_rangerShadowWolf = base.Config.Bind("Class Modifiers", "vl_svr_rangerShadowWolf", 100f, "Modifies the damage of Shadow Wolves");
		vl_svr_rangerShadowStalk = base.Config.Bind("Class Modifiers", "vl_svr_rangerShadowStalk", 100f, "Modifies the movement speed from Shadow Stalk");
		vl_svr_rangerBonusPoisonResistance = base.Config.Bind("Class Modifiers", "vl_svr_rangerBonusPoisonResistance", 100f, "Modifies the bonus from Poison Resitance");
		vl_svr_rangerBonusRunCost = base.Config.Bind("Class Modifiers", "vl_svr_rangerBonusRunCost", 100f, "Modifies the bonus stamina reduction while running");
		vl_svr_rangerItem = base.Config.Bind("Class Modifiers", "vl_svr_rangerItem", "item_boar_meat", "Sacrifice this item at Eikthyr's altar to become a ranger");
		vl_svr_rogueBackstab = base.Config.Bind("Class Modifiers", "vl_svr_rogueBackstab", 100f, "Modifies the damage of Backstab");
		vl_svr_rogueFadeCooldown = base.Config.Bind("Class Modifiers", "vl_svr_rogueFadeCooldown", 100f, "Modifies the cooldown of Fade");
		vl_svr_roguePoisonBomb = base.Config.Bind("Class Modifiers", "vl_svr_roguePoisonBomb", 100f, "Modifies the damage dealt by Poison Bomb");
		vl_svr_rogueBonusThrowingDagger = base.Config.Bind("Class Modifiers", "vl_svr_rogueBonusThrowingDagger", 100f, "Modifies the damage dealt by Throwing knives");
		vl_svr_rogueTrickCharge = base.Config.Bind("Class Modifiers", "vl_svr_rogueTrickCharge", 100f, "Modifies how quickly trick points increase");
		vl_svr_rogueItem = base.Config.Bind("Class Modifiers", "vl_svr_rogueItem", "item_honey", "Sacrifice this item at Eikthyr's altar to become a rogue");
		vl_svr_shamanSpiritShock = base.Config.Bind("Class Modifiers", "vl_svr_shamanSpiritShock", 100f, "Modifies the power of Spirit Shock");
		vl_svr_shamanEnrage = base.Config.Bind("Class Modifiers", "vl_svr_shamanEnrage", 100f, "Modifies the stamina regeneration from Enrage");
		vl_svr_shamanShell = base.Config.Bind("Class Modifiers", "vl_svr_shamanShell", 100f, "Modifies the elemental protection applied by Shell");
		vl_svr_shamanBonusSpiritGuide = base.Config.Bind("Class Modifiers", "vl_svr_shamanBonusSpiritGuide", 100f, "Modifies the amount of stamina gained from Spirit Guide");
		vl_svr_shamanBonusWaterGlideCost = base.Config.Bind("Class Modifiers", "vl_svr_shamanBonusWaterGlideCost", 100f, "Modifies the stamina cost to Water Glide");
		vl_svr_shamanItem = base.Config.Bind("Class Modifiers", "vl_svr_shamanItem", "item_greydwarfeye", "Sacrifice this item at Eikthyr's altar to become a shaman");
		vl_svr_valkyrieLeap = base.Config.Bind("Class Modifiers", "vl_svr_valkyrieLeap", 100f, "Modifies the damage of Leap");
		vl_svr_valkyrieStaggerCooldown = base.Config.Bind("Class Modifiers", "vl_svr_valkyrieStaggerCooldown", 100f, "Modifies the cooldown of Stagger");
		vl_svr_valkyrieBulwark = base.Config.Bind("Class Modifiers", "vl_svr_valkyrieBulwark", 100f, "Modifies the damage reduction of Bulwark");
		vl_svr_valkyrieBonusChillWave = base.Config.Bind("Class Modifiers", "vl_svr_valkyrieBonusChillWave", 100f, "Modifies the damage from Chill Wave");
		vl_svr_valkyrieBonusIceLance = base.Config.Bind("Class Modifiers", "vl_svr_valkyrieBonusIceLance", 100f, "Modifies the damage from Ice Lance");
		vl_svr_valkyrieChargeDuration = base.Config.Bind("Class Modifiers", "vl_svr_valkyrieChargeDuration", 100f, "Modifies how quickly ice charges decrease");
		vl_svr_valkyrieItem = base.Config.Bind("Class Modifiers", "vl_svr_valkyrieItem", "item_flint", "Sacrifice this item at Eikthyr's altar to become a valkyrie");
		VL_GlobalConfigs.ConfigStrings = new Dictionary<string, float>();
		VL_GlobalConfigs.ConfigStrings.Clear();
		VL_GlobalConfigs.ItemStrings = new Dictionary<string, string>();
		VL_GlobalConfigs.ItemStrings.Clear();
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_energyCostMultiplier", vl_svr_energyCostMultiplier.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_cooldownMultiplier", vl_svr_cooldownMultiplier.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_abilityDamageMultiplier", vl_svr_abilityDamageMultiplier.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_skillGainMultiplier", vl_svr_skillGainMultiplier.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_unarmedDamageMultiplier", vl_svr_unarmedDamageMultiplier.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_berserkerDash", vl_svr_berserkerDash.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_berserkerBerserk", vl_svr_berserkerBerserk.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_berserkerExecute", vl_svr_berserkerExecute.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_berserkerBonusDamage", vl_svr_berserkerBonusDamage.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_berserkerBonus2h", vl_svr_berserkerBonus2h.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_berserkerItem", vl_svr_berserkerItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_druidVines", vl_svr_druidVines.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_druidRegen", vl_svr_druidRegen.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_druidDefenders", vl_svr_druidDefenders.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_druidBonusSeeds", vl_svr_druidBonusSeeds.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_druidItem", vl_svr_druidItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_duelistSeismicSlash", vl_svr_duelistSeismicSlash.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_duelistRiposte", vl_svr_duelistRiposte.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_duelistHipShot", vl_svr_duelistHipShot.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_duelistBonusParry", vl_svr_duelistBonusParry.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_duelistItem", vl_svr_duelistItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_enchanterWeaken", vl_svr_enchanterWeaken.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_enchanterCharm", vl_svr_enchanterCharm.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_enchanterBiome", vl_svr_enchanterBiome.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_enchanterBiomeShock", vl_svr_enchanterBiomeShock.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_enchanterBonusElementalBlock", vl_svr_enchanterBonusElementalBlock.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_enchanterBonusElementalTouch", vl_svr_enchanterBonusElementalTouch.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_enchanterItem", vl_svr_enchanterItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_mageFireball", vl_svr_mageFireball.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_mageFrostDagger", vl_svr_mageFrostDagger.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_mageFrostNova", vl_svr_mageFrostNova.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_mageInferno", vl_svr_mageInferno.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_mageMeteor", vl_svr_mageMeteor.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_mageItem", vl_svr_mageItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_meteavokerLight", vl_svr_metavokerLight.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_metavokerReplica", vl_svr_metavokerReplica.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_metavokerWarpDamage", vl_svr_metavokerWarpDamage.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_metavokerWarpDistance", vl_svr_metavokerWarpDistance.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_metavokerBonusSafeFallCost", vl_svr_metavokerBonusSafeFallCost.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_metavokerBonusForceWave", vl_svr_metavokerBonusForceWave.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_metavokerItem", vl_svr_metavokerItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_monkChiPunch", vl_svr_monkChiPunch.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_monkChiSlam", vl_svr_monkChiSlam.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_monkChiBlast", vl_svr_monkChiBlast.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_monkFlyingKick", vl_svr_monkFlyingKick.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_monkBonusBlock", vl_svr_monkBonusBlock.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_monkSurge", vl_svr_monkSurge.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_monkChiDuration", vl_svr_monkChiDuration.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_monkItem", vl_svr_monkItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_priestHeal", vl_svr_priestHeal.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_priestPurgeHeal", vl_svr_priestPurgeHeal.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_priestPurgeDamage", vl_svr_priestPurgeDamage.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_priestSanctify", vl_svr_priestSanctify.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_priestBonusDyingLightCooldown", vl_svr_priestBonusDyingLightCooldown.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_priestItem", vl_svr_priestItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rangerPowerShot", vl_svr_rangerPowerShot.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rangerShadowWolf", vl_svr_rangerShadowWolf.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rangerShadowStalk", vl_svr_rangerShadowStalk.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rangerBonusPoisonResistance", vl_svr_rangerBonusPoisonResistance.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rangerBonusRunCost", vl_svr_rangerBonusRunCost.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_rangerItem", vl_svr_rangerItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rogueBackstab", vl_svr_rogueBackstab.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rogueFadeCooldown", vl_svr_rogueFadeCooldown.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_roguePoisonBomb", vl_svr_roguePoisonBomb.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rogueBonusThrowingDagger", vl_svr_rogueBonusThrowingDagger.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_rogueTrickCharge", vl_svr_rogueTrickCharge.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_rogueItem", vl_svr_rogueItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_shamanSpiritShock", vl_svr_shamanSpiritShock.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_shamanEnrage", vl_svr_shamanEnrage.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_shamanShell", vl_svr_shamanShell.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_shamanBonusSpiritGuide", vl_svr_shamanBonusSpiritGuide.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_shamanBonusWaterGlideCost", vl_svr_shamanBonusWaterGlideCost.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_shamanItem", vl_svr_shamanItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_valkyrieLeap", vl_svr_valkyrieLeap.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_valkyrieStaggerCooldown", vl_svr_valkyrieStaggerCooldown.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_valkyrieBulwark", vl_svr_valkyrieBulwark.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_valkyrieBonusChillWave", vl_svr_valkyrieBonusChillWave.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_valkyrieBonusIceLance", vl_svr_valkyrieBonusIceLance.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_valkyrieChargeDuration", vl_svr_valkyrieChargeDuration.Value);
		VL_GlobalConfigs.ItemStrings.Add("vl_svr_valkyrieItem", vl_svr_valkyrieItem.Value);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_enforceConfigClass", vl_svr_enforceConfigClass.Value ? 1f : 0f);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_aoeRequiresLoS", vl_svr_aoeRequiresLoS.Value ? 1f : 0f);
		VL_GlobalConfigs.ConfigStrings.Add("vl_svr_allowAltarClassChange", vl_svr_allowAltarClassChange.Value ? 1f : 0f);
		VL_Utility.ModID = "valheim.torann.valheimlegends";
		VL_Utility.Folder = Path.GetDirectoryName(base.Info.Location);
		ZLog.Log("Valheim Legends attempting to find VLAssets in the directory with " + base.Info.Location);
		Texture2D texture2D = VL_Utility.LoadTextureFromAssets("abjuration_skill.png");
		Sprite icon = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D2 = VL_Utility.LoadTextureFromAssets("conjuration_skill.png");
		Sprite icon2 = Sprite.Create(texture2D2, new Rect(0f, 0f, texture2D2.width, texture2D2.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D3 = VL_Utility.LoadTextureFromAssets("alteration_skill.png");
		Sprite icon3 = Sprite.Create(texture2D3, new Rect(0f, 0f, texture2D3.width, texture2D3.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D4 = VL_Utility.LoadTextureFromAssets("discipline_skill.png");
		Sprite icon4 = Sprite.Create(texture2D4, new Rect(0f, 0f, texture2D4.width, texture2D4.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D5 = VL_Utility.LoadTextureFromAssets("evocation_skill.png");
		Sprite icon5 = Sprite.Create(texture2D5, new Rect(0f, 0f, texture2D5.width, texture2D5.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D6 = VL_Utility.LoadTextureFromAssets("illusion_skill.png");
		Sprite icon6 = Sprite.Create(texture2D6, new Rect(0f, 0f, texture2D6.width, texture2D6.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D7 = VL_Utility.LoadTextureFromAssets("movement_icon.png");
        Ability3_Sprite = Sprite.Create(texture2D7, new Rect(0f, 0f, texture2D7.width, texture2D7.height), new UnityEngine.Vector2(0.5f, 0.5f));
        //Ability3_Sprite = ZNetScene.instance.GetPrefab("ShieldSilver").GetComponent<ItemDrop>().m_itemData.GetIcon();
        Texture2D texture2D8 = VL_Utility.LoadTextureFromAssets("strength_icon.png");
        Ability2_Sprite = Sprite.Create(texture2D8, new Rect(0f, 0f, texture2D8.width, texture2D8.height), new UnityEngine.Vector2(0.5f, 0.5f));
        //Ability2_Sprite = ZNetScene.instance.GetPrefab("ShieldBanded").GetComponent<ItemDrop>().m_itemData.GetIcon();
        Texture2D texture2D9 = VL_Utility.LoadTextureFromAssets("protection_icon.png");
        Ability1_Sprite = Sprite.Create(texture2D9, new Rect(0f, 0f, texture2D9.width, texture2D9.height), new UnityEngine.Vector2(0.5f, 0.5f));
        //Ability1_Sprite = ZNetScene.instance.GetPrefab("ShieldWood").GetComponent<ItemDrop>().m_itemData.GetIcon();
        Texture2D texture2D10 = VL_Utility.LoadTextureFromAssets("riposte_icon.png");
		RiposteIcon = Sprite.Create(texture2D10, new Rect(0f, 0f, texture2D10.width, texture2D10.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D11 = VL_Utility.LoadTextureFromAssets("rogue_icon.png");
		RogueIcon = Sprite.Create(texture2D11, new Rect(0f, 0f, texture2D11.width, texture2D11.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D12 = VL_Utility.LoadTextureFromAssets("monk_icon.png");
		MonkIcon = Sprite.Create(texture2D12, new Rect(0f, 0f, texture2D12.width, texture2D12.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D13 = VL_Utility.LoadTextureFromAssets("weaken_icon.png");
		WeakenIcon = Sprite.Create(texture2D13, new Rect(0f, 0f, texture2D13.width, texture2D13.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D14 = VL_Utility.LoadTextureFromAssets("ranger_icon.png");
		RangerIcon = Sprite.Create(texture2D14, new Rect(0f, 0f, texture2D14.width, texture2D14.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D15 = VL_Utility.LoadTextureFromAssets("valkyrie_icon.png");
		ValkyrieIcon = Sprite.Create(texture2D15, new Rect(0f, 0f, texture2D15.width, texture2D15.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D16 = VL_Utility.LoadTextureFromAssets("biome_meadows_icon.png");
		BiomeMeadowsIcon = Sprite.Create(texture2D16, new Rect(0f, 0f, texture2D16.width, texture2D16.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D17 = VL_Utility.LoadTextureFromAssets("biome_blackforest_icon.png");
		BiomeBlackForestIcon = Sprite.Create(texture2D17, new Rect(0f, 0f, texture2D17.width, texture2D17.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D18 = VL_Utility.LoadTextureFromAssets("biome_swamp_icon.png");
		BiomeSwampIcon = Sprite.Create(texture2D18, new Rect(0f, 0f, texture2D18.width, texture2D18.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D19 = VL_Utility.LoadTextureFromAssets("biome_mountain_icon.png");
		BiomeMountainIcon = Sprite.Create(texture2D19, new Rect(0f, 0f, texture2D19.width, texture2D19.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D20 = VL_Utility.LoadTextureFromAssets("biome_plains_icon.png");
		BiomePlainsIcon = Sprite.Create(texture2D20, new Rect(0f, 0f, texture2D20.width, texture2D20.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D21 = VL_Utility.LoadTextureFromAssets("biome_ocean_icon.png");
		BiomeOceanIcon = Sprite.Create(texture2D21, new Rect(0f, 0f, texture2D21.width, texture2D21.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D22 = VL_Utility.LoadTextureFromAssets("biome_mist_icon.png");
		BiomeMistIcon = Sprite.Create(texture2D22, new Rect(0f, 0f, texture2D22.width, texture2D22.height), new UnityEngine.Vector2(0.5f, 0.5f));
		Texture2D texture2D23 = VL_Utility.LoadTextureFromAssets("biome_ash_icon.png");
		BiomeAshIcon = Sprite.Create(texture2D23, new Rect(0f, 0f, texture2D23.width, texture2D23.height), new UnityEngine.Vector2(0.5f, 0.5f));
		LoadModAssets_Awake();
		VL_Utility.SetTimer();
		AbjurationSkillDef = new Skills.SkillDef
		{
			m_skill = (Skills.SkillType)AbjurationSkillID,
			m_icon = icon,
			m_description = "Skill in creating protective spells and wards",
			m_increseStep = 1f
		};
		AlterationSkillDef = new Skills.SkillDef
		{
			m_skill = (Skills.SkillType)AlterationSkillID,
			m_icon = icon3,
			m_description = "Skill in temporarily enhancing or modifying attributes",
			m_increseStep = 1f
		};
		ConjurationSkillDef = new Skills.SkillDef
		{
			m_skill = (Skills.SkillType)ConjurationSkillID,
			m_icon = icon2,
			m_description = "Skill in temporarily manifesting reality by molding objects and energy",
			m_increseStep = 1f
		};
		DisciplineSkillDef = new Skills.SkillDef
		{
			m_skill = (Skills.SkillType)DisciplineSkillID,
			m_icon = icon4,
			m_description = "Ability to perform or resist phenomenal feats through strength of body and mind",
			m_increseStep = 1f
		};
		EvocationSkillDef = new Skills.SkillDef
		{
			m_skill = (Skills.SkillType)EvocationSkillID,
			m_icon = icon5,
			m_description = "Skill in creating and manipulating energy",
			m_increseStep = 1f
		};
		IllusionSkillDef = new Skills.SkillDef
		{
			m_skill = (Skills.SkillType)IllusionSkillID,
			m_icon = icon6,
			m_description = "Skill in creating convincing illusions",
			m_increseStep = 1f
		};
		legendsSkills.Add(DisciplineSkillDef);
		legendsSkills.Add(AbjurationSkillDef);
		legendsSkills.Add(AlterationSkillDef);
		legendsSkills.Add(ConjurationSkillDef);
		legendsSkills.Add(EvocationSkillDef);
		legendsSkills.Add(IllusionSkillDef);
		_Harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), "valheim.torann.valheimlegends");
	}

	private void OnDestroy()
	{
		if (_Harmony != null)
		{
			_Harmony.UnpatchSelf();
		}
	}

	public static void SetVLPlayer(Player p)
	{
		vl_player = new VL_Player();
		foreach (VL_Player vl_player in vl_playerList)
		{
			if (!(p.GetPlayerName() == vl_player.vl_name))
			{
				continue;
			}
			ValheimLegends.vl_player.vl_name = vl_player.vl_name;
			ValheimLegends.vl_player.vl_class = vl_player.vl_class;
			if ((ValheimLegends.vl_player.vl_class == PlayerClass.None && chosenClass.Value.ToLower() != "none") || (chosenClass.Value.ToLower() != "none" && VL_GlobalConfigs.ConfigStrings["vl_svr_enforceConfigClass"] != 0f))
			{
				switch (chosenClass.Value.ToLower())
				{
				case "berserker":
					ValheimLegends.vl_player.vl_class = PlayerClass.Berserker;
					break;
				case "druid":
					ValheimLegends.vl_player.vl_class = PlayerClass.Druid;
					break;
				case "mage":
					ValheimLegends.vl_player.vl_class = PlayerClass.Mage;
					break;
				case "ranger":
					ValheimLegends.vl_player.vl_class = PlayerClass.Ranger;
					break;
				case "shaman":
					ValheimLegends.vl_player.vl_class = PlayerClass.Shaman;
					break;
				case "valkyrie":
					ValheimLegends.vl_player.vl_class = PlayerClass.Valkyrie;
					break;
				case "metavoker":
					ValheimLegends.vl_player.vl_class = PlayerClass.Metavoker;
					break;
				case "priest":
					ValheimLegends.vl_player.vl_class = PlayerClass.Priest;
					break;
				case "monk":
					ValheimLegends.vl_player.vl_class = PlayerClass.Monk;
					break;
				case "duelist":
					ValheimLegends.vl_player.vl_class = PlayerClass.Duelist;
					break;
				case "enchanter":
					ValheimLegends.vl_player.vl_class = PlayerClass.Enchanter;
					break;
				case "rogue":
					ValheimLegends.vl_player.vl_class = PlayerClass.Rogue;
					break;
				}
			}
			NameCooldowns();
		}
	}

	public static void UpdateVLPlayer(Player p)
	{
		foreach (VL_Player vl_player in vl_playerList)
		{
			if (p.GetPlayerName() == vl_player.vl_name)
			{
				vl_player.vl_class = ValheimLegends.vl_player.vl_class;
				SaveVLPlayer_Patch.Postfix(Game.instance.GetPlayerProfile(), Game.instance.GetPlayerProfile().GetFilename(), Game.instance.GetPlayerProfile().GetName());
			}
		}
	}

	public static void NameCooldowns()
	{
        if (vl_player.vl_class == PlayerClass.Mage)
        {
            ZLog.Log("Valheim Legend: Mage");

            // Verifica qual afinidade está focada
            var focus = Class_Mage.GetCurrentFocus(Player.m_localPlayer);

            if (focus == Class_Mage.MageAffinity.Frost)
            {
                Ability1_Name = "Ice Shard"; // Ability 1
                Ability2_Name = "Frost Nova";   // Ability 2
                Ability3_Name = "Blizzard";     // Ability 3
            }
            else if (focus == Class_Mage.MageAffinity.Arcane)
            {
                Ability1_Name = "Elemental Mastery"; // Ability 1
                Ability2_Name = "Arcane Intellect";// Ability 2
                Ability3_Name = "Mana Shield";  // Ability 3
            }
            else // Default: Fire
            {
                Ability1_Name = "Fireball";     // Ability 1
                Ability2_Name = "Inferno";   // Ability 2
                Ability3_Name = "Meteor";       // Ability 3
            }

            Player.m_localPlayer.ShowTutorial("VL_Mage");
        }
        else if (vl_player.vl_class == PlayerClass.Druid)
		{
			ZLog.Log("Valheim Legend: Druid");
            var seMan = Player.m_localPlayer.GetSEMan();
            if (seMan == null) return;

            if (seMan.HaveStatusEffect("SE_VL_DruidFenringForm".GetStableHashCode()))
            {
                Ability1_Name = "Shadow";
                Ability2_Name = "Stagger";
                Ability3_Name = "Dash";
            } else
			{
                Ability1_Name = "Regen";
                Ability2_Name = "Living Defender";
                Ability3_Name = "Vines";
            }
            Player.m_localPlayer.ShowTutorial("VL_Druid");
		}
		else if (vl_player.vl_class == PlayerClass.Shaman)
		{
			ZLog.Log("Valheim Legend: Shaman");
			Ability1_Name = "Enrage";
			Ability2_Name = "Shell";
			Ability3_Name = "Spirit Shock";
			Player.m_localPlayer.ShowTutorial("VL_Shaman");
		}
		else if (vl_player.vl_class == PlayerClass.Ranger)
		{
			ZLog.Log("Valheim Legend: Ranger");
			Ability1_Name = "Shadow";
			Ability2_Name = "Wolf";
			Ability3_Name = "Power Shot";
			Player.m_localPlayer.ShowTutorial("VL_Ranger");
		}
		else if (vl_player.vl_class == PlayerClass.Berserker)
		{
			ZLog.Log("Valheim Legend: Berserker");
			Ability1_Name = "Execute";
			Ability2_Name = "Berserk";
			Ability3_Name = "Dash";
			Player.m_localPlayer.ShowTutorial("VL_Berserker");
		}
		else if (vl_player.vl_class == PlayerClass.Valkyrie)
		{
			ZLog.Log("Valheim Legend: Valkyrie");
			Ability1_Name = "Bulwark";
			Ability2_Name = "Stagger";
			Ability3_Name = "Leap";
			Player.m_localPlayer.ShowTutorial("VL_Valkyrie");
		}
		else if (vl_player.vl_class == PlayerClass.Metavoker)
		{
			ZLog.Log("Valheim Legend: Metavoker");
			Ability1_Name = "Light";
			Ability2_Name = "Replica";
			Ability3_Name = "Warp";
			Player.m_localPlayer.ShowTutorial("VL_Metavoker");
		}
		else if (vl_player.vl_class == PlayerClass.Duelist)
		{
			ZLog.Log("Valheim Legend: Duelist");
			Ability1_Name = "Coin Shot";
			Ability2_Name = "Riposte";
			Ability3_Name = "Seismic Slash";
			Player.m_localPlayer.ShowTutorial("VL_Duelist");
		}
		else if (vl_player.vl_class == PlayerClass.Priest)
		{
			ZLog.Log("Valheim Legend: Priest");
			Ability1_Name = "Sanctify";
			Ability2_Name = "Purge";
			Ability3_Name = "Heal";
			Player.m_localPlayer.ShowTutorial("VL_Priest");
		}
		else if (vl_player.vl_class == PlayerClass.Rogue)
		{
			ZLog.Log("Valheim Legend: Rogue");
			Ability1_Name = "Poison Bomb";
			Ability2_Name = "Fade";
			Ability3_Name = "Backstab";
			Player.m_localPlayer.ShowTutorial("VL_Rogue");
		}
		else if (vl_player.vl_class == PlayerClass.Monk)
		{
			ZLog.Log("Valheim Legend: Monk");
			Ability1_Name = "Chi Strike";
			Ability2_Name = "Flying Kick";
			Ability3_Name = "Chi Blast";
			Player.m_localPlayer.ShowTutorial("VL_Monk");
		}
		else if (vl_player.vl_class == PlayerClass.Enchanter)
		{
			ZLog.Log("Valheim Legend: Enchanter");
			Ability1_Name = "Weaken";
			Ability2_Name = "Charm";
			Ability3_Name = "Zone Charge";
			Player.m_localPlayer.ShowTutorial("VL_Enchanter");
		}
		else
		{
			ZLog.Log("Valheim Legend: --None--");
		}
	}

	private static void LoadModAssets_Awake()
	{
		AssetBundle assetBundleFromResources = GetAssetBundleFromResources("vl_assetbundle");
		VL_Deathsquit = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_Deathsquit.prefab");
		VL_ShadowWolf = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_ShadowWolf.prefab");
		VL_DemonWolf = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_DemonWolf.prefab");
		VL_Light = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_Light.prefab");
		VL_SanctifyHammer = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_SanctifyHammer.prefab");
		VL_PoisonBomb = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_PoisonBomb.prefab");
		VL_PoisonBombExplosion = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_PoisonBombExplosion.prefab");
		VL_ThrowingKnife = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_ThrowingKnife.prefab");
		VL_PsiBolt = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_PsiBolt.prefab");
		VL_Charm = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_Charm.prefab");
		VL_FrostDagger = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_FrostDagger.prefab");
		VL_ValkyrieSpear = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_ValkyrieSpear.prefab");
		VL_ShadowWolfAttack = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/VL_ShadowWolfAttack.prefab");
		fx_VL_Lightburst = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Lightburst.prefab");
		fx_VL_ParticleLightburst = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ParticleLightburst.prefab");
		fx_VL_ParticleLightSuction = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ParticleLightSuction.prefab");
		fx_VL_ReverseLightburst = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ReverseLightburst.prefab");
		fx_VL_BlinkStrike = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_BlinkStrike.prefab");
		fx_VL_QuickShot = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_QuickShot.prefab");
		fx_VL_HealPulse = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_HealPulse.prefab");
		fx_VL_Purge = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Purge.prefab");
		fx_VL_Smokeburst = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Smokeburst.prefab");
		fx_VL_Shadowburst = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Shadowburst.prefab");
		fx_VL_Shockwave = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Shockwave.prefab");
		fx_VL_FlyingKick = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_FlyingKick.prefab");
		fx_VL_MeteorSlam = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_MeteorSlam.prefab");
		fx_VL_Weaken = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Weaken.prefab");
		fx_VL_WeakenStatus = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_WeakenStatus.prefab");
		fx_VL_Shock = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Shock.prefab");
		fx_VL_ParticleTailField = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ParticleTailField.prefab");
		fx_VL_ParticleFieldBurst = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ParticleFieldBurst.prefab");
		fx_VL_HeavyCrit = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_HeavyCrit.prefab");
		fx_VL_ChiPulse = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ChiPulse.prefab");
		fx_VL_Replica = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Replica.prefab");
		fx_VL_ReplicaCreate = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ReplicaCreate.prefab");
		fx_VL_ForwardLightningShock = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ForwardLightningShock.prefab");
		fx_VL_Flames = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_Flames.prefab");
		fx_VL_FlameBurst = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_FlameBurst.prefab");
		fx_VL_AbsorbSpirit = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_AbsorbSpirit.prefab");
		fx_VL_ForceWall = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ForceWall.prefab");
		fx_VL_ShieldRelease = assetBundleFromResources.LoadAsset<GameObject>("Assets/CustomAssets/fx_VL_ShieldRelease.prefab");
		anim_player_float = assetBundleFromResources.LoadAsset<AnimationClip>("Assets/CustomAssets/anim_float.anim");
	}

	public static AssetBundle GetAssetBundleFromResources(string fileName)
	{
		Assembly executingAssembly = Assembly.GetExecutingAssembly();
		string text = executingAssembly.GetManifestResourceNames().Single((string str) => str.EndsWith(fileName));
		Stream manifestResourceStream = executingAssembly.GetManifestResourceStream(text);
		using (manifestResourceStream)
		{
			return AssetBundle.LoadFromStream(manifestResourceStream);
		}
	}

	private static void Add_VL_Assets()
	{
		if (ObjectDB.instance == null || ObjectDB.instance.m_items.Count == 0)
		{
			return;
		}
		ItemDrop component = VL_Deathsquit.GetComponent<ItemDrop>();
		if (component != null)
		{
			if (ObjectDB.instance.GetItemPrefab(VL_Deathsquit.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_Deathsquit);
				Dictionary<int, GameObject> dictionary = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary[VL_Deathsquit.name.GetStableHashCode()] = VL_Deathsquit;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_ShadowWolf.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_ShadowWolf);
				Dictionary<int, GameObject> dictionary2 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary2[VL_ShadowWolf.name.GetStableHashCode()] = VL_ShadowWolf;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_DemonWolf.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_DemonWolf);
				Dictionary<int, GameObject> dictionary3 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary3[VL_DemonWolf.name.GetStableHashCode()] = VL_DemonWolf;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_Light.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_Light);
				Dictionary<int, GameObject> dictionary4 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary4[VL_Light.name.GetStableHashCode()] = VL_Light;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_PoisonBomb.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_PoisonBomb);
				Dictionary<int, GameObject> dictionary5 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary5[VL_PoisonBomb.name.GetStableHashCode()] = VL_PoisonBomb;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_PoisonBombExplosion.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_PoisonBombExplosion);
				Dictionary<int, GameObject> dictionary6 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary6[VL_PoisonBombExplosion.name.GetStableHashCode()] = VL_PoisonBombExplosion;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_ThrowingKnife.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_ThrowingKnife);
				Dictionary<int, GameObject> dictionary7 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary7[VL_ThrowingKnife.name.GetStableHashCode()] = VL_ThrowingKnife;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_PsiBolt.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_PsiBolt);
				Dictionary<int, GameObject> dictionary8 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary8[VL_PsiBolt.name.GetStableHashCode()] = VL_PsiBolt;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_Charm.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_Charm);
				Dictionary<int, GameObject> dictionary9 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary9[VL_Charm.name.GetStableHashCode()] = VL_Charm;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_FrostDagger.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_FrostDagger);
				Dictionary<int, GameObject> dictionary10 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary10[VL_FrostDagger.name.GetStableHashCode()] = VL_FrostDagger;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_ValkyrieSpear.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_ValkyrieSpear);
				Dictionary<int, GameObject> dictionary11 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary11[VL_ValkyrieSpear.name.GetStableHashCode()] = VL_ValkyrieSpear;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_ShadowWolfAttack.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_ShadowWolfAttack);
				Dictionary<int, GameObject> dictionary12 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary12[VL_ShadowWolfAttack.name.GetStableHashCode()] = VL_ShadowWolfAttack;
			}
			if (ObjectDB.instance.GetItemPrefab(VL_SanctifyHammer.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(VL_SanctifyHammer);
				Dictionary<int, GameObject> dictionary13 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary13[VL_SanctifyHammer.name.GetStableHashCode()] = VL_SanctifyHammer;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Lightburst.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Lightburst);
				Dictionary<int, GameObject> dictionary14 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary14[fx_VL_Lightburst.name.GetStableHashCode()] = fx_VL_Lightburst;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ParticleLightburst.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ParticleLightburst);
				Dictionary<int, GameObject> dictionary15 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary15[fx_VL_ParticleLightburst.name.GetStableHashCode()] = fx_VL_ParticleLightburst;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ParticleLightSuction.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ParticleLightSuction);
				Dictionary<int, GameObject> dictionary16 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary16[fx_VL_ParticleLightSuction.name.GetStableHashCode()] = fx_VL_ParticleLightSuction;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ReverseLightburst.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ReverseLightburst);
				Dictionary<int, GameObject> dictionary17 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary17[fx_VL_ReverseLightburst.name.GetStableHashCode()] = fx_VL_ReverseLightburst;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_BlinkStrike.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_BlinkStrike);
				Dictionary<int, GameObject> dictionary18 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary18[fx_VL_BlinkStrike.name.GetStableHashCode()] = fx_VL_BlinkStrike;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_QuickShot.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_QuickShot);
				Dictionary<int, GameObject> dictionary19 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary19[fx_VL_QuickShot.name.GetStableHashCode()] = fx_VL_QuickShot;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_HealPulse.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_HealPulse);
				Dictionary<int, GameObject> dictionary20 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary20[fx_VL_HealPulse.name.GetStableHashCode()] = fx_VL_HealPulse;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Purge.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Purge);
				Dictionary<int, GameObject> dictionary21 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary21[fx_VL_Purge.name.GetStableHashCode()] = fx_VL_Purge;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Smokeburst.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Smokeburst);
				Dictionary<int, GameObject> dictionary22 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary22[fx_VL_Smokeburst.name.GetStableHashCode()] = fx_VL_Smokeburst;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Shadowburst.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Shadowburst);
				Dictionary<int, GameObject> dictionary23 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary23[fx_VL_Shadowburst.name.GetStableHashCode()] = fx_VL_Shadowburst;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Shockwave.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Shockwave);
				Dictionary<int, GameObject> dictionary24 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary24[fx_VL_Shockwave.name.GetStableHashCode()] = fx_VL_Shockwave;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_FlyingKick.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_FlyingKick);
				Dictionary<int, GameObject> dictionary25 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary25[fx_VL_FlyingKick.name.GetStableHashCode()] = fx_VL_FlyingKick;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_MeteorSlam.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_MeteorSlam);
				Dictionary<int, GameObject> dictionary26 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary26[fx_VL_MeteorSlam.name.GetStableHashCode()] = fx_VL_MeteorSlam;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Weaken.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Weaken);
				Dictionary<int, GameObject> dictionary27 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary27[fx_VL_Weaken.name.GetStableHashCode()] = fx_VL_Weaken;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_WeakenStatus.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_WeakenStatus);
				Dictionary<int, GameObject> dictionary28 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary28[fx_VL_WeakenStatus.name.GetStableHashCode()] = fx_VL_WeakenStatus;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Shock.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Shock);
				Dictionary<int, GameObject> dictionary29 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary29[fx_VL_Shock.name.GetStableHashCode()] = fx_VL_Shock;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ParticleTailField.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ParticleTailField);
				Dictionary<int, GameObject> dictionary30 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary30[fx_VL_ParticleTailField.name.GetStableHashCode()] = fx_VL_ParticleTailField;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ParticleFieldBurst.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ParticleFieldBurst);
				Dictionary<int, GameObject> dictionary31 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary31[fx_VL_ParticleFieldBurst.name.GetStableHashCode()] = fx_VL_ParticleFieldBurst;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_HeavyCrit.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_HeavyCrit);
				Dictionary<int, GameObject> dictionary32 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary32[fx_VL_HeavyCrit.name.GetStableHashCode()] = fx_VL_HeavyCrit;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ChiPulse.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ChiPulse);
				Dictionary<int, GameObject> dictionary33 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary33[fx_VL_ChiPulse.name.GetStableHashCode()] = fx_VL_ChiPulse;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Replica.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Replica);
				Dictionary<int, GameObject> dictionary34 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary34[fx_VL_Replica.name.GetStableHashCode()] = fx_VL_Replica;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ReplicaCreate.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ReplicaCreate);
				Dictionary<int, GameObject> dictionary35 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary35[fx_VL_ReplicaCreate.name.GetStableHashCode()] = fx_VL_ReplicaCreate;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ForwardLightningShock.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ForwardLightningShock);
				Dictionary<int, GameObject> dictionary36 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary36[fx_VL_ForwardLightningShock.name.GetStableHashCode()] = fx_VL_ForwardLightningShock;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_Flames.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_Flames);
				Dictionary<int, GameObject> dictionary37 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary37[fx_VL_Flames.name.GetStableHashCode()] = fx_VL_Flames;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_FlameBurst.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_FlameBurst);
				Dictionary<int, GameObject> dictionary38 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary38[fx_VL_FlameBurst.name.GetStableHashCode()] = fx_VL_FlameBurst;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_AbsorbSpirit.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_AbsorbSpirit);
				Dictionary<int, GameObject> dictionary39 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary39[fx_VL_AbsorbSpirit.name.GetStableHashCode()] = fx_VL_AbsorbSpirit;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ForceWall.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ForceWall);
				Dictionary<int, GameObject> dictionary40 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary40[fx_VL_ForceWall.name.GetStableHashCode()] = fx_VL_ForceWall;
			}
			if (ObjectDB.instance.GetItemPrefab(fx_VL_ShieldRelease.name.GetStableHashCode()) == null)
			{
				ObjectDB.instance.m_items.Add(fx_VL_ShieldRelease);
				Dictionary<int, GameObject> dictionary41 = (Dictionary<int, GameObject>)typeof(ObjectDB).GetField("m_itemByHash", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ObjectDB.instance);
				dictionary41[fx_VL_ShieldRelease.name.GetStableHashCode()] = fx_VL_ShieldRelease;
			}
		}
	}
}
