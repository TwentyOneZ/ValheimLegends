using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEngine;

namespace ValheimLegends
{
    // =========================
    //  SE: Eitr Shield
    //  Funcionalidade: Absorve dano usando Eitr (e Stamina com Arcane Intellect).
    //  Custo: 1 Carga Arcana por Hit.
    //  Cooldown: 20s ao desativar/quebrar.
    // =========================
    public class SE_ManaShield : StatusEffect
    {
        // Timer e Intervalo removidos (não consome mais por tempo)

        public SE_ManaShield()
        {
            base.name = "SE_VL_ManaShield";
            m_name = "Eitr Shield";
            m_tooltip = "Absorbs damage using Eitr.\nConsumes 1 Arcane Charge per hit.\nCooldown applied when effect ends.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("StaminaUpgrade_Greydwarf");
                if (prefab) m_icon = prefab.GetComponent<ItemDrop>().m_itemData.GetIcon();
            }
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);
            // Lógica de tempo removida.
        }

    }

    internal static class ManaShieldUtil
    {
        internal static readonly int SE_HASH = "SE_VL_ManaShield".GetStableHashCode();
        internal static readonly int ARCANE_AFFINITY_HASH = "SE_VL_MageArcaneAffinity".GetStableHashCode();

        // Cache para o método AddCooldown da Class_Mage (caso seja privado ou static)
        private static MethodInfo _addCooldownMethod;

        internal static void ApplyCooldown(Player p, string abilityName, float duration)
        {
            // Tenta invocar Class_Mage.AddCooldown via Reflection para garantir compatibilidade
            if (_addCooldownMethod == null)
            {
                _addCooldownMethod = AccessTools.Method(typeof(Class_Mage), "AddCooldown", new Type[] { typeof(string), typeof(float) });
            }

            if (_addCooldownMethod != null)
            {
                // AddCooldown é static na Class_Mage na maioria das implementações deste mod
                _addCooldownMethod.Invoke(null, new object[] { abilityName, duration });
            }
        }

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
        /// Mantido conforme original (Revert)
        /// </summary>
        internal static float GetEitrCostRatio(float level)
        {
            float ratio = 3.0f - (level * (2.0f / 150.0f));
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

        // --- Lógica Centralizada de Absorção ---
        internal static float ProcessAbsorption(Player p, float totalDamage)
        {
            if (totalDamage <= 0.1f) return 0f;

            // 1. VERIFICAÇÃO DE CARGAS (Nova Lógica: 1 Carga por Hit)
            var seman = p.GetSEMan();
            var affinity = seman.GetStatusEffect(ARCANE_AFFINITY_HASH) as SE_MageAffinityBase;

            if (affinity != null && affinity.m_currentCharges >= 1)
            {
                // Consome 1 carga obrigatória
                affinity.ConsumeCharges(1);
            }
            else
            {
                // Sem cargas = Quebra o Escudo
                GameObject vfx = ZNetScene.instance.GetPrefab("fx_VL_ParticleLightburst");
                if (vfx) UnityEngine.Object.Instantiate(vfx, p.GetCenterPoint(), UnityEngine.Quaternion.LookRotation(UnityEngine.Vector3.up));

                p.Message(MessageHud.MessageType.TopLeft, "Eitr Shield faded (No Charges)");

                // Remover o efeito chamará SE_ManaShield.Stop(), que aplicará o Cooldown
                seman.RemoveStatusEffect(SE_HASH);
                // Calcula cooldown: 20s * Redução
                float cdDuration = 20f * Class_Mage.GetCooldownReduction(p);

                // Aplica o Cooldown
                ManaShieldUtil.ApplyCooldown(p, "Mana Shield", cdDuration);
                return 0f;
            }

            // 2. RECURSOS E CÁLCULOS (Lógica Mantida)
            float currentEitr = p.GetEitr();
            float currentStamina = p.GetStamina();

            bool hasArcaneIntellect = ArcaneIntellectUtil.HasArcane(p);
            float staminaToEitrRatio = hasArcaneIntellect ? ArcaneIntellectUtil.GetStaminaCostRatio(p) : 0f;

            float effectiveEitrPool = currentEitr;
            if (hasArcaneIntellect && currentStamina > 1f)
            {
                effectiveEitrPool += currentStamina / staminaToEitrRatio;
            }

            if (effectiveEitrPool <= 1f) return 0f;

            float mageLevel = Class_Mage.GetEvocationLevel(p);
            float manaShieldCostRatio = GetEitrCostRatio(mageLevel);

            float maxAbsorbableDamage = effectiveEitrPool / manaShieldCostRatio;
            float damageToAbsorb = Mathf.Min(totalDamage, maxAbsorbableDamage);

            if (damageToAbsorb <= 0.1f) return 0f;

            float totalEitrCost = damageToAbsorb * manaShieldCostRatio;
            float eitrRemainingToPay = totalEitrCost;

            if (hasArcaneIntellect && currentStamina > 1f)
            {
                float maxEitrStaminaCanPay = currentStamina / staminaToEitrRatio;
                float eitrPaidByStamina = Mathf.Min(eitrRemainingToPay, maxEitrStaminaCanPay);

                if (eitrPaidByStamina > 0f)
                {
                    float staminaCost = eitrPaidByStamina * staminaToEitrRatio;
                    p.UseStamina(staminaCost);
                    eitrRemainingToPay -= eitrPaidByStamina;
                }
            }

            if (eitrRemainingToPay > 0f)
            {
                p.AddEitr(-eitrRemainingToPay);
            }

            TryPlayManaShieldFX(p);
            try
            {
                p.RaiseSkill(ValheimLegends.AbjurationSkill, VL_Utility.GetShellSkillGain(p) * 0.1f);
            }
            catch { }

            return damageToAbsorb;
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

            float absorbedAmount = ManaShieldUtil.ProcessAbsorption(p, totalDamage);

            if (absorbedAmount > 0f)
            {
                float damagePctRemaining = 1f - (absorbedAmount / totalDamage);
                ManaShieldUtil.ScaleDamage(hit, damagePctRemaining);
            }
        }
    }

    // =========================================================
    //  Patch 2: Parry
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
            if (!__result) return;
            if (__instance == null || hit == null) return;
            if (!(__instance is Player p)) return;

            if (!ManaShieldUtil.ShouldRunOnThisInstance(p)) return;
            if (!ManaShieldUtil.HasManaShield(p)) return;

            bool perfect = false;
            try
            {
                var f = AccessTools.Field(typeof(Humanoid), "m_perfectBlock");
                if (f != null) perfect = (bool)f.GetValue(__instance);
            }
            catch { }

            if (!perfect) return;

            float totalDamage = ManaShieldUtil.SumDamage(hit);

            float absorbedAmount = ManaShieldUtil.ProcessAbsorption(p, totalDamage);

            if (absorbedAmount > 0f)
            {
                float damagePctRemaining = 1f - (absorbedAmount / totalDamage);
                ManaShieldUtil.ScaleDamage(hit, damagePctRemaining);
            }
        }
    }
}