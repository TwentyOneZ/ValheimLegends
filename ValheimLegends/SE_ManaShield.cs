using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends
{
    // =========================
    //  SE: Eitr Shield
    //  Funcionalidade: Absorve até 100% do dano usando Eitr
    //  Custo: 500% (lvl 0) a 300% (lvl 150) do dano em Eitr
    // =========================
    public class SE_ManaShield : StatusEffect
    {
        private float m_timer = 0f;
        private const float m_consumptionInterval = 15f;
        private int Hash_ArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();

        public SE_ManaShield()
        {
            base.name = "SE_VL_ManaShield";
            m_name = "Eitr Shield";
            m_tooltip = "Absorbs damage using Eitr.\nEfficiency increases with Abjuration level.\nConsumes 1 Arcane Charge every 15s.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("StaminaUpgrade_Greydwarf");
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
                        GameObject vfx = ZNetScene.instance.GetPrefab("fx_VL_ParticleLightburst");
                        if (vfx) UnityEngine.Object.Instantiate(vfx, m_character.GetCenterPoint(), UnityEngine.Quaternion.LookRotation(UnityEngine.Vector3.up));
                        m_character.Message(MessageHud.MessageType.TopLeft, "Eitr Shield fades (No Charges)");
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

        /// <summary>
        /// Retorna o multiplicador de custo de Eitr baseado no level calculado.
        /// Level 0 = 5.0 (500%)
        /// Level 150 = 3.0 (300%)
        /// </summary>
        internal static float GetEitrCostRatio(float level)
        {
            // Interpolação Linear: y = mx + c
            // (0, 5) e (150, 3)
            // m = (3 - 5) / 150 = -0.013333...

            float ratio = 3.0f - (level * (2.0f / 150.0f));

            // Opcional: Clampar para nunca ficar abaixo de 100% (1.0f) se o level for absurdo
            return Mathf.Max(1.0f, ratio);
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
        private static readonly string[] FX_NAMES = { "fx_ShieldCharge_5", "fx_ShieldCharge_4", "fx_ShieldCharge_3" };
        private static readonly string[] SFX_NAMES = { "sfx_perfectblock", "sfx_ice_hit" };

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
    //  Patch 1: Normal Damage
    //  Absorve até 100% do dano dependendo do Eitr disponível.
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

            float totalDamage = ManaShieldUtil.SumDamage(hit);
            if (totalDamage <= 0.1f) return;

            float currentEitr = p.GetEitr();
            if (currentEitr <= 1f) return; // Sem Eitr suficiente para processar

            // 1. Calcular Level e Custo
            float calcLevel = Class_Mage.GetEvocationLevel(p);
            float costRatio = ManaShieldUtil.GetEitrCostRatio(calcLevel);

            // 2. Calcular quanto de dano podemos "comprar" com o Eitr atual
            // Ex: Tenho 100 Eitr, Ratio é 5.0. Posso absorver 20 de dano.
            float maxAbsorbableDamage = currentEitr / costRatio;

            // 3. Determinar absorção real (O menor entre o dano total e o que podemos pagar)
            float damageToAbsorb = Mathf.Min(totalDamage, maxAbsorbableDamage);

            // Se a absorção for irrisória, ignora
            if (damageToAbsorb <= 0.1f) return;

            // 4. Calcular custo final e consumir
            float eitrToConsume = damageToAbsorb * costRatio;
            p.AddEitr(-eitrToConsume);

            // 5. Aplicar redução no dano
            // Se absorvemos tudo (damageToAbsorb == totalDamage), damagePctRemaining = 0.
            float damagePctRemaining = 1f - (damageToAbsorb / totalDamage);
            ManaShieldUtil.ScaleDamage(hit, damagePctRemaining);

            // 6. Tocar efeitos
            ManaShieldUtil.TryPlayManaShieldFX(p);

            // 7. Aumentar Skill (Leveling)
            // Fórmula fornecida: raise skill based on shell utility calculation * 0.1f
            try
            {
                p.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetShellSkillGain(p) * 0.1f);
            }
            catch { /* Evitar erro se VL_Utility não for acessível */ }
        }
    }

    // =========================================================
    //  Patch 2: Parry (Opcional - Mantido para consistência)
    //  Também usa a nova lógica se ocorrer um Perfect Block
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

            // Verifica Perfect Block (Reflection)
            bool perfect = false;
            try
            {
                var f = AccessTools.Field(typeof(Humanoid), "m_perfectBlock");
                if (f != null) perfect = (bool)f.GetValue(__instance);
            }
            catch { }

            if (!perfect) return;

            float totalDamage = ManaShieldUtil.SumDamage(hit);
            if (totalDamage <= 0.1f) return;

            float currentEitr = p.GetEitr();
            if (currentEitr <= 1f) return;

            // Mesma lógica do Prefix acima para consistência
            float calcLevel = Class_Mage.GetEvocationLevel(p);
            float costRatio = ManaShieldUtil.GetEitrCostRatio(calcLevel);
            float maxAbsorbableDamage = currentEitr / costRatio;
            float damageToAbsorb = Mathf.Min(totalDamage, maxAbsorbableDamage);

            if (damageToAbsorb <= 0.1f) return;

            float eitrToConsume = damageToAbsorb * costRatio;
            p.AddEitr(-eitrToConsume);

            float damagePctRemaining = 1f - (damageToAbsorb / totalDamage);
            ManaShieldUtil.ScaleDamage(hit, damagePctRemaining);
            ManaShieldUtil.TryPlayManaShieldFX(p);

            try
            {
                p.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetShellSkillGain(p) * 0.1f);
            }
            catch { }
        }
    }
}