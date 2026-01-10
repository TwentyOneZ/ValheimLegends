// ============================================================================
// FILE: VL_AbilityBorrow.cs
// PURPOSE: Helper “borrowed abilities” used by Druid shapeshift forms.
//          This file exposes ONLY the entrypoints that Class_Druid calls:
//            - Berserker_Dash(Player)
//            - Valkyrie_Stagger(Player)
//            - Ranger_ShadowStalk(Player)
//            - Mage_Fireball(Player)
//            - Mage_Inferno(Player)
//            - Mage_Meditate(Player)
// ============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends
{
    public static class VL_AbilityBorrow
    {
        // Reuse the same layermask style used by several classes
        private static readonly int Script_Layermask = LayerMask.GetMask(
            "Default", "static_solid", "Default_small", "piece_nonsolid", "terrain",
            "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost"
        );

        // --------------------------------------------------------------------
        // Fenring Form (Ability3): Berserker Dash
        // --------------------------------------------------------------------
        public static void Berserker_Dash(Player player)
        {
            if (player == null) return;

            // We map Dash onto "Ability3" while in Fenring form, so we use Ability3 CD.
            int cdHash = "SE_VL_Ability3_CD".GetStableHashCode();
            if (player.GetSEMan().HaveStatusEffect(cdHash))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
                return;
            }

            float cost = VL_Utility.GetDashCost(player);
            if (player.GetStamina() < cost)
            {
                player.Message(MessageHud.MessageType.TopLeft,
                    $"Not enough stamina for Dash: ({player.GetStamina():#.#}/{cost})");
                return;
            }

            // Cooldown
            ValheimLegends.shouldUseGuardianPower = false;
            StatusEffect cd = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
            cd.m_ttl = VL_Utility.GetDashCooldown(player);
            player.GetSEMan().AddStatusEffect(cd);

            // Spend
            player.UseStamina(cost);

            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_longsword2");
            UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_stamina_medium"), player.transform.position, UnityEngine.Quaternion.identity);
            ValheimLegends.isChargingDash = true;
            ValheimLegends.dashCounter = 0;
            // Skill gain matches Berserker dash behavior (Discipline)
            player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetDashSkillGain(player));
        }

        // --------------------------------------------------------------------
        // Fenring Form (Ability2): Valkyrie Stagger
        // --------------------------------------------------------------------
        public static void Valkyrie_Stagger(Player player)
        {
            if (player == null) return;

            int cdHash = "SE_VL_Ability2_CD".GetStableHashCode();
            if (player.GetSEMan().HaveStatusEffect(cdHash))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
                return;
            }

            float cost = VL_Utility.GetStaggerCost; // NOTE: property, not method
            if (player.GetStamina() < cost)
            {
                player.Message(MessageHud.MessageType.TopLeft,
                    $"Not enough stamina for Stagger: ({player.GetStamina():#.#}/{cost})");
                return;
            }

            // Same Discipline scaling used by Valkyrie
            float level = player.GetSkills().GetSkillList()
                .FirstOrDefault(x => x.m_info == ValheimLegends.DisciplineSkillDef)
                .m_level * (1f + Mathf.Clamp(
                    (EpicMMOSystem.LevelSystem.Instance.getAddStamina() / 200f) +
                    (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 60f),
                    0f, 0.5f));

            ValheimLegends.shouldUseGuardianPower = false;

            StatusEffect cd = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
            // Valkyrie uses: GetStaggerCooldownTime * g_DamageModifer * c_valkyrieStaggerCooldown
            cd.m_ttl = VL_Utility.GetStaggerCooldownTime * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_valkyrieStaggerCooldown;
            player.GetSEMan().AddStatusEffect(cd);

            player.UseStamina(cost);

            // Animation
            try
            {
                var zanim = (ZSyncAnimation)typeof(Player)
                    .GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(Player.m_localPlayer);
                zanim?.SetTrigger("gpower");
            }
            catch { }

            // FX/SFX used by Valkyrie
            try
            {
                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_perfectblock"), player.transform.position, Quaternion.identity);
            }
            catch { }

            // Stagger pulse (same as Valkyrie)
            List<Character> list = new List<Character>();
            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Player.m_localPlayer)).SetTrigger("battleaxe_attack1");
            UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_troll_rock_destroyed"), player.transform.position, UnityEngine.Quaternion.identity);
            UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_sledge_iron_hit"), player.transform.position, UnityEngine.Quaternion.identity);
            List<Character> allCharacters = Character.GetAllCharacters();
            foreach (Character item in allCharacters)
            {
                if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 6f && VL_Utility.LOS_IsValid(item, player.transform.position, player.GetCenterPoint()))
                {
                    UnityEngine.Vector3 forceDirection = item.transform.position - player.transform.position;
                    item.Stagger(forceDirection);
                }
            }
            player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetStaggerSkillGain);
        }

        // --------------------------------------------------------------------
        // Fenring Form (Ability1): Ranger Shadow Stalk
        // --------------------------------------------------------------------
        public static void Ranger_ShadowStalk(Player player)
        {
            if (player == null) return;

            // We map Shadow Stalk onto "Ability1" while in Fenring form, so we use Ability1 CD.
            int cdHash = "SE_VL_Ability1_CD".GetStableHashCode();
            if (player.GetSEMan().HaveStatusEffect(cdHash))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
                return;
            }

            float cost = VL_Utility.GetShadowStalkCost(player); // method
            if (player.GetStamina() < cost)
            {
                player.Message(MessageHud.MessageType.TopLeft,
                    $"Not enough stamina for Shadow Stalk: ({player.GetStamina():#.#}/{cost})");
                return;
            }

            float level = player.GetSkills().GetSkillList()
                .FirstOrDefault(x => x.m_info == ValheimLegends.DisciplineSkillDef)
                .m_level * (1f + Mathf.Clamp(
                    (EpicMMOSystem.LevelSystem.Instance.getAddPhysicDamage() / 40f) +
                    (EpicMMOSystem.LevelSystem.Instance.getAddAttackSpeed() / 40f),
                    0f, 0.5f));

            StatusEffect cd = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
            cd.m_ttl = VL_Utility.GetShadowStalkCooldown(player);
            player.GetSEMan().AddStatusEffect(cd);

            player.UseStamina(cost);

            // FX/SFX
            try
            {
                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_odin_despawn"), player.transform.position, Quaternion.identity);
                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_wraith_death"), player.transform.position, Quaternion.identity);
            }
            catch { }

            // Apply ShadowStalk SE (existing SE in the project)
            SE_ShadowStalk se = (SE_ShadowStalk)ScriptableObject.CreateInstance(typeof(SE_ShadowStalk));
            se.m_ttl = SE_ShadowStalk.m_baseTTL * (1f + 0.02f * level);
            se.speedAmount = 1.5f + 0.01f * level * VL_GlobalConfigs.c_rangerShadowStalk;
            se.speedDuration = 3f + 0.03f * level;
            player.GetSEMan().AddStatusEffect(se);

            // Drop aggro from nearby monsters (same as Ranger)
            try
            {
                List<Character> chars = new List<Character>();
                Character.GetCharactersInRange(player.GetCenterPoint(), 500f, chars);

                foreach (Character c in chars)
                {
                    if (c?.GetBaseAI() is MonsterAI ai && ai.IsEnemy(player))
                    {
                        if (ai.GetTargetCreature() == player)
                        {
                            Traverse.Create(ai).Field("m_alerted").SetValue(false);
                            Traverse.Create(ai).Field("m_targetCreature").SetValue(null);
                        }
                    }
                }
            }
            catch { }

            player.RaiseSkill(ValheimLegends.DisciplineSkill, VL_Utility.GetShadowStalkSkillGain(player));
        }

        // --------------------------------------------------------------------
        // Cultist Form (Ability1): Mage Fireball
        // --------------------------------------------------------------------
        public static void Mage_Fireball(Player player)
        {
            if (player == null) return;

            int cdHash = "SE_VL_Ability1_CD".GetStableHashCode();
            if (player.GetSEMan().HaveStatusEffect(cdHash))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
                return;
            }

            float cost = VL_Utility.GetFireballCost; // property
            if (player.GetStamina() < cost)
            {
                player.Message(MessageHud.MessageType.TopLeft,
                    $"Not enough eitr for Fireball: ({player.GetEitr():#.#}/{cost})");
                return;
            }

            float level = player.GetSkills().GetSkillList()
                .FirstOrDefault(x => x.m_info == ValheimLegends.EvocationSkillDef)
                .m_level * (1f + Mathf.Clamp(
                    (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f) +
                    (EpicMMOSystem.LevelSystem.Instance.getEitrRegen() / 100f),
                    0f, 0.5f));

            ValheimLegends.shouldUseGuardianPower = false;

            StatusEffect cd = (SE_Ability1_CD)ScriptableObject.CreateInstance(typeof(SE_Ability1_CD));
            cd.m_ttl = VL_Utility.GetFireballCooldownTime;
            player.GetSEMan().AddStatusEffect(cd);

            player.UseStamina(cost);

            try
            {
                var zanim = (ZSyncAnimation)typeof(Player)
                    .GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(Player.m_localPlayer);
                zanim?.SetTrigger("gpower");
            }
            catch { }

            // Projectile
            GameObject prefab = ZNetScene.instance.GetPrefab("projectile_fireball");
            Vector3 spawn = player.transform.position + player.transform.up * 1.5f + player.GetLookDir() * 1.5f;

            GameObject go = UnityEngine.Object.Instantiate(prefab, spawn, Quaternion.identity);
            Projectile proj = go.GetComponent<Projectile>();

            proj.name = "Mage_Fireball";
            proj.m_respawnItemOnHit = false;
            proj.m_spawnOnHit = null;
            proj.m_ttl = 25f;
            proj.m_gravity = 0f;
            proj.m_rayRadius = 0.3f;
            proj.m_aoe = 4f + 0.02f * level;
            proj.m_hitNoise = 100f;

            HitData hit = new HitData();
            hit.m_damage.m_fire = UnityEngine.Random.Range(40f + 1.5f * level, 50f + 3f * level)
                                  * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFireball;

            proj.transform.localRotation = Quaternion.LookRotation(player.GetLookDir());
            proj.Setup(player, player.GetLookDir() * 50f, -1f, hit, null, null);

            Traverse.Create(proj).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);

            player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain);
        }

        // --------------------------------------------------------------------
        // Cultist Form (Ability2): Mage Inferno (Flame Nova) – same “queued dash attack” pattern as Mage
        // --------------------------------------------------------------------
        public static void Mage_Inferno(Player player)
        {
            if (player == null) return;

            int cdHash = "SE_VL_Ability2_CD".GetStableHashCode();
            if (player.GetSEMan().HaveStatusEffect(cdHash))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
                return;
            }

            float cost = VL_Utility.GetFrostNovaCost * 2f; // property
            if (player.GetStamina() < cost)
            {
                player.Message(MessageHud.MessageType.TopLeft,
                    $"Not enough eitr for Inferno: ({player.GetEitr():#.#}/{cost})");
                return;
            }

            float level = player.GetSkills().GetSkillList()
                .FirstOrDefault(x => x.m_info == ValheimLegends.EvocationSkillDef)
                .m_level * (1f + Mathf.Clamp(
                    (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f) +
                    (EpicMMOSystem.LevelSystem.Instance.getEitrRegen() / 100f),
                    0f, 0.5f));

            ValheimLegends.shouldUseGuardianPower = false;

            // CD
            StatusEffect cd = (SE_Ability2_CD)ScriptableObject.CreateInstance(typeof(SE_Ability2_CD));
            cd.m_ttl = VL_Utility.GetFrostNovaCooldownTime;
            player.GetSEMan().AddStatusEffect(cd);

            // Spend eitr
            player.UseStamina(cost);

            // Queue the Mage FlameNova and trigger the global dash/attack runner,
            // matching Class_Mage behavior (it uses isChargingDash as a short cast/attack runner).
            Vector3 lookDir = player.GetLookDir();
            lookDir.y = 0f;
            player.transform.rotation = Quaternion.LookRotation(lookDir);

            ValheimLegends.isChargingDash = true;
            ValheimLegends.dashCounter = 0;

            // Public on Class_Mage (confirmed by the Mage file)
            Class_Mage.QueuedAttack = Class_Mage.MageAttackType.FlameNova;

            // Skill gain
            player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFrostNovaSkillGain * 2f);
        }

        // --------------------------------------------------------------------
        // Cultist Form (Ability3): Mage Meditation – lifted from Class_Mage (no “block” requirement here)
        // --------------------------------------------------------------------
        public static void Mage_Meditate(Player player)
        {
            if (player == null) return;

            int cdHash = "SE_VL_Ability3_CD".GetStableHashCode();
            if (player.GetSEMan().HaveStatusEffect(cdHash))
            {
                player.Message(MessageHud.MessageType.TopLeft, "Ability not ready");
                return;
            }

            // Same scaling used by Mage meditation
            float level = player.GetSkills().GetSkillList()
                .FirstOrDefault(x => x.m_info == ValheimLegends.EvocationSkillDef)
                .m_level * (1f + Mathf.Clamp(
                    (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f) +
                    (EpicMMOSystem.LevelSystem.Instance.getEitrRegen() / 100f),
                    0f, 0.5f));

            float maxEitrRecover = 40f + 0.2f * level;
            float missingEitr = player.GetMaxEitr() - player.GetEitr();
            float eitrRecover = Mathf.Min(maxEitrRecover, missingEitr);

            if (eitrRecover <= 0.01f)
            {
                player.Message(MessageHud.MessageType.TopLeft, "Eitr already full.");
                return;
            }

            float maxStaminaSpend = 20f;
            float staminaSpend = Mathf.Min(maxStaminaSpend, player.GetStamina());
            if (staminaSpend <= 0.01f)
            {
                player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to meditate.");
                return;
            }

            // Mage logic uses up to 20 stamina to recover up to maxEitrRecover eitr.
            // It caps recoverEitr by available stamina.
            float recoverEitr = Mathf.Min(eitrRecover, staminaSpend);
            if (recoverEitr <= 0.01f) return;

            ValheimLegends.shouldUseGuardianPower = false;

            // CD scales with how much you recovered (same as Class_Mage)
            StatusEffect cd = (SE_Ability3_CD)ScriptableObject.CreateInstance(typeof(SE_Ability3_CD));
            cd.m_ttl = recoverEitr * (1f - EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 100f) * 0.5f;
            player.GetSEMan().AddStatusEffect(cd);

            // Spend stamina, gain eitr
            player.UseStamina(recoverEitr);
            player.AddEitr(recoverEitr);

            // FX
            try
            {
                UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_Potion_health_medium"), player.transform.position, Quaternion.identity);
            }
            catch { }

        }
    }
}
