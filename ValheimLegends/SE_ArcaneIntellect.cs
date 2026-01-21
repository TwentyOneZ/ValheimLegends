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
            m_tooltip = "Eitr costs Stamina first.\nEfficiency improves with Evocation level (500% to 200% cost).\nConsumes 1 Arcane Charge every 20s.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("HelmetPointyHat");
                if (prefab) m_icon = prefab.GetComponent<ItemDrop>().m_itemData.GetIcon();
            }
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            // [CORREÇÃO MULTIPLAYER] 
            // Impede que outros clientes tentem gerenciar as cargas do seu personagem
            if (m_character != Player.m_localPlayer) return;

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
                        GameObject vfx = ZNetScene.instance.GetPrefab("vfx_HitSparks");
                        if (vfx) UnityEngine.Object.Instantiate(vfx, m_character.GetCenterPoint(), UnityEngine.Quaternion.LookRotation(UnityEngine.Vector3.up));
                        vfx = ZNetScene.instance.GetPrefab("sfx_lootspawn");
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

        internal static float GetStaminaCostRatio(Player p)
        {
            float level = 0f;
            try
            {
                level = Class_Mage.GetEvocationLevel(p);
            }
            catch
            {
                level = 0f;
            }

            float ratio = 3.0f - (level * 0.02f);
            return Mathf.Clamp(ratio, 1.0f, 3.0f);
        }
    }

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
            if (currentStamina <= 1.0f) return true;

            float costRatio = ArcaneIntellectUtil.GetStaminaCostRatio(__instance);
            float maxEitrAffordable = currentStamina / costRatio;
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

                v -= eitrToOffset;
                if (v <= 0f) return false;
            }

            return true;
        }
    }
}