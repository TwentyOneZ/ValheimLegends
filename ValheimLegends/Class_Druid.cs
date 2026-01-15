using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static UnityEngine.UIElements.UIR.Allocator2D;

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

    public static bool canDoubleJump = true;

    public static readonly int SE_FENRING_HASH = "SE_VL_DruidFenringForm".GetStableHashCode();
    public static readonly int SE_CULTIST_HASH = "SE_VL_DruidCultistForm".GetStableHashCode();

    // Reflection getters (funcionam mesmo se forem protected/private)
    private static readonly MethodInfo MI_GetRightItem =
        AccessTools.Method(typeof(Humanoid), "GetRightItem");

    private static readonly MethodInfo MI_GetLeftItem =
        AccessTools.Method(typeof(Humanoid), "GetLeftItem");

    // Fallback fields (caso o método mude/renomeie)
    private static readonly FieldInfo FI_RightItem =
        AccessTools.Field(typeof(Humanoid), "m_rightItem");

    private static readonly FieldInfo FI_LeftItem =
        AccessTools.Field(typeof(Humanoid), "m_leftItem");

    private static readonly MethodInfo MI_UpdateEquipmentVisuals =
        AccessTools.Method(typeof(Humanoid), "UpdateEquipmentVisuals");


    public static void Process_FenringForm(Player player, float altitude, ref Rigidbody playerBody)
    {
        // Ability1 -> ShadowStalk do Ranger
        if (ZInput.GetButtonDown("Jump") && !player.IsDead() && !player.InAttack() && !player.IsEncumbered() && !player.InDodge() && !player.IsKnockedBack())
        {
            if (!player.IsOnGround() && canDoubleJump)
            {
                UnityEngine.Vector3 velocity = player.GetVelocity();
                velocity.y = 0f;
                playerBody.linearVelocity = velocity * 2f + new UnityEngine.Vector3(0f, 8f, 0f);
                canDoubleJump = false;
                altitude = 0f;
                ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("jump");
            }
            else if (player.IsOnGround())
            {
                canDoubleJump = true;
            }
        }
        if (VL_Utility.Ability1_Input_Down)
        {
            VL_AbilityBorrow.Ranger_ShadowStalk(player);
            return;
        }

        // Ability2 -> Stagger da Valkyrie
        if (VL_Utility.Ability2_Input_Down)
        {
            if (player.IsBlocking())
			{
				TryActivate_HumanForm(player, false);
				return;
            }
			VL_AbilityBorrow.Valkyrie_Stagger(player);
            return;
        }

        // Ability3 -> Dash do Berserker
        if (VL_Utility.Ability3_Input_Down)
        {
            // aqui você vai chamar um helper de “dash”
            VL_AbilityBorrow.Berserker_Dash(player);
        }
    }

    //public static void Process_CultistForm(Player player, float altitude)
    //{
    //    // Ability1 -> Fireball do Mage
    //    if (VL_Utility.Ability1_Input_Down)
    //    {
    //        VL_AbilityBorrow.Mage_Fireball(player);
    //        return;
    //    }

    //    // Ability2 -> Inferno do Mage
    //    if (VL_Utility.Ability2_Input_Down)
    //    {
    //        VL_AbilityBorrow.Mage_Inferno(player);
    //        return;
    //    }

    //    // Ability3 -> Meditate do Mage
    //    if (VL_Utility.Ability3_Input_Down)
    //    {
    //        VL_AbilityBorrow.Mage_Meditate(player);
    //        return;
    //    }
    //}


    public static void Process_Input(Player player, float altitude, ref Rigidbody playerBody)
	{
        // 1) Se estiver transformado, roteia e sai
        if (player.GetSEMan().HaveStatusEffect(SE_FENRING_HASH))
        {
            Process_FenringForm(player, altitude, ref playerBody);
            return;
        }
        //if (player.GetSEMan().HaveStatusEffect(SE_CULTIST_HASH))
        //{
        //    Process_CultistForm(player, altitude);
        //    return;
        //}


        System.Random random = new System.Random();
		UnityEngine.Vector3 vector = default(Vector3);
		if (VL_Utility.Ability3_Input_Down)
		{

            //if (player.IsBlocking())
            //{
            //    TryActivate_CultistForm(player);
            //    return;
            //}
            if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
			{
				ValheimLegends.shouldUseGuardianPower = false;
				if (player.GetStamina() >= VL_Utility.GetRootCost && !ValheimLegends.isChanneling)
				{
					ValheimLegends.isChanneling = true;
                    ValheimLegends.channelingBlocksMovement = false;
                    StatusEffect statusEffect = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
					statusEffect.m_ttl = VL_Utility.GetRootCooldownTime;
					player.GetSEMan().AddStatusEffect(statusEffect);
					player.UseStamina(VL_Utility.GetRootCost);
                    //((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("gpower");
                    //((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetSpeed(5f);
                    player.StartEmote("point");
                    float level = player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.ConjurationSkillDef)
						.m_level * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
					rootCount = 0;
					rootCountTrigger = 16 - Mathf.RoundToInt((0.05f * level) - (7.5f * (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f)));
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
					P_Root.name = "VL_DruidRoot";
					P_Root.m_respawnItemOnHit = false;
					P_Root.m_spawnOnHit = null;
					P_Root.m_ttl = 35f;
					P_Root.m_gravity = 0f;
					P_Root.m_rayRadius = 0.1f;
					Traverse.Create(P_Root).Field("m_skill").SetValue(ValheimLegends.ConjurationSkill);
                    var d = player.GetLookDir();
                    if (d.sqrMagnitude > 0.000001f) 
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
		else if (VL_Utility.Ability3_Input_Pressed && player.GetStamina() > VL_Utility.GetRootCostPerUpdate && ValheimLegends.isChanneling)
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
				hitData.m_damage.m_pierce = UnityEngine.Random.Range(6f + 0.6f * level2, 10f + 1.2f * level2) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_druidVines;
				hitData.m_pushForce = 2f;
				rootTotal++;
				UnityEngine.Vector3 vector3 = UnityEngine.Vector3.MoveTowards(GO_Root.transform.position, target, 1f);
				if (P_Root != null && P_Root.name == "VL_DruidRoot")
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
			P_Root.name = "VL_DruidRoot";
			P_Root.m_respawnItemOnHit = false;
			P_Root.m_spawnOnHit = null;
			P_Root.m_ttl = rootCountTrigger + 1;
			P_Root.m_gravity = 0f;
			P_Root.m_rayRadius = 0.1f;
			Traverse.Create(P_Root).Field("m_skill").SetValue(ValheimLegends.ConjurationSkill);
            var d = player.GetLookDir();
            if (d.sqrMagnitude > 0.000001f)
                P_Root.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetLookDir());
			GO_Root.transform.localScale = UnityEngine.Vector3.one * 1.5f;
		}
		else if (((VL_Utility.Ability3_Input_Up || player.GetStamina() <= VL_Utility.GetRootCostPerUpdate) && ValheimLegends.isChanneling))
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
            ValheimLegends.channelingBlocksMovement = true;
        }
        else if (VL_Utility.Ability2_Input_Down)
		{
            if (player.IsBlocking())
            {
                TryActivate_FenringForm(player);
                return;
            }
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
                    var inv = player.GetInventory();
                    if (inv == null) return;

                    int requiredItems = 1;
                    string materialName = "AncientSeed";

                    // 1) checar material
                    ItemDrop.ItemData foundItem = VL_Utility.FindItemByPrefabName(inv, materialName, requiredItems);
                    if (foundItem == null)
                    {
                        player.Message(MessageHud.MessageType.Center,
                            "You need an Ancient Seed to create a Spirit Binding Vial.");
                        return;
                    }

                    // 2) checar stamina
                    if (player.GetStamina() < VL_Utility.GetRegenerationCost)
                    {
                        player.Message(MessageHud.MessageType.TopLeft,
                            "Not enough stamina to Create Spirit Binding Vial: (" +
                            player.GetStamina().ToString("#.#") + "/" +
                            VL_Utility.GetRegenerationCost.ToString("#.#") + ")");
                        return;
                    }

                    // 3) cooldown/skill etc (seu código)
                    player.RaiseSkill(ValheimLegends.AlterationSkill, VL_Utility.GetRegenerationSkillGain);
                    StatusEffect statusEffect = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));

                    float level = player.GetSkills().GetSkillList()
                        .FirstOrDefault(x => x.m_info == ValheimLegends.AlterationSkillDef).m_level
                        * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) +
                                            (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));

                    statusEffect.m_ttl = VL_Utility.GetHealCooldownTime * 20f / (1f + level / 150f);
                    player.GetSEMan().AddStatusEffect(statusEffect);

                    player.UseStamina(VL_Utility.GetRegenerationCost);

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
            ValheimLegends.channelingBlocksMovement = true;
        }
	}

    public static void TryActivate_HumanForm(Player player, bool forced)
    {
        var seMan = player.GetSEMan();
        if (seMan == null) return;

        bool hasFenring = seMan.HaveStatusEffect(SE_FENRING_HASH);
        bool hasCultist = seMan.HaveStatusEffect(SE_CULTIST_HASH);

        if (!hasFenring && !hasCultist)
        {
            player.Message(MessageHud.MessageType.TopLeft, "Already human.");
            return;
        }

        float cost = VL_Utility.GetDefenderCost;
        if (!forced)
        {
            if (player.GetStamina() < cost)
            {
                player.Message(MessageHud.MessageType.TopLeft, $"Not enough stamina: ({player.GetStamina():#.#}/{cost:#.#})");
                return;
            }

            if (seMan.HaveStatusEffect("SE_VL_Shapeshift_CD".GetStableHashCode()))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Can't shapeshift again yet.");
                return;
            }
        }
        else
        {
            player.Message(MessageHud.MessageType.TopLeft, "Ran out of Eitr to sustain the shapeshifting.");
        }

        StatusEffect statusEffect2 = (SE_Shapeshift_CD)ScriptableObject.CreateInstance(typeof(SE_Shapeshift_CD));
        float level = player.GetSkills().GetSkillList()
            .FirstOrDefault(x => x.m_info == ValheimLegends.AlterationSkillDef).m_level
            * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) +
                                (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
        statusEffect2.m_ttl = 180f - (60f * (level / 150f)) - (60f * (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f));
        if (!forced)
        {
            statusEffect2.m_ttl = 30f - (14.25f * (level / 150f)) - (14.25f * (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f));
            player.UseStamina(cost);
        }
        seMan.AddStatusEffect(statusEffect2);

        // ✅ quiet:false (ou use overload sem bool)
        if (hasFenring) seMan.RemoveStatusEffect(SE_FENRING_HASH, false);
        if (hasCultist) seMan.RemoveStatusEffect(SE_CULTIST_HASH, false);

        // ✅ refresh forçado (opcional)
        ForceWeaponRefresh(player);

        ValheimLegends.NameCooldowns();
        if (ValheimLegends.abilitiesStatus != null)
        {
            foreach (RectTransform item2 in ValheimLegends.abilitiesStatus)
            {
                if (item2.gameObject != null)
                {
                    UnityEngine.Object.Destroy(item2.gameObject);
                }
            }
            ValheimLegends.abilitiesStatus.Clear();
        }
        // FX/anim (opcional)
        ValheimLegends.shouldUseGuardianPower = false;

        var zanim = (ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(player);
        zanim?.SetTrigger("gpower");
        if (ZNetScene.instance != null)
        {
            var fx = ZNetScene.instance.GetPrefab("vfx_odin_despawn");
            if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, Quaternion.identity);
            fx = ZNetScene.instance.GetPrefab("sfx_wraith_death");
            if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, Quaternion.identity);
        }
    }


    private static void TryActivate_FenringForm(Player player)
    {
        var seMan = player.GetSEMan();
        if (seMan == null) return;

        // Se já está em Fenring, desativa (volta humano)
        if (seMan.HaveStatusEffect(SE_FENRING_HASH))
        {
            TryActivate_HumanForm(player, false);
            return;
        }

        // Se está em outra forma, também volta humano primeiro (ou bloqueia)
        if (seMan.HaveStatusEffect(SE_CULTIST_HASH))
        {
            player.Message(MessageHud.MessageType.TopLeft, "Already transformed (Cultist).");
            return;
            // ou: TryActivate_HumanForm(player); e depois continua
        }

        float cost = VL_Utility.GetDefenderCost;
        if (player.GetStamina() < cost)
        {
            player.Message(MessageHud.MessageType.TopLeft, $"Not enough stamina: ({player.GetStamina():#.#}/{cost:#.#})");
            return;
        }

        if (seMan.HaveStatusEffect("SE_VL_Shapeshift_CD".GetStableHashCode()))
        {
            player.Message(MessageHud.MessageType.TopLeft, "Can't shapeshift again yet.");
            return;
        }

        StatusEffect statusEffect2 = (SE_Shapeshift_CD)ScriptableObject.CreateInstance(typeof(SE_Shapeshift_CD));
        float level = player.GetSkills().GetSkillList()
            .FirstOrDefault(x => x.m_info == ValheimLegends.AlterationSkillDef).m_level
            * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) +
                                (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
        statusEffect2.m_ttl = 30f - (14.25f * (level / 150f)) - (14.25f * (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f));
        seMan.AddStatusEffect(statusEffect2);

        player.UseStamina(cost);
        var se = (SE_DruidFenringForm)ScriptableObject.CreateInstance(typeof(SE_DruidFenringForm));
        seMan.AddStatusEffect(se);

        ValheimLegends.NameCooldowns();
        if (ValheimLegends.abilitiesStatus != null)
        {
            foreach (RectTransform item2 in ValheimLegends.abilitiesStatus)
            {
                if (item2.gameObject != null)
                {
                    UnityEngine.Object.Destroy(item2.gameObject);
                }
            }
            ValheimLegends.abilitiesStatus.Clear();
        }

        ValheimLegends.shouldUseGuardianPower = false;

        var zanim = (ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(player);
        zanim?.SetTrigger("gpower");

        if (ZNetScene.instance != null)
        {
            var fx = ZNetScene.instance.GetPrefab("vfx_odin_despawn");
            if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, Quaternion.identity);
            fx = ZNetScene.instance.GetPrefab("sfx_wraith_death");
            if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, Quaternion.identity);
        }

        //player.Message(MessageHud.MessageType.TopLeft, "Shapeshift: Fenring Form");
    }


    private static void TryActivate_CultistForm(Player player)
    {
        var seMan = player.GetSEMan();
        if (seMan == null) return;

        if (seMan.HaveStatusEffect(SE_FENRING_HASH) || seMan.HaveStatusEffect(SE_CULTIST_HASH))
        {
            player.Message(MessageHud.MessageType.TopLeft, "Already transformed.");
            return;
        }

        float cost = VL_Utility.GetDefenderCost; // como você pediu
        if (player.GetStamina() < cost)
        {
            player.Message(MessageHud.MessageType.TopLeft, $"Not enough stamina: ({player.GetStamina():#.#}/{cost})");
            return;
        }

        player.UseStamina(cost);

        var se = (SE_DruidCultistForm)ScriptableObject.CreateInstance(typeof(SE_DruidCultistForm));
        se.m_ttl = SE_DruidCultistForm.BaseTTL;

        ValheimLegends.shouldUseGuardianPower = false;
        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_odin_despawn"), player.transform.position, Quaternion.identity);
        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_wraith_death"), player.transform.position, Quaternion.identity);
        seMan.AddStatusEffect(se, resetTime: true);
        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetSpeed(2f);
        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_odin_despawn"), player.transform.position, Quaternion.identity);
        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_wraith_death"), player.transform.position, Quaternion.identity);

        player.Message(MessageHud.MessageType.TopLeft, "Transformed: Cultist Form");
    }

    public static ItemDrop.ItemData GetRightItemSafe(Player player)
    {
        if (player == null) return null;

        try
        {
            if (MI_GetRightItem != null)
                return MI_GetRightItem.Invoke(player, null) as ItemDrop.ItemData;
        }
        catch { /* ignore */ }

        try
        {
            if (FI_RightItem != null)
                return FI_RightItem.GetValue(player) as ItemDrop.ItemData;
        }
        catch { /* ignore */ }

        // Último fallback: arma atual (geralmente é a right-hand)
        try { return player.GetCurrentWeapon(); } catch { return null; }
    }

    private static ItemDrop.ItemData GetLeftItemSafe(Player player)
    {
        if (player == null) return null;

        try
        {
            if (MI_GetLeftItem != null)
                return MI_GetLeftItem.Invoke(player, null) as ItemDrop.ItemData;
        }
        catch { /* ignore */ }

        try
        {
            if (FI_LeftItem != null)
                return FI_LeftItem.GetValue(player) as ItemDrop.ItemData;
        }
        catch { /* ignore */ }

        return null;
    }

    private static void ForceWeaponRefresh(Player player)
    {
        if (player == null) return;

        // MP-safe: só owner mexe em equip
        var nview = player.GetComponent<ZNetView>();
        if (nview == null || !nview.IsOwner()) return;

        var right = GetRightItemSafe(player);
        var left = GetLeftItemSafe(player);

        // Se tem algo em alguma mão, só "refresh" do que estiver equipado
        if (right != null)
        {
            // triggerEquipEffects=false pra não tocar SFX/anim
            player.UnequipItem(right, true);
            player.EquipItem(right, true);
            return;
        }

        if (left != null)
        {
            player.UnequipItem(left, true);
            player.EquipItem(left, true);
            return;
        }

        // Caso NÃO tenha nada equipado nas mãos:
        // Buscar a primeira arma equipável no inventário, equipá-la e desequipá-la
        try
        {
            var inv = player.GetInventory();
            if (inv == null) return;

            // Procura algo que o Humanoid aceite equipar
            ItemDrop.ItemData candidate = null;
            foreach (var item in inv.GetAllItems())
            {
                if (item == null) continue;

                // Só itens equipáveis
                if (!item.IsEquipable()) continue;

                // Evita capacete/armadura/capa/cinto etc: tenta priorizar mão (weapon/tool/shield/torch)
                // (Mas o critério final é: EquipItem retornar true)
                candidate = item;

                // Tenta equipar silencioso; se funcionar, usa e para
                bool equipped = player.EquipItem(candidate, true);
                if (equipped)
                {
                    player.UnequipItem(candidate, true);
                    return;
                }

                // Se não equipou, continua buscando
                candidate = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[VL] ForceWeaponRefresh inventory fallback failed: " + e);
        }
    }

}
