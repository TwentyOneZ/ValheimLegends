using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends
{
    // =========================
    //  SE: Mana Shield
    //  Funcionalidade: Absorve dano usando Eitr
    // =========================
    public class SE_ManaShield : StatusEffect
    {
        private float m_timer = 0f;
        private const float m_consumptionInterval = 15f;
        private int Hash_ArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();

        public SE_ManaShield()
        {
            base.name = "SE_VL_ManaShield";
            m_name = "Mana Shield";
            m_tooltip = "Absorbs damage using Eitr.\nConsumes 1 Arcane Charge every 15s.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("ShieldBanded");
                if (prefab) m_icon = prefab.GetComponent<ItemDrop>().m_itemData.GetIcon();
            }
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            m_timer += dt;
            if (m_timer >= m_consumptionInterval)
            {
                m_timer = 0f;
                if (m_character.IsPlayer())
                {
                    var seman = m_character.GetSEMan();
                    SE_MageArcaneAffinity affinity = seman.GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        affinity.ConsumeCharges(1);
                    }
                    else
                    {
                        m_character.Message(MessageHud.MessageType.TopLeft, "Mana Shield fades (No Charges)");
                        seman.RemoveStatusEffect(this.name.GetStableHashCode());
                    }
                }
            }
        }
    }
    internal static class ManaShieldUtil
    {
        internal static readonly int SE_HASH = "SE_VL_ManaShield".GetStableHashCode();

        internal static bool HasManaShield(Player p)
        {
            return p != null && p.GetSEMan() != null && p.GetSEMan().HaveStatusEffect(SE_HASH);
        }

        internal static bool ShouldRunOnThisInstance(Player p)
        {
            try
            {
                var nview = p != null ? p.GetComponent<ZNetView>() : null;
                bool isOwner = (nview == null) || nview.IsOwner();
                bool isServer = (ZNet.instance == null) || ZNet.instance.IsServer();
                return isOwner || isServer;
            }
            catch { return true; }
        }

        internal static float GetAbjurationLevel(Player p)
        {
            try
            {
                // Certifique-se que ValheimLegends.AbjurationSkillDef está acessível
                var skill = p.GetSkills().GetSkillList().FirstOrDefault(s => s.m_info == ValheimLegends.AbjurationSkillDef);
                return skill != null ? skill.m_level : 0f;
            }
            catch { return 0f; }
        }

        internal static float GetManaShieldPercent(Player p)
        {
            float lvl = Mathf.Clamp(GetAbjurationLevel(p), 0f, 100f);
            return 0.25f + 0.50f * (lvl / 100f); // 25% a 75%
        }

        internal static float SumDamage(HitData hit)
        {
            if (hit == null) return 0f;
            var d = hit.m_damage;
            return d.m_blunt + d.m_slash + d.m_pierce + d.m_chop + d.m_pickaxe +
                   d.m_fire + d.m_frost + d.m_lightning + d.m_poison + d.m_spirit;
        }

        internal static void ScaleDamage(HitData hit, float multiplier)
        {
            if (hit == null) return;
            multiplier = Mathf.Clamp01(multiplier);
            var d = hit.m_damage;
            d.m_blunt *= multiplier; d.m_slash *= multiplier; d.m_pierce *= multiplier;
            d.m_chop *= multiplier; d.m_pickaxe *= multiplier; d.m_fire *= multiplier;
            d.m_frost *= multiplier; d.m_lightning *= multiplier; d.m_poison *= multiplier; d.m_spirit *= multiplier;
            hit.m_damage = d;
        }

        // --- FX Logic ---
        private const float FX_COOLDOWN = 0.15f;
        private static readonly Dictionary<int, float> _lastFxTimeByInstance = new();
        private static readonly string[] FX_NAMES = { "fx_blocked", "vfx_blocked", "fx_guardstone_permitted_removed" };
        private static readonly string[] SFX_NAMES = { "sfx_blocked", "sfx_shield_blocked" };

        internal static void TryPlayManaShieldFX(Player p)
        {
            if (p == null) return;
            var nview = p.GetComponent<ZNetView>();
            if (nview != null && !nview.IsOwner()) return;

            float now = Time.time;
            int key = p.GetInstanceID();
            if (_lastFxTimeByInstance.TryGetValue(key, out float last) && (now - last) < FX_COOLDOWN) return;

            _lastFxTimeByInstance[key] = now;
            TrySpawnFirstExistingPrefab(p.GetCenterPoint(), FX_NAMES);
            TrySpawnFirstExistingPrefab(p.GetCenterPoint(), SFX_NAMES);
        }

        private static void TrySpawnFirstExistingPrefab(Vector3 pos, string[] prefabNames)
        {
            if (ZNetScene.instance == null || prefabNames == null) return;
            foreach (string name in prefabNames)
            {
                var prefab = ZNetScene.instance.GetPrefab(name);
                if (prefab) { UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity); return; }
            }
        }
    }

    // =========================================================
    //  Patch 1: Normal Damage (Absorve % baseada em Abjuration)
    // =========================================================
    [HarmonyPatch]
    public static class ManaShield_NormalDamage_Patch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Character), "Damage", new[] { typeof(HitData) });
        }

        [HarmonyPrefix]
        private static void Prefix(Character __instance, HitData hit)
        {
            if (__instance == null || hit == null) return;
            if (!(__instance is Player p)) return;

            if (!ManaShieldUtil.ShouldRunOnThisInstance(p)) return;
            if (!ManaShieldUtil.HasManaShield(p)) return;

            float total = ManaShieldUtil.SumDamage(hit);
            if (total <= 0.01f) return;

            float pct = ManaShieldUtil.GetManaShieldPercent(p);
            float desiredAbsorb = total * pct;
            float curEitr = p.GetEitr();

            if (curEitr <= 0.01f) return;

            float absorb = Mathf.Min(desiredAbsorb, curEitr);
            if (absorb <= 0.01f) return;

            float effectivePct = Mathf.Clamp01(absorb / total);

            // Aplica redução
            ManaShieldUtil.ScaleDamage(hit, 1f - effectivePct);
            p.AddEitr(-absorb); // Consome Eitr
            ManaShieldUtil.TryPlayManaShieldFX(p);
        }
    }

    // =========================================================
    //  Patch 2: Parry (Absorve até 100%)
    // =========================================================
    [HarmonyPatch]
    public static class ManaShield_Parry_Patch
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Humanoid), "BlockAttack", new[] { typeof(HitData), typeof(Character) });
        }

        [HarmonyPostfix]
        private static void Postfix(Humanoid __instance, HitData hit, bool __result)
        {
            if (!__result) return; // Não bloqueou
            if (__instance == null || hit == null) return;
            if (!(__instance is Player p)) return;

            if (!ManaShieldUtil.ShouldRunOnThisInstance(p)) return;
            if (!ManaShieldUtil.HasManaShield(p)) return;

            // Check Perfect Block (Reflection)
            bool perfect = false;
            try
            {
                var f = AccessTools.Field(typeof(Humanoid), "m_perfectBlock");
                if (f != null) perfect = (bool)f.GetValue(__instance);
            }
            catch { }

            if (!perfect) return; // Só aplica no Parry

            float total = ManaShieldUtil.SumDamage(hit);
            if (total <= 0.01f) return;

            float curEitr = p.GetEitr();
            if (curEitr <= 0.01f) return;

            float absorb = Mathf.Min(total, curEitr);
            if (absorb <= 0.01f) return;

            float effectivePct = Mathf.Clamp01(absorb / total);

            ManaShieldUtil.ScaleDamage(hit, 1f - effectivePct);
            p.AddEitr(-absorb);
            ManaShieldUtil.TryPlayManaShieldFX(p);
        }
    }
}