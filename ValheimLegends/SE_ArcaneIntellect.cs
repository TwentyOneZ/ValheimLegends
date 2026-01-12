using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends
{
    // =========================
    //  SE: Arcane Intellect
    //  Funcionalidade: Eitr custa Stamina primeiro (1:1)
    // =========================
    public class SE_ArcaneIntellect : StatusEffect
    {
        private float m_timer = 0f;
        private const float m_consumptionInterval = 15f;
        private int Hash_ArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();

        public SE_ArcaneIntellect()
        {
            base.name = "SE_VL_ArcaneIntellect";
            m_name = "Arcane Intellect";
            m_tooltip = "Eitr costs Stamina first.\nConsumes 1 Arcane Charge every 15s.";
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
    }

    // =========================================================
    //  Patch: Gastar Eitr => gasta Stamina primeiro
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

            float stamina = __instance.GetStamina();
            if (stamina <= 0.01f) return true;

            float payWithStamina = Mathf.Min(v, stamina);
            if (payWithStamina > 0f)
            {
                try
                {
                    ArcaneIntellectUtil.RedirectingEitrCost = true;
                    __instance.UseStamina(payWithStamina);
                }
                finally
                {
                    ArcaneIntellectUtil.RedirectingEitrCost = false;
                }

                v -= payWithStamina;
                if (v <= 0f) return false; // Tudo pago com stamina
            }
            return true; // Restante pago com Eitr
        }
    }
}