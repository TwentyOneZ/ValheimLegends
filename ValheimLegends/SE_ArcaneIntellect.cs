using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace ValheimLegends
{
    // =========================
    //  SE: Arcane Intellect (infinito)
    // =========================
    public class SE_ArcaneIntellect : SE_Stats
    {
        public SE_ArcaneIntellect()
        {
            name = "SE_VL_ArcaneIntellect";
            m_name = "Arcane Intelect";
            m_tooltip =
                "Arcane Intellect:\n" +
                "- Eitr costs Stamina first (1:1) until Stamina reaches 0.\n" +
                "- Mana Shield: part of incoming damage is paid with Eitr instead of HP (scales with Abjuration).\n" +
                "- On parry (perfect block): can absorb up to 100% (if enough Eitr).";
        }

        public override bool CanAdd(Character character) => character != null && character.IsPlayer();
        public override bool IsDone() => false;
    }

    internal static class ArcaneIntellectUtil
    {
        internal static readonly int SE_HASH = "SE_VL_ArcaneIntellect".GetStableHashCode();

        // evita loop quando redirecionamos custo de Eitr chamando UseStamina
        internal static bool RedirectingEitrCost = false;

        internal static bool HasArcane(Player p)
        {
            return p != null && p.GetSEMan() != null && p.GetSEMan().HaveStatusEffect(SE_HASH);
        }

        internal static bool ShouldRunOnThisInstance(Player p)
        {
            // roda no server OU no owner (SP/host ok)
            try
            {
                var nview = p != null ? p.GetComponent<ZNetView>() : null;
                bool isOwner = (nview == null) || nview.IsOwner();
                bool isServer = (ZNet.instance == null) || ZNet.instance.IsServer();
                return isOwner || isServer;
            }
            catch
            {
                return true;
            }
        }

        internal static float GetAbjurationLevel(Player p)
        {
            try
            {
                var skill = p.GetSkills().GetSkillList()
                    .FirstOrDefault(s => s.m_info == ValheimLegends.AbjurationSkillDef);

                return skill != null ? skill.m_level : 0f;
            }
            catch
            {
                return 0f;
            }
        }

        // 25%..75% baseado em Abjuration (0..100)
        internal static float GetManaShieldPercent(Player p)
        {
            float lvl = Mathf.Clamp(GetAbjurationLevel(p), 0f, 100f);
            return 0.25f + 0.50f * (lvl / 100f);
        }

        internal static float SumDamage(HitData hit)
        {
            if (hit == null) return 0f;
            var d = hit.m_damage;

            return
                d.m_blunt +
                d.m_slash +
                d.m_pierce +
                d.m_chop +
                d.m_pickaxe +
                d.m_fire +
                d.m_frost +
                d.m_lightning +
                d.m_poison +
                d.m_spirit;
        }

        internal static void ScaleDamage(HitData hit, float multiplier)
        {
            if (hit == null) return;
            multiplier = Mathf.Clamp01(multiplier);

            var d = hit.m_damage;
            d.m_blunt *= multiplier;
            d.m_slash *= multiplier;
            d.m_pierce *= multiplier;
            d.m_chop *= multiplier;
            d.m_pickaxe *= multiplier;
            d.m_fire *= multiplier;
            d.m_frost *= multiplier;
            d.m_lightning *= multiplier;
            d.m_poison *= multiplier;
            d.m_spirit *= multiplier;
            hit.m_damage = d;
        }

        // =========================
        //  FX/SFX Mana Shield
        // =========================
        private const float FX_COOLDOWN = 0.15f; // evita spam de efeitos
        private static readonly Dictionary<int, float> _lastFxTimeByInstance = new();

        private static readonly string[] FX_NAMES_NORMAL =
        {
            "fx_blocked",
            "vfx_blocked",
            "fx_shield_blocked",
            "vfx_shield_blocked",
            "fx_guardstone_permitted_removed", // fallback (existe na maioria)
        };

        private static readonly string[] FX_NAMES_PARRY =
        {
            "fx_perfectblock",
            "vfx_perfectblock",
            "fx_blocked",
            "vfx_blocked",
            "fx_guardstone_activate", // fallback (existe na maioria)
        };

        private static readonly string[] SFX_NAMES_NORMAL =
        {
            "sfx_blocked",
            "sfx_shield_blocked",
            "sfx_perfectblock", // fallback
        };

        private static readonly string[] SFX_NAMES_PARRY =
        {
            "sfx_perfectblock",
            "sfx_blocked",
        };

        internal static void TryPlayManaShieldFX(Player p, bool isParry)
        {
            if (p == null) return;

            // Só para feedback local do player (owner)
            var nview = p.GetComponent<ZNetView>();
            if (nview != null && !nview.IsOwner()) return;

            float now = Time.time;
            int key = p.GetInstanceID();

            if (_lastFxTimeByInstance.TryGetValue(key, out float last) && (now - last) < FX_COOLDOWN)
                return;

            _lastFxTimeByInstance[key] = now;

            Vector3 pos = p.GetCenterPoint();

            TrySpawnFirstExistingPrefab(pos, isParry ? FX_NAMES_PARRY : FX_NAMES_NORMAL);
            TrySpawnFirstExistingPrefab(pos, isParry ? SFX_NAMES_PARRY : SFX_NAMES_NORMAL);
        }

        private static void TrySpawnFirstExistingPrefab(Vector3 pos, string[] prefabNames)
        {
            if (ZNetScene.instance == null || prefabNames == null) return;

            for (int i = 0; i < prefabNames.Length; i++)
            {
                string name = prefabNames[i];
                if (string.IsNullOrWhiteSpace(name)) continue;

                var prefab = ZNetScene.instance.GetPrefab(name);
                if (prefab == null) continue;

                UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
                return;
            }
        }

    }

    // =========================================================
    //  1) Arcane Intellect: gastar Eitr => gasta Stamina primeiro (1:1)
    //     - até stamina chegar em 0
    //     - resto paga em Eitr normalmente
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

        // reduz "v" (restante) e/ou cancela o original
        private static bool Prefix(Player __instance, ref float v)
        {
            if (__instance == null) return true;
            if (v <= 0f) return true;

            if (!ArcaneIntellectUtil.ShouldRunOnThisInstance(__instance)) return true;
            if (!ArcaneIntellectUtil.HasArcane(__instance)) return true;
            if (ArcaneIntellectUtil.RedirectingEitrCost) return true;

            float stamina = __instance.GetStamina();
            if (stamina <= 0.01f) return true; // stamina zerada: gasta eitr normal

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

                // zerou custo => cancela gasto de eitr
                if (v <= 0f)
                    return false;
            }

            // sobrou custo => original vai gastar Eitr do restante
            return true;
        }
    }

    // =========================================================
    //  2) Mana Shield (normal): Character.Damage Prefix
    //     - absorve 25%..75% (Abjuration) em Eitr
    // =========================================================
    [HarmonyPatch]
    public static class ArcaneIntellect_ManaShield_NormalDamage_Patch
    {
        static MethodBase TargetMethod()
        {
            // Preferência: void Character.Damage(HitData hit)
            var m = AccessTools.Method(typeof(Character), "Damage", new[] { typeof(HitData) });
            if (m != null) return m;

            // Fallback: qualquer overload Damage cujo 1º param seja HitData
            return AccessTools.GetDeclaredMethods(typeof(Character))
                .FirstOrDefault(x =>
                {
                    if (x.Name != "Damage") return false;
                    var p = x.GetParameters();
                    return p.Length >= 1 && p[0].ParameterType == typeof(HitData);
                });
        }

        private static void Prefix(Character __instance, HitData hit)
        {
            if (__instance == null || hit == null) return;
            if (!(__instance is Player p)) return;

            if (!ArcaneIntellectUtil.ShouldRunOnThisInstance(p)) return;
            if (!ArcaneIntellectUtil.HasArcane(p)) return;

            float total = ArcaneIntellectUtil.SumDamage(hit);
            if (total <= 0.01f) return;

            float pct = ArcaneIntellectUtil.GetManaShieldPercent(p); // 0.25..0.75
            float desiredAbsorb = total * pct;

            float curEitr = p.GetEitr();
            if (curEitr <= 0.01f) return;

            float absorb = Mathf.Min(desiredAbsorb, curEitr);
            if (absorb <= 0.01f) return;

            float effectivePct = Mathf.Clamp01(absorb / total);

            // reduz dano que iria pro HP
            ArcaneIntellectUtil.ScaleDamage(hit, 1f - effectivePct);

            // paga em Eitr (não interceptado pelo redirect, pois é AddEitr)
            p.AddEitr(-absorb);
            ArcaneIntellectUtil.TryPlayManaShieldFX(p, isParry: false);
        }
    }

    // =========================================================
    //  3) Mana Shield (PARry / perfect block):
    //     Ao aparar, absorve ATÉ 100% do dano (limitado ao Eitr),
    //     independentemente de Abjuration.
    //
    //     Implementação: patcha Humanoid.BlockAttack Postfix.
    //     Se foi perfect block, pega o dano restante (após block),
    //     e zera via Eitr se houver o bastante.
    // =========================================================
    [HarmonyPatch]
    public static class ArcaneIntellect_ManaShield_Parry_Patch
    {
        static MethodBase TargetMethod()
        {
            // Assinatura típica: bool Humanoid.BlockAttack(HitData hit, Character attacker)
            var m = AccessTools.Method(typeof(Humanoid), "BlockAttack", new[] { typeof(HitData), typeof(Character) });
            if (m != null) return m;

            // fallback: busca por nome/param
            return AccessTools.GetDeclaredMethods(typeof(Humanoid))
                .FirstOrDefault(x =>
                {
                    if (x.Name != "BlockAttack") return false;
                    var p = x.GetParameters();
                    return p.Length >= 1 && p[0].ParameterType == typeof(HitData);
                });
        }

        [HarmonyPostfix]
        private static void Postfix(Humanoid __instance, HitData hit, bool __result)
        {
            if (!__result) return; // não bloqueou
            if (__instance == null || hit == null) return;
            if (!(__instance is Player p)) return;

            if (!ArcaneIntellectUtil.ShouldRunOnThisInstance(p)) return;
            if (!ArcaneIntellectUtil.HasArcane(p)) return;

            // Detecta "perfect block" (parry) via field privado, se existir
            bool perfect = false;
            try
            {
                var f = AccessTools.Field(typeof(Humanoid), "m_perfectBlock");
                if (f != null && f.FieldType == typeof(bool))
                {
                    perfect = (bool)f.GetValue(__instance);
                }
            }
            catch
            {
                perfect = false;
            }

            if (!perfect) return; // só aplica 100% no parry

            // dano restante (já passou por block/parry logic)
            float total = ArcaneIntellectUtil.SumDamage(hit);
            if (total <= 0.01f) return;

            float curEitr = p.GetEitr();
            if (curEitr <= 0.01f) return;

            // absorve até 100% se eitr suficiente
            float absorb = Mathf.Min(total, curEitr);
            if (absorb <= 0.01f) return;

            float effectivePct = Mathf.Clamp01(absorb / total);

            ArcaneIntellectUtil.ScaleDamage(hit, 1f - effectivePct);
            p.AddEitr(-absorb);
            ArcaneIntellectUtil.TryPlayManaShieldFX(p, isParry: true);
        }
    }
}
