using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends
{
    // =========================
    //  SE: Arcane Intellect
    //  Funcionalidade: Eitr custa Stamina primeiro
    //  Custo: 5x (lvl 0) a 2x (lvl 150)
    // =========================
    public class SE_ArcaneIntellect : StatusEffect
    {
        private float m_timer = 0f;
        private const float m_consumptionInterval = 20f;
        private int Hash_ArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();

        public SE_ArcaneIntellect()
        {
            base.name = "SE_VL_ArcaneIntellect";
            m_name = "Arcane Intellect";
            m_tooltip = "Eitr costs Stamina first.\nEfficiency improves with Evocation level (500% to 200% cost).\nConsumes 1 Arcane Charge every 15s.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("HelmetPointyHat");
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
                        m_character.Message(MessageHud.MessageType.TopLeft, "Arcane Intellect fades (No Charges)");
                        seman.RemoveStatusEffect(this.name.GetStableHashCode());
                    }
                }
            }
        }
    }

    internal static class ArcaneIntellectUtil
    {
        internal static readonly int SE_HASH = "SE_VL_ArcaneIntellect".GetStableHashCode();
        internal static bool RedirectingEitrCost = false;

        internal static bool HasArcane(Player p)
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
        /// Retorna o multiplicador de custo de Stamina.
        /// Level 0   = 5x (500%)
        /// Level 150 = 2x (200%)
        /// </summary>
        internal static float GetStaminaCostRatio(Player p)
        {
            float level = 0f;
            try
            {
                // Chama o método estático solicitado Class_Mage.GetEvocationLevel
                level = Class_Mage.GetEvocationLevel(p);
            }
            catch
            {
                level = 0f;
            }

            // Matemática Linear:
            // (0, 5) -> (150, 2)
            // m = (2 - 5) / 150 = -0.02
            // y = 5 - 0.02 * x

            float ratio = 3.0f - (level * 0.02f);
            return Mathf.Clamp(ratio, 1.0f, 3.0f);
        }
    }

    // =========================================================
    //  Patch: Gastar Eitr => gasta Stamina primeiro (com custo extra)
    // =========================================================
    [HarmonyPatch]
    public static class ArcaneIntellect_EitrCostRedirect_Patch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            var t = typeof(Player);
            var mUse = AccessTools.Method(t, "UseEitr", new[] { typeof(float) });
            if (mUse != null) yield return mUse;
            var mConsume = AccessTools.Method(t, "ConsumeEitr", new[] { typeof(float) });
            if (mConsume != null && mConsume != mUse) yield return mConsume;
        }

        private static bool Prefix(Player __instance, ref float v)
        {
            if (__instance == null || v <= 0f) return true;
            if (!ArcaneIntellectUtil.ShouldRunOnThisInstance(__instance)) return true;
            if (!ArcaneIntellectUtil.HasArcane(__instance)) return true;
            if (ArcaneIntellectUtil.RedirectingEitrCost) return true;

            float currentStamina = __instance.GetStamina();
            if (currentStamina <= 1.0f) return true; // Sem stamina mínima para converter

            // 1. Obter o custo (Ratio) baseado no Level
            float costRatio = ArcaneIntellectUtil.GetStaminaCostRatio(__instance);

            // 2. Calcular quanto de Eitr conseguimos pagar com a Stamina atual
            // Ex: Tenho 100 Stamina, Ratio é 5.0. Consigo pagar por 20 Eitr.
            float maxEitrAffordable = currentStamina / costRatio;

            // 3. O quanto vamos realmente pagar (limitado pelo custo original 'v')
            float eitrToOffset = Mathf.Min(v, maxEitrAffordable);

            if (eitrToOffset > 0f)
            {
                float staminaToConsume = eitrToOffset * costRatio;

                try
                {
                    ArcaneIntellectUtil.RedirectingEitrCost = true;
                    __instance.UseStamina(staminaToConsume);
                }
                finally
                {
                    ArcaneIntellectUtil.RedirectingEitrCost = false;
                }

                // Deduzimos do custo original de Eitr o que foi pago com Stamina
                v -= eitrToOffset;

                // Se pagamos tudo (v <= 0), impedimos o método original de rodar (retornando false)
                // Se sobrou v > 0, o método original roda e desconta o restante do Eitr real.
                if (v <= 0f) return false;
            }

            return true;
        }
    }
}