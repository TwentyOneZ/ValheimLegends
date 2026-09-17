using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends
{
    // =========================
    //  SE: Arcane Intellect
    //  Funcionalidade: Eitr custa Stamina primeiro; concede 1 de Eitr Maximo enquanto ativo.
    //  Custo: 3x (lvl 0) a 1x (lvl 100)
    // =========================
    public class SE_ArcaneIntellect : StatusEffect
    {
        private float m_timer = 0f;
        private const float m_consumptionInterval = 20f;
        private int Hash_ArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();
        internal bool m_isStopping = false;

        public SE_ArcaneIntellect()
        {
            base.name = "SE_VL_ArcaneIntellect";
            m_name = "Arcane Intellect";
            m_tooltip = "Eitr costs Stamina first.\nEfficiency improves with Evocation level (300% to 100% cost).\nGrants +1 Max Eitr while active.\nConsumes 1 Arcane Charge every 20s.";
            if (ZNetScene.instance)
            {
                var prefab = ZNetScene.instance.GetPrefab("HelmetPointyHat");
                if (prefab) m_icon = prefab.GetComponent<ItemDrop>().m_itemData.GetIcon();
            }
        }

        public override void Setup(Character character)
        {
            base.Setup(character);
            m_isStopping = false;
            if (character != null && character.IsPlayer())
            {
                Player player = character as Player;
                player.AddEitr(1f);
            }
        }

        public override void Stop()
        {
            m_isStopping = true;
            base.Stop();
            if (m_character != null && m_character.IsPlayer())
            {
                Player player = m_character as Player;
                float baseMax = player.GetMaxEitr();
                if (player.GetEitr() > baseMax)
                {
                    float excess = player.GetEitr() - baseMax;
                    player.UseEitr(excess);
                }
            }
        }

        public override void UpdateStatusEffect(float dt)
        {
            base.UpdateStatusEffect(dt);

            // [CORRECAO MULTIPLAYER] 
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
            if (p == null) return false;
            var seman = p.GetSEMan();
            if (seman == null) return false;
            var se = seman.GetStatusEffect(SE_HASH) as SE_ArcaneIntellect;
            return se != null && !se.m_isStopping;
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

    [HarmonyPatch(typeof(Player), "GetMaxEitr")]
    public static class ArcaneIntellect_GetMaxEitr_Patch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            if (ArcaneIntellectUtil.HasArcane(__instance))
            {
                __result += 1f;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "HaveEitr")]
    public static class ArcaneIntellect_HaveEitr_Patch
    {
        private static void Postfix(Player __instance, float amount, ref bool __result)
        {
            if (!__result && ArcaneIntellectUtil.HasArcane(__instance))
            {
                float costRatio = ArcaneIntellectUtil.GetStaminaCostRatio(__instance);
                float availableEitr = __instance.GetEitr() + (__instance.GetStamina() / costRatio);
                if (availableEitr >= amount)
                {
                    __result = true;
                }
            }
        }
    }

    [HarmonyPatch]
    public static class ArcaneIntellect_EitrCostRedirect_Patch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(Player), "UseEitr", new[] { typeof(float) });
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