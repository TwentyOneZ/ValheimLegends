using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace ValheimLegends
{
    /// <summary>
    /// Cache de membros privados (FieldInfo e MethodInfo) frequentemente acessados via reflection
    /// ou Traverse.Create em hot paths (combate, animações, watchdogs e status effects).
    /// </summary>
    public static class VL_ReflectCache
    {
        // Player / Character
        public static readonly FieldInfo FI_Character_ZAnim = AccessTools.Field(typeof(Character), "m_zanim") ?? AccessTools.Field(typeof(Player), "m_zanim");
        public static readonly FieldInfo FI_Character_Body = AccessTools.Field(typeof(Character), "m_body");
        public static readonly FieldInfo FI_Character_PushForce = AccessTools.Field(typeof(Character), "m_pushForce");
        public static readonly FieldInfo FI_Player_StaminaRegenDelay = AccessTools.Field(typeof(Player), "m_staminaRegenDelay");

        // Humanoid
        public static readonly FieldInfo FI_Humanoid_LeftItem = AccessTools.Field(typeof(Humanoid), "m_leftItem");
        public static readonly FieldInfo FI_Humanoid_RightItem = AccessTools.Field(typeof(Humanoid), "m_rightItem");
        public static readonly FieldInfo FI_Humanoid_PerfectBlock = AccessTools.Field(typeof(Humanoid), "m_perfectBlock");

        // Projectile
        public static readonly FieldInfo FI_Projectile_Skill = AccessTools.Field(typeof(Projectile), "m_skill");
        public static readonly FieldInfo FI_Projectile_Owner = AccessTools.Field(typeof(Projectile), "m_owner");

        // MonsterAI
        public static readonly FieldInfo FI_MonsterAI_Alerted = AccessTools.Field(typeof(MonsterAI), "m_alerted");
        public static readonly FieldInfo FI_MonsterAI_TargetCreature = AccessTools.Field(typeof(MonsterAI), "m_targetCreature");

        // Aoe
        public static readonly FieldInfo FI_Aoe_PushForce = AccessTools.Field(typeof(Aoe), "m_pushForce");

        // SEMan
        public static readonly FieldInfo FI_SEMan_Character = AccessTools.Field(typeof(SEMan), "m_character");

        // StatusEffect
        public static readonly FieldInfo FI_StatusEffect_Time = AccessTools.Field(typeof(StatusEffect), "m_time");

        // ObjectDB
        public static readonly FieldInfo FI_ObjectDB_ItemByHash = AccessTools.Field(typeof(ObjectDB), "m_itemByHash");

        // Methods
        public static readonly MethodInfo MI_Humanoid_BlockAttack = AccessTools.Method(typeof(Humanoid), "BlockAttack", new[] { typeof(HitData), typeof(Character) });
        public static readonly MethodInfo MI_Localization_AddWord = AccessTools.Method(typeof(Localization), "AddWord");

        // Helpers de alto desempenho
        public static Character GetSEManCharacter(SEMan seman)
        {
            return seman != null ? FI_SEMan_Character?.GetValue(seman) as Character : null;
        }

        public static float GetStatusEffectTime(StatusEffect se)
        {
            if (se != null && FI_StatusEffect_Time != null)
            {
                object val = FI_StatusEffect_Time.GetValue(se);
                if (val is float f) return f;
            }
            return 0f;
        }

        public static bool BlockAttack(Humanoid humanoid, HitData hit, Character attacker)
        {
            if (humanoid != null && MI_Humanoid_BlockAttack != null)
            {
                object res = MI_Humanoid_BlockAttack.Invoke(humanoid, new object[] { hit, attacker });
                if (res is bool b) return b;
            }
            return false;
        }

        public static System.Collections.Generic.Dictionary<int, GameObject> GetItemByHashDictionary(ObjectDB objectDB)
        {
            return objectDB != null ? FI_ObjectDB_ItemByHash?.GetValue(objectDB) as System.Collections.Generic.Dictionary<int, GameObject> : null;
        }

        public static ZSyncAnimation GetZAnim(Character character)
        {
            return character != null ? FI_Character_ZAnim?.GetValue(character) as ZSyncAnimation : null;
        }

        public static Rigidbody GetBody(Character character)
        {
            return character != null ? FI_Character_Body?.GetValue(character) as Rigidbody : null;
        }

        public static void SetCharacterPushForce(Character character, Vector3 pushForce)
        {
            if (character != null && FI_Character_PushForce != null)
            {
                FI_Character_PushForce.SetValue(character, pushForce);
            }
        }

        public static ItemDrop.ItemData GetLeftItem(Humanoid humanoid)
        {
            return humanoid != null ? FI_Humanoid_LeftItem?.GetValue(humanoid) as ItemDrop.ItemData : null;
        }

        public static ItemDrop.ItemData GetRightItem(Humanoid humanoid)
        {
            return humanoid != null ? FI_Humanoid_RightItem?.GetValue(humanoid) as ItemDrop.ItemData : null;
        }

        public static bool GetPerfectBlock(Humanoid humanoid)
        {
            if (humanoid != null && FI_Humanoid_PerfectBlock != null)
            {
                object val = FI_Humanoid_PerfectBlock.GetValue(humanoid);
                if (val is bool b) return b;
            }
            return false;
        }

        public static void SetProjectileSkill(Projectile projectile, Skills.SkillType skill)
        {
            if (projectile != null && FI_Projectile_Skill != null)
            {
                FI_Projectile_Skill.SetValue(projectile, skill);
            }
        }

        public static Character GetProjectileOwner(Projectile projectile)
        {
            return projectile != null ? FI_Projectile_Owner?.GetValue(projectile) as Character : null;
        }

        public static void ResetMonsterAggro(MonsterAI monsterAI)
        {
            if (monsterAI == null) return;
            if (FI_MonsterAI_Alerted != null) FI_MonsterAI_Alerted.SetValue(monsterAI, false);
            if (FI_MonsterAI_TargetCreature != null) FI_MonsterAI_TargetCreature.SetValue(monsterAI, null);
        }

        public static void SetAoePushForce(Aoe aoe, Vector3 pushForce)
        {
            if (aoe != null && FI_Aoe_PushForce != null)
            {
                FI_Aoe_PushForce.SetValue(aoe, pushForce);
            }
        }

        public static float GetStaminaRegenDelay(Player player)
        {
            if (player != null && FI_Player_StaminaRegenDelay != null)
            {
                object val = FI_Player_StaminaRegenDelay.GetValue(player);
                if (val is float f) return f;
            }
            return 1f;
        }

        public static void SetStaminaRegenDelay(Player player, float delay)
        {
            if (player != null && FI_Player_StaminaRegenDelay != null)
            {
                FI_Player_StaminaRegenDelay.SetValue(player, delay);
            }
        }
    }
}
