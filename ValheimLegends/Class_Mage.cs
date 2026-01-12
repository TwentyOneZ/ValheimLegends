using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ValheimLegends
{
    public class Class_Mage
    {
        public enum MageAffinity { None, Fire, Frost, Arcane }
        public enum MageAttackType { None = 0, IceShard = 20, FlameNova = 60 }

        public static MageAttackType QueuedAttack;

        private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");
        private static int ScriptChar_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost");

        // Hashes
        private static int Hash_FireAffinity = "SE_VL_MageFireAffinity".GetStableHashCode();
        private static int Hash_FrostAffinity = "SE_VL_MageFrostAffinity".GetStableHashCode();
        private static int Hash_ArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();
        private static int Hash_ElementalMastery = "SE_VL_ElementalMastery".GetStableHashCode();
        public static int Hash_Frozen = "SE_VL_Frozen".GetStableHashCode();
        public static int Hash_Slow = "SE_Slow".GetStableHashCode();

        // Variables
        private static bool meteorCharging = false;
        private static int meteorCount;
        private static int meteorChargeAmount;
        private static int meteorChargeAmountMax;
        private static float meteorSkillGain = 0f;
        private static GameObject GO_CastFX;

        // Blizzard Variables
        private static bool blizzardCharging = false;
        private static float blizzardChargeTimer = 0f;
        private static float blizzardSpawnTimer = 0f;

        public static void Process_Input(Player player, float altitude)
        {
            EnsureAffinities(player);

            if (player.IsBlocking())
            {
                if (VL_Utility.Ability3_Input_Down) { SetAffinityFocus(player, MageAffinity.Fire); return; }
                else if (VL_Utility.Ability2_Input_Down) { SetAffinityFocus(player, MageAffinity.Frost); return; }
                else if (VL_Utility.Ability1_Input_Down) { SetAffinityFocus(player, MageAffinity.Arcane); return; }
            }

            MageAffinity currentFocus = GetCurrentFocus(player);

            if (currentFocus == MageAffinity.Fire) Process_Fire_Input(player, altitude);
            else if (currentFocus == MageAffinity.Frost) Process_Frost_Input(player, altitude);
            else if (currentFocus == MageAffinity.Arcane) Process_Arcane_Input(player);
        }

        // --- FIRE SKILLSET ---
        private static void Process_Fire_Input(Player player, float altitude)
        {
            float level = GetEvocationLevel(player);

            // Ability 1: Fireball
            if (VL_Utility.Ability1_Input_Down)
            {
                if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
                {
                    SE_MageFireAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        if (player.GetStamina() >= VL_Utility.GetFireballCost)
                        {
                            affinity.ConsumeCharges(1);
                            ValheimLegends.shouldUseGuardianPower = false;
                            StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability1_CD>();
                            cd.m_ttl = VL_Utility.GetFireballCooldownTime - 0.02f * level;
                            player.GetSEMan().AddStatusEffect(cd);
                            player.UseStamina(VL_Utility.GetFireballCost);
                            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
                            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetSpeed(3f);

                            GameObject fx = ZNetScene.instance.GetPrefab("fx_VL_Flames");
                            if (fx) GO_CastFX = UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);

                            SpawnFireball(player, level);
                            player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain);
                        }
                        else player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina");
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 1 Fire Charge");
                }
            }

            // Ability 2: Flame Nova
            if (VL_Utility.Ability2_Input_Down)
            {
                if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
                {
                    SE_MageFireAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
                    if (affinity != null && affinity.m_currentCharges >= 3)
                    {
                        if (player.GetStamina() >= VL_Utility.GetFrostNovaCost)
                        {
                            affinity.ConsumeCharges(3);
                            StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability2_CD>();
                            cd.m_ttl = VL_Utility.GetFrostNovaCooldownTime;
                            player.GetSEMan().AddStatusEffect(cd);
                            player.UseStamina(VL_Utility.GetFrostNovaCost);
                            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_sledge");
                            ValheimLegends.isChargingDash = true;
                            ValheimLegends.dashCounter = 0;
                            QueuedAttack = MageAttackType.FlameNova;
                        }
                        else player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina");
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 3 Fire Charges");
                }
            }

            // Ability 3: Meteor
            Process_Meteor_Logic(player, level);
        }

        // --- FROST SKILLSET ---
        private static void Process_Frost_Input(Player player, float altitude)
        {
            float level = GetEvocationLevel(player);

            // Ability 1: Ice Shard
            if (VL_Utility.Ability1_Input_Down)
            {
                if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
                {
                    if (player.GetStamina() >= VL_Utility.GetFireballCost * 0.5f)
                    {
                        StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability1_CD>();
                        cd.m_ttl = 0.5f;
                        player.GetSEMan().AddStatusEffect(cd);
                        player.UseStamina(VL_Utility.GetFireballCost * 0.5f);

                        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");

                        ValheimLegends.isChargingDash = true;
                        ValheimLegends.dashCounter = 0;
                        QueuedAttack = MageAttackType.IceShard;

                        player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.5f);
                        SE_MageFrostAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
                        if (affinity != null) affinity.AddCharges(1);
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina");
                }
            }

            // Ability 2: Frost Nova
            if (VL_Utility.Ability2_Input_Down)
            {
                if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
                {
                    SE_MageFrostAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
                    if (affinity != null && affinity.m_currentCharges >= 3)
                    {
                        if (player.GetStamina() >= VL_Utility.GetFrostNovaCost)
                        {
                            affinity.ConsumeCharges(3);
                            player.UseStamina(VL_Utility.GetFrostNovaCost);

                            StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability2_CD>();
                            cd.m_ttl = VL_Utility.GetFrostNovaCooldownTime;
                            player.GetSEMan().AddStatusEffect(cd);

                            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_axe1");

                            GameObject fx = ZNetScene.instance.GetPrefab("fx_guardstone_activate");
                            if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);

                            if (player.GetSEMan().HaveStatusEffect("Burning".GetStableHashCode()))
                                player.GetSEMan().RemoveStatusEffect("Burning".GetStableHashCode());

                            bool hasElementalMastery = player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery);
                            List<Character> allCharacters = Character.GetAllCharacters();
                            foreach (Character item in allCharacters)
                            {
                                if (BaseAI.IsEnemy(player, item) &&
                                    (item.transform.position - player.transform.position).magnitude <= 10f + 0.1f * level &&
                                    VL_Utility.LOS_IsValid(item, player.GetCenterPoint(), player.transform.position + player.transform.up * 0.15f))
                                {
                                    HitData hitData2 = new HitData();
                                    hitData2.m_damage.m_frost = UnityEngine.Random.Range(10f + 0.5f * level, 20f + level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFrostNova;

                                    if (hasElementalMastery) AddElementalMasteryDamage(player, ref hitData2);

                                    hitData2.m_pushForce = 20f;
                                    hitData2.m_dir = item.transform.position - player.transform.position;
                                    hitData2.m_skill = ValheimLegends.EvocationSkill;
                                    hitData2.SetAttacker(player);
                                    item.Damage(hitData2);

                                    if (Hash_Frozen != 0) item.GetSEMan().AddStatusEffect(Hash_Frozen, true);
                                }
                            }
                            player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFrostNovaSkillGain);
                        }
                        else player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina");
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 3 Frost Charges");
                }
            }

            Process_Blizzard_Logic(player, level);
        }

        // --- ARCANE SKILLSET ---
        private static void Process_Arcane_Input(Player player)
        {
            SE_MageArcaneAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;

            void ToggleArcaneBuff(string buffHashName, string buffClassName, string msgName, string vfxPrefab)
            {
                int buffHash = buffHashName.GetStableHashCode();
                if (!player.GetSEMan().HaveStatusEffect(buffHash))
                {
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        Type type = Type.GetType("ValheimLegends." + buffClassName);
                        if (type != null)
                        {
                            StatusEffect se = (StatusEffect)ScriptableObject.CreateInstance(type);
                            if (se != null)
                            {
                                affinity.ConsumeCharges(1);
                                player.GetSEMan().AddStatusEffect(se);

                                GameObject vfx = ZNetScene.instance.GetPrefab(vfxPrefab);
                                if (vfx) UnityEngine.Object.Instantiate(vfx, player.GetCenterPoint(), Quaternion.identity);

                                player.Message(MessageHud.MessageType.TopLeft, $"{msgName}: ON");
                            }
                        }
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 1 Arcane Charge");
                }
                else
                {
                    player.GetSEMan().RemoveStatusEffect(buffHash);
                    player.Message(MessageHud.MessageType.TopLeft, $"{msgName}: OFF");
                }
            }

            if (VL_Utility.Ability1_Input_Down && !player.GetSEMan().HaveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode()))
            {
                ToggleArcaneBuff("SE_VL_ElementalMastery", "SE_ElementalMastery", "Elemental Mastery", "fx_guardstone_activate");
                StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability1_CD>(); cd.m_ttl = 1f;
                player.GetSEMan().AddStatusEffect(cd);
            }
            if (VL_Utility.Ability2_Input_Down && !player.GetSEMan().HaveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode()))
            {
                ToggleArcaneBuff("SE_VL_ArcaneIntellect", "SE_ArcaneIntellect", "Arcane Intellect", "fx_VL_ChiPulse");
                StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability2_CD>(); cd.m_ttl = 1f;
                player.GetSEMan().AddStatusEffect(cd);
            }
            if (VL_Utility.Ability3_Input_Down && !player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
            {
                ToggleArcaneBuff("SE_VL_ManaShield", "SE_ManaShield", "Mana Shield", "fx_guardstone_permitted_removed");
                StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability3_CD>(); cd.m_ttl = 1f;
                player.GetSEMan().AddStatusEffect(cd);
            }
        }

        // --- BLIZZARD LOGIC ---
        private static void Process_Blizzard_Logic(Player player, float level)
        {
            if (VL_Utility.Ability3_Input_Down && !blizzardCharging)
            {
                if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
                {
                    SE_MageFrostAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        ValheimLegends.shouldUseGuardianPower = false;
                        ValheimLegends.isChanneling = true;
                        blizzardCharging = true;
                        blizzardChargeTimer = 1.1f;
                        blizzardSpawnTimer = 0f;
                        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
                        player.StartEmote("point");
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 1 Frost Charge to start");
                }
            }
            else if (VL_Utility.Ability3_Input_Pressed && blizzardCharging)
            {
                if (player.GetStamina() <= 5f) { 
                    blizzardCharging = false;
                    ValheimLegends.isChanneling = false;
                    ValheimLegends.channelingBlocksMovement = true;

                    typeof(Player).GetMethod("StopEmote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(player, null);

                    StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability3_CD>(); cd.m_ttl = 10f;
                    player.GetSEMan().AddStatusEffect(cd);
                    return; 
                }

                ValheimLegends.isChanneling = true;
                player.UseStamina(10f * Time.deltaTime);

                blizzardSpawnTimer += Time.deltaTime;
                if (blizzardSpawnTimer >= 0.05f)
                {
                    blizzardSpawnTimer = 0f;
                    SpawnBlizzardShard(player, level);
                }

                blizzardChargeTimer += Time.deltaTime;
                if (blizzardChargeTimer >= 1.0f)
                {
                    SE_MageFrostAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        affinity.ConsumeCharges(1);
                        blizzardChargeTimer = 0f;
                    }
                    else
                    {
                        blizzardCharging = false;
                        ValheimLegends.isChanneling = false;
                        ValheimLegends.channelingBlocksMovement = true;

                        typeof(Player).GetMethod("StopEmote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(player, null);

                        StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability3_CD>(); cd.m_ttl = 10f;
                        player.GetSEMan().AddStatusEffect(cd);
                        player.Message(MessageHud.MessageType.TopLeft, "Out of Frost Charges");
                    }
                }
            }
            else if (blizzardCharging && (VL_Utility.Ability3_Input_Up || player.GetStamina() <= 5f))
            {
                blizzardCharging = false;
                ValheimLegends.isChanneling = false;
                ValheimLegends.channelingBlocksMovement = true;

                typeof(Player).GetMethod("StopEmote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(player, null);

                StatusEffect cd = ScriptableObject.CreateInstance<SE_Ability3_CD>(); cd.m_ttl = 10f;
                player.GetSEMan().AddStatusEffect(cd);
            }
        }

        // --- EXECUTE & SPAWNERS ---
        public static void Execute_Attack(Player player)
        {
            bool hasElementalMastery = player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery);
            float level = GetEvocationLevel(player);

            if (QueuedAttack == MageAttackType.FlameNova)
            {
                GameObject fx = ZNetScene.instance.GetPrefab("fx_VL_FlameBurst");
                if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);

                foreach (Character item in Character.GetAllCharacters())
                {
                    if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 8f + 0.1f * level &&
                        VL_Utility.LOS_IsValid(item, player.GetCenterPoint(), player.transform.position + player.transform.up * 0.2f))
                    {
                        HitData hitData = new HitData();
                        hitData.m_damage.m_fire = UnityEngine.Random.Range(5f + 2.75f * level, 10f + 3.5f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageInferno;
                        if (hasElementalMastery) AddElementalMasteryDamage(player, ref hitData);
                        hitData.m_skill = ValheimLegends.EvocationSkill;
                        hitData.SetAttacker(player);
                        item.Damage(hitData);
                    }
                }
            }
            else if (QueuedAttack == MageAttackType.IceShard)
            {
                GameObject prefab = ZNetScene.instance.GetPrefab("VL_FrostDagger");
                if (prefab == null) prefab = ZNetScene.instance.GetPrefab("ice_arrow");

                if (prefab != null)
                {
                    UnityEngine.Vector3 vector = player.GetEyePoint() + player.GetLookDir() * 0.2f + player.transform.up * 0.1f + player.transform.right * 0.28f;
                    GameObject gameObject = UnityEngine.Object.Instantiate(prefab, vector, UnityEngine.Quaternion.identity);

                    Projectile component = gameObject.GetComponent<Projectile>();
                    component.name = "IceShard";
                    component.m_respawnItemOnHit = false;
                    component.m_spawnOnHit = null;
                    component.m_ttl = 1.5f;
                    component.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector));
                    gameObject.transform.localScale = UnityEngine.Vector3.one * 0.8f;

                    RaycastHit hitInfo = default(RaycastHit);
                    UnityEngine.Vector3 target = ((!Physics.Raycast(vector, player.GetLookDir(), out hitInfo, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo.collider)
                        ? (player.transform.position + player.GetLookDir() * 1000f) : hitInfo.point);

                    HitData hitData2 = new HitData();
                    hitData2.m_damage.m_pierce = UnityEngine.Random.Range(2f + 0.25f * level, 5f + 0.75f * level) * VL_GlobalConfigs.c_mageFrostDagger * VL_GlobalConfigs.g_DamageModifer;
                    hitData2.m_damage.m_frost = UnityEngine.Random.Range(0.5f * level, 2f + 1f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFrostDagger;
                    if (hasElementalMastery) AddElementalMasteryDamage(player, ref hitData2);
                    hitData2.m_skill = ValheimLegends.EvocationSkill;
                    hitData2.SetAttacker(player);

                    UnityEngine.Vector3 vector2 = UnityEngine.Vector3.MoveTowards(gameObject.transform.position, target, 1f);
                    component.Setup(player, (vector2 - gameObject.transform.position) * 55f, -1f, hitData2, null, null);
                    Traverse.Create(component).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
                }
            }
            QueuedAttack = MageAttackType.None;
        }

        // --- SPAWNERS ---
        private static void SpawnBlizzardShard(Player player, float level)
        {
            UnityEngine.Vector3 targetPoint;
            RaycastHit hitInfo;
            if (Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, 30f, Script_Layermask)) targetPoint = hitInfo.point;
            else targetPoint = player.GetEyePoint() + player.GetLookDir() * 30f;

            UnityEngine.Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 6f;
            UnityEngine.Vector3 spawnPos = targetPoint + new UnityEngine.Vector3(randomCircle.x, 20f, randomCircle.y);

            GameObject prefab = ZNetScene.instance.GetPrefab("VL_FrostDagger");
            if (prefab == null) prefab = ZNetScene.instance.GetPrefab("ice_arrow");

            if (prefab != null)
            {
                GameObject go = UnityEngine.Object.Instantiate(prefab, spawnPos, UnityEngine.Quaternion.LookRotation(UnityEngine.Vector3.down));
                Projectile p = go.GetComponent<Projectile>();
                p.name = "BlizzardShard";
                p.m_gravity = 20f; 
                p.m_ttl = 5f;

                HitData hit = new HitData();
                hit.m_damage.m_frost = 5f + (level * 0.5f);
                if (player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery)) AddElementalMasteryDamage(player, ref hit);
                hit.SetAttacker(player);
                hit.m_skill = ValheimLegends.EvocationSkill;

                p.Setup(player, UnityEngine.Vector3.down * 25f, -1f, hit, null, null);
            }
        }

        private static void Process_Meteor_Logic(Player player, float level)
        {
            if (VL_Utility.Ability3_Input_Down && !meteorCharging)
            {
                SE_MageFireAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
                if (!player.GetSEMan().HaveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode()))
                {
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        if (player.GetStamina() >= VL_Utility.GetMeteorCost)
                        {
                            ValheimLegends.shouldUseGuardianPower = false;
                            ValheimLegends.isChanneling = true;
                            affinity.ConsumeCharges(1);
                            meteorSkillGain = 0f;
                            player.UseStamina(VL_Utility.GetMeteorCost);
                            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");

                            GameObject fx = ZNetScene.instance.GetPrefab("fx_VL_Flames");
                            if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);

                            meteorCharging = true;
                            meteorChargeAmount = 0;
                            meteorChargeAmountMax = Mathf.RoundToInt(20f * (1f - level / 200f));
                            meteorCount = 1;
                            meteorSkillGain += VL_Utility.GetMeteorSkillGain;
                        }
                        else player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina");
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 1 Fire Charge");
                }
            }
            else if (VL_Utility.Ability3_Input_Pressed && meteorCharging && player.GetStamina() > 1f)
            {
                SE_MageFireAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
                VL_Utility.SetTimer();
                meteorChargeAmount++;
                player.UseStamina(VL_Utility.GetMeteorCostPerUpdate);
                ValheimLegends.isChanneling = true;
                if (meteorChargeAmount >= meteorChargeAmountMax)
                {
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        affinity.ConsumeCharges(1);
                        meteorCount++;
                        meteorChargeAmount = 0;
                        GameObject fx = ZNetScene.instance.GetPrefab("fx_VL_Flames");
                        if (fx) GO_CastFX = UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);
                        meteorSkillGain += 0.2f;
                    }
                }
            }
            else if ((VL_Utility.Ability3_Input_Up || player.GetStamina() <= 1f) && meteorCharging)
            {
                meteorCharging = false;
                CastMeteor(player, meteorCount);
                StatusEffect statusEffect = ScriptableObject.CreateInstance<SE_Ability3_CD>();
                statusEffect.m_ttl = VL_Utility.GetMeteorCooldownTime;
                player.GetSEMan().AddStatusEffect(statusEffect);
                meteorCount = 0;
                ValheimLegends.isChanneling = false;
                ValheimLegends.channelingBlocksMovement = true;
                player.RaiseSkill(ValheimLegends.EvocationSkill, meteorSkillGain);
            }
        }
        private static void SpawnFireball(Player player, float level)
        {
            UnityEngine.Vector3 vector3 = player.transform.position + player.transform.up * 2.5f + player.GetLookDir() * 0.5f;
            GameObject prefab2 = ZNetScene.instance.GetPrefab("Imp_fireball_projectile");
            GameObject GO_Fireball = UnityEngine.Object.Instantiate(prefab2, new UnityEngine.Vector3(vector3.x, vector3.y, vector3.z), UnityEngine.Quaternion.identity);
            Projectile P_Fireball = GO_Fireball.GetComponent<Projectile>();
            P_Fireball.name = "Fireball";
            P_Fireball.m_respawnItemOnHit = false;
            P_Fireball.m_spawnOnHit = null;
            P_Fireball.m_ttl = 60f;
            P_Fireball.m_gravity = 2.5f;
            P_Fireball.m_rayRadius = 0.1f;
            P_Fireball.m_aoe = 3f + 0.03f * level;
            P_Fireball.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector3));
            GO_Fireball.transform.localScale = UnityEngine.Vector3.zero;
            RaycastHit hitInfo2 = default(RaycastHit);
            UnityEngine.Vector3 position2 = player.transform.position;
            UnityEngine.Vector3 target2 = ((!Physics.Raycast(vector3, player.GetLookDir(), out hitInfo2, float.PositiveInfinity, Script_Layermask) || !hitInfo2.collider)
                ? (position2 + player.GetLookDir() * 1000f) : hitInfo2.point);
            HitData hitData3 = new HitData();
            hitData3.m_damage.m_fire = UnityEngine.Random.Range(5f + 1.6f * level, 10f + 1.8f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFireball;
            hitData3.m_damage.m_blunt = UnityEngine.Random.Range(5f + 0.9f * level, 10f + 1.1f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFireball;
            hitData3.m_pushForce = 2f;
            hitData3.m_skill = ValheimLegends.EvocationSkill;
            hitData3.SetAttacker(player);
            if (player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery)) AddElementalMasteryDamage(player, ref hitData3);
            UnityEngine.Vector3 vector4 = UnityEngine.Vector3.MoveTowards(GO_Fireball.transform.position, target2, 1f);
            P_Fireball.Setup(player, (vector4 - GO_Fireball.transform.position) * 25f, -1f, hitData3, null, null);
            Traverse.Create(P_Fireball).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
        }
        private static void CastMeteor(Player player, int count)
        {
            float level = GetEvocationLevel(player);
            System.Random random = new System.Random();
            UnityEngine.Vector3 targetBase = player.transform.position + player.transform.up * 2f + player.GetLookDir() * 10f;
            RaycastHit hitInfo = default(RaycastHit);
            if (Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, 1000f, ScriptChar_Layermask)) targetBase = hitInfo.point;
            GameObject prefab = ZNetScene.instance.GetPrefab("projectile_meteor");
            bool hasElementalMastery = player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery);
            for (int i = 0; i < count; i++)
            {
                UnityEngine.Vector3 spawnPos = new UnityEngine.Vector3(targetBase.x + (float)random.Next(-10, 10), targetBase.y + 100f, targetBase.z + (float)random.Next(-10, 10));
                GameObject go = UnityEngine.Object.Instantiate(prefab, spawnPos, UnityEngine.Quaternion.identity);
                Projectile p = go.GetComponent<Projectile>();
                HitData hitData = new HitData();
                hitData.m_damage.m_fire = UnityEngine.Random.Range(30f + 0.5f * level, 50f * level) * VL_GlobalConfigs.g_DamageModifer;
                hitData.m_damage.m_blunt = UnityEngine.Random.Range(15f, 30f);
                hitData.SetAttacker(player);
                hitData.m_skill = ValheimLegends.EvocationSkill;
                if (hasElementalMastery) AddElementalMasteryDamage(player, ref hitData);
                UnityEngine.Vector3 target = targetBase;
                target.x += random.Next(-8, 8);
                target.z += random.Next(-8, 8);
                p.Setup(player, (target - spawnPos).normalized * 50f, -1f, hitData, null, null);
            }
        }
        private static void AddElementalMasteryDamage(Player player, ref HitData hitData)
        {
            if (player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery))
            {
                HitData.DamageTypes weaponDamage = player.GetCurrentWeapon().GetDamage();
                hitData.m_damage.m_fire += weaponDamage.m_fire; hitData.m_damage.m_frost += weaponDamage.m_frost;
                hitData.m_damage.m_lightning += weaponDamage.m_lightning; hitData.m_damage.m_poison += weaponDamage.m_poison;
                hitData.m_damage.m_spirit += weaponDamage.m_spirit;
            }
        }
        private static float GetEvocationLevel(Player player)
        {
            return player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef).m_level
                  * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
        }
        private static void EnsureAffinities(Player player)
        {
            var seman = player.GetSEMan();
            if (!seman.HaveStatusEffect(Hash_FireAffinity)) seman.AddStatusEffect(ScriptableObject.CreateInstance<SE_MageFireAffinity>());
            if (!seman.HaveStatusEffect(Hash_FrostAffinity)) seman.AddStatusEffect(ScriptableObject.CreateInstance<SE_MageFrostAffinity>());
            if (!seman.HaveStatusEffect(Hash_ArcaneAffinity))
            {
                var arcane = ScriptableObject.CreateInstance<SE_MageArcaneAffinity>();
                seman.AddStatusEffect(arcane);
                if (GetCurrentFocus(player) == MageAffinity.None) arcane.SetFocus(true);
            }
        }
        private static void SetAffinityFocus(Player player, MageAffinity target)
        {
            var seman = player.GetSEMan();
            var fire = seman.GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
            var frost = seman.GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
            var arcane = seman.GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;

            SE_MageAffinityBase targetAffinity = null;
            string fxPrefabName = "";
            switch (target)
            {
                case MageAffinity.Fire: targetAffinity = fire; fxPrefabName = "vfx_Potion_health_medium"; break;
                case MageAffinity.Frost: targetAffinity = frost; fxPrefabName = "fx_Potion_frostresist"; break;
                case MageAffinity.Arcane: targetAffinity = arcane; fxPrefabName = "vfx_Potion_stamina_medium"; break;
            }

            if (targetAffinity != null && targetAffinity.isFocused)
            {
                float cost = Mathf.Max(50f, player.GetMaxStamina() * 0.8f);
                if (player.GetStamina() >= cost)
                {
                    player.UseStamina(cost);
                    targetAffinity.AddCharges(1);

                    if (!string.IsNullOrEmpty(fxPrefabName))
                    {
                        GameObject prefab = ZNetScene.instance.GetPrefab(fxPrefabName);
                        if (prefab) UnityEngine.Object.Instantiate(prefab, player.transform.position, UnityEngine.Quaternion.identity);
                    }

                    //player.Message(MessageHud.MessageType.Center, $"Recharged {targetAffinity.m_name}");
                }
                else player.Message(MessageHud.MessageType.TopLeft, $"Not enough stamina to recharge ({player.GetStamina():0}/{cost:0})");
                return;
            }

            if (fire == null || frost == null || arcane == null) return;
            if ((target == MageAffinity.Fire && fire.isFocused) || (target == MageAffinity.Frost && frost.isFocused) || (target == MageAffinity.Arcane && arcane.isFocused)) return;
            fire.SetFocus(target == MageAffinity.Fire);
            frost.SetFocus(target == MageAffinity.Frost);
            arcane.SetFocus(target == MageAffinity.Arcane);
            ValheimLegends.NameCooldowns();
            if (ValheimLegends.abilitiesStatus != null)
            {
                foreach (RectTransform item2 in ValheimLegends.abilitiesStatus) { if (item2 != null && item2.gameObject != null) UnityEngine.Object.Destroy(item2.gameObject); }
                ValheimLegends.abilitiesStatus.Clear();
            }
            string msg = "";
            switch (target) { case MageAffinity.Fire: msg = "Focus: Fire"; break; case MageAffinity.Frost: msg = "Focus: Frost"; break; case MageAffinity.Arcane: msg = "Focus: Arcane"; break; }
            player.Message(MessageHud.MessageType.Center, msg);
        }
        public static MageAffinity GetCurrentFocus(Player player)
        {
            var seman = player.GetSEMan();
            var fire = seman.GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
            if (fire != null && fire.isFocused) return MageAffinity.Fire;
            var frost = seman.GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
            if (frost != null && frost.isFocused) return MageAffinity.Frost;
            var arcane = seman.GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;
            if (arcane != null && arcane.isFocused) return MageAffinity.Arcane;
            return MageAffinity.None;
        }
    }

    [HarmonyPatch(typeof(Projectile), "OnHit")]
    public static class Mage_IceShard_Shatter_Patch
    {
        private static void Prefix(Projectile __instance, Collider collider, UnityEngine.Vector3 hitPoint, bool water)
        {
            if (__instance == null) return;

            string name = __instance.name;

            // 1. BLIZZARD SHARD LOGIC
            if (name == "BlizzardShard")
            {
                Character attacker = Traverse.Create(__instance).Field("m_owner").GetValue<Character>();
                if (attacker == null || !attacker.IsPlayer()) return;

                GameObject vfx = ZNetScene.instance.GetPrefab("fx_Frost_Cone");
                if (vfx == null) vfx = ZNetScene.instance.GetPrefab("vfx_Ice_destroyed");
                if (vfx != null) UnityEngine.Object.Instantiate(vfx, hitPoint, UnityEngine.Quaternion.identity);

                // CORREÇÃO: Acesso direto a m_damage (público)
                HitData.DamageTypes damageTypes = __instance.m_damage;
                Skills.SkillType skill = __instance.m_skill;
                // float pushForce = __instance.m_pushForce; // Campo privado, usar traverse se necessário ou valor fixo
                float pushForce = 10f; // Valor fixo para garantir

                List<Character> allCharacters = new List<Character>();
                Character.GetCharactersInRange(hitPoint, 6f, allCharacters);

                foreach (Character item in allCharacters)
                {
                    if (!BaseAI.IsEnemy(attacker, item)) continue;

                    HitData hit = new HitData();
                    hit.m_damage = damageTypes.Clone();
                    hit.m_point = item.GetCenterPoint();
                    hit.m_dir = (item.transform.position - hitPoint).normalized;
                    hit.m_pushForce = pushForce;
                    hit.m_skill = skill;
                    hit.SetAttacker(attacker);

                    item.Damage(hit);

                    var seman = item.GetSEMan();
                    if (seman.HaveStatusEffect(Class_Mage.Hash_Slow) || seman.HaveStatusEffect("SE_Frost".GetStableHashCode()))
                    {
                        seman.RemoveStatusEffect(Class_Mage.Hash_Slow);
                        seman.RemoveStatusEffect("SE_Frost".GetStableHashCode());
                        if (Class_Mage.Hash_Frozen != 0) seman.AddStatusEffect(Class_Mage.Hash_Frozen, true);
                    }
                    else
                    {
                        if (Class_Mage.Hash_Slow != 0) seman.AddStatusEffect(Class_Mage.Hash_Slow, true);
                    }
                }

                // Anula dano do projétil original
                __instance.m_damage = new HitData.DamageTypes();
                return;
            }

            // 2. ICE SHARD SHATTER LOGIC
            if (name.StartsWith("IceShard"))
            {
                Character attacker = Traverse.Create(__instance).Field("m_owner").GetValue<Character>();
                if (attacker == null || !attacker.IsPlayer()) return;

                GameObject go = collider ? Projectile.FindHitObject(collider) : null;
                IDestructible destructible = go ? go.GetComponent<IDestructible>() : null;
                Character victim = destructible as Character;

                if (victim != null)
                {
                    if (victim.GetSEMan().HaveStatusEffect(Class_Mage.Hash_Frozen))
                    {
                        victim.GetSEMan().RemoveStatusEffect(Class_Mage.Hash_Frozen);
                        victim.GetSEMan().RemoveStatusEffect(Class_Mage.Hash_Slow);

                        // CORREÇÃO: Acesso direto a m_damage
                        __instance.m_damage.Modify(3.0f);

                        GameObject vfx = ZNetScene.instance.GetPrefab("vfx_Ice_destroyed");
                        if (vfx) UnityEngine.Object.Instantiate(vfx, hitPoint, UnityEngine.Quaternion.identity);

                        attacker.Message(MessageHud.MessageType.Center, "Shattered!");
                    }
                    else
                    {
                        if (Class_Mage.Hash_Slow != 0) victim.GetSEMan().AddStatusEffect(Class_Mage.Hash_Slow, true);
                    }
                }
            }
        }
    }
}