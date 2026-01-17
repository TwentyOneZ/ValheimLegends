using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ValheimLegends
{
    public class Class_Mage
    {
        public enum MageAffinity { None, Fire, Frost, Arcane }
        public enum MageAttackType { None = 0, FlameNova = 20 }

        public static MageAttackType QueuedAttack;

        private static int Script_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock");
        private static int ScriptChar_Layermask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece_nonsolid", "terrain", "vehicle", "piece", "viewblock", "character", "character_net", "character_ghost");

        // Hashes
        private static int Hash_FireAffinity = "SE_VL_MageFireAffinity".GetStableHashCode();
        private static int Hash_FrostAffinity = "SE_VL_MageFrostAffinity".GetStableHashCode();
        private static int Hash_ArcaneAffinity = "SE_VL_MageArcaneAffinity".GetStableHashCode();
        private static int Hash_ElementalMastery = "SE_VL_ElementalMastery".GetStableHashCode();
        public static int Hash_Frozen = "SE_VL_Frozen".GetStableHashCode();
        public static int Hash_Slow = "SE_VL_Slow".GetStableHashCode(); // Changed name to ensure uniqueness
        public static int Hash_FrostImmunity = "SE_VL_FrostImmunity".GetStableHashCode();

        // Hashes de Visual CD
        private static int Hash_CD1 = "SE_VL_Ability1_CD".GetStableHashCode();
        private static int Hash_CD2 = "SE_VL_Ability2_CD".GetStableHashCode();
        private static int Hash_CD3 = "SE_VL_Ability3_CD".GetStableHashCode();

        // Variables Meteor
        private static bool meteorCharging = false;
        private static int meteorCount;
        private static float meteorTimer = 0f;
        private static float meteorSkillGain = 0f;
        private static GameObject GO_CastFX;

        // Blizzard Variables
        private static bool blizzardCharging = false;
        private static float blizzardChargeTimer = 0f;
        private static float blizzardSpawnTimer = 0f;
        private static int blizzardTickCount = 0;

        // Meditation Variables
        private static float meditationTimer = 0f;
        private static bool isMeditating = false;
        private static MageAffinity meditatonFocus = MageAffinity.None; // Para saber qual botão estamos segurando

        // --- SISTEMA DE COOLDOWN LÓGICO ---
        private static Dictionary<string, float> InternalCooldowns = new Dictionary<string, float>();

        public static int Hash_BlizzardImmunity = "SE_VL_BlizzardImmunity".GetStableHashCode();

        private static void UpdateInternalCooldowns(float dt)
        {
            if (InternalCooldowns.Count == 0) return;
            List<string> keys = new List<string>(InternalCooldowns.Keys);
            foreach (var key in keys)
            {
                InternalCooldowns[key] -= dt;
                if (InternalCooldowns[key] <= 0) InternalCooldowns.Remove(key);
            }
        }

        public static void AddCooldown(string id, float duration)
        {
            if (InternalCooldowns.ContainsKey(id)) InternalCooldowns[id] = duration;
            else InternalCooldowns.Add(id, duration);
        }

        public static bool IsOnCooldown(string id)
        {
            return InternalCooldowns.ContainsKey(id) && InternalCooldowns[id] > 0;
        }

        private static float GetRemainingCooldown(string id)
        {
            return InternalCooldowns.ContainsKey(id) ? InternalCooldowns[id] : 0f;
        }

        // --- LOGICA DE ACUMULO DE DANO (FROZEN) ---
        private static Dictionary<ZDOID, float> FrozenDamageRegistry = new Dictionary<ZDOID, float>();

        public static void AddFrozenDamage(Character c, float dmg)
        {
            if (c == null) return;
            ZDOID id = c.GetZDOID();
            if (FrozenDamageRegistry.ContainsKey(id)) FrozenDamageRegistry[id] += dmg;
            else FrozenDamageRegistry.Add(id, dmg);
        }

        public static float GetFrozenDamage(Character c)
        {
            if (c == null) return 0f;
            ZDOID id = c.GetZDOID();
            return FrozenDamageRegistry.ContainsKey(id) ? FrozenDamageRegistry[id] : 0f;
        }

        public static void ClearFrozenDamage(Character c)
        {
            if (c == null) return;
            ZDOID id = c.GetZDOID();
            if (FrozenDamageRegistry.ContainsKey(id)) FrozenDamageRegistry.Remove(id);
        }

        // --- PROCESS INPUT ---
        // --- PROCESS INPUT ---
        public static void Process_Input(Player player, float altitude)
        {

            if (player != Player.m_localPlayer) return;

            UpdateInternalCooldowns(Time.deltaTime);
            EnsureAffinities(player);

            // Verifica se está sentado
            if (player.IsSitting())
            {
                Process_Meditation(player);
                return; // Impede castar magias ou trocar afinidade enquanto medita
            }
            else
            {
                // Se levantar, reseta o estado imediatamente
                if (isMeditating)
                {
                    isMeditating = false;
                    meditationTimer = 0f;
                    meditatonFocus = MageAffinity.None;
                    player.Message(MessageHud.MessageType.TopLeft, "Meditation interrupted");
                }
            }

            if (player.IsBlocking())
            {
                if (VL_Utility.Ability3_Input_Down) { SetAffinityFocus(player, MageAffinity.Fire); return; }
                else if (VL_Utility.Ability2_Input_Down) { SetAffinityFocus(player, MageAffinity.Frost); return; }
                else if (VL_Utility.Ability1_Input_Down) { SetAffinityFocus(player, MageAffinity.Arcane); return; }
            }

            MageAffinity currentFocus = GetCurrentFocus(player);
            SyncVisualCooldowns(player, currentFocus);

            if (currentFocus == MageAffinity.Fire) Process_Fire_Input(player, altitude);
            else if (currentFocus == MageAffinity.Frost) Process_Frost_Input(player, altitude);
            else if (currentFocus == MageAffinity.Arcane) Process_Arcane_Input(player);
        }

        // --- SISTEMA DE MEDITAÇÃO ---
// --- SISTEMA DE MEDITAÇÃO ---
        private static void Process_Meditation(Player player)
        {
            // 1. Identifica qual botão está sendo SEGURADO (Pressed)
            MageAffinity currentInput = MageAffinity.None;
            if (VL_Utility.Ability1_Input_Pressed) currentInput = MageAffinity.Arcane;
            else if (VL_Utility.Ability2_Input_Pressed) currentInput = MageAffinity.Frost;
            else if (VL_Utility.Ability3_Input_Pressed) currentInput = MageAffinity.Fire;

            // Se soltou os botões, encerra
            if (currentInput == MageAffinity.None)
            {
                isMeditating = false;
                meditationTimer = 0f;
                meditatonFocus = MageAffinity.None;
                return;
            }

            // Se mudou de botão no meio do caminho, reseta para evitar bugs
            if (isMeditating && currentInput != meditatonFocus)
            {
                isMeditating = false;
                meditationTimer = 0f;
            }

            // 2. INÍCIO (Start): Executa apenas no primeiro frame que começa a segurar
            if (!isMeditating)
            {
                // Verifica Inimigos (Raio 30m)
                List<Character> nearbyChars = new List<Character>();
                Character.GetCharactersInRange(player.transform.position, 30f, nearbyChars);
                foreach (Character c in nearbyChars)
                {
                    if (BaseAI.IsEnemy(player, c) && !c.IsPlayer())
                    {
                        MonsterAI ai = c.GetComponent<MonsterAI>();
                        if (ai != null && ai.IsAlerted())
                        {
                            player.Message(MessageHud.MessageType.Center, "Cannot meditate with enemies nearby!");
                            return; // Bloqueia o início
                        }
                    }
                }

                // Inicia o estado
                isMeditating = true;
                meditatonFocus = currentInput; 
                meditationTimer = 0f;
                player.Message(MessageHud.MessageType.Center, "Meditating...");
                
                // Opcional: Trigger de animação suave (ex: emotes)
                // player.StartEmote("cower"); // Exemplo
            }

            // 3. MANUTENÇÃO (Update): Executa enquanto segura e tem stamina
            if (isMeditating)
            {
                // Consumo de Stamina (10 por segundo)
                float drain = 10f * Time.deltaTime;

                if (player.GetStamina() > drain)
                {
                    player.UseStamina(drain);
                    meditationTimer += Time.deltaTime;

                    // Ciclo de 1 segundo completo
                    if (meditationTimer >= 1.0f) 
                    {
                        // Verifica Inimigos (Raio 30m)
                        List<Character> nearbyChars = new List<Character>();
                        Character.GetCharactersInRange(player.transform.position, 30f, nearbyChars);
                        foreach (Character c in nearbyChars)
                        {
                            if (BaseAI.IsEnemy(player, c) && !c.IsPlayer())
                            {
                                MonsterAI ai = c.GetComponent<MonsterAI>();
                                if (ai != null && ai.IsAlerted())
                                {
                                    // Monstros por perto
                                    player.Message(MessageHud.MessageType.Center, "Cannot focus with enemies nearby!");
                                    isMeditating = false;
                                    meditationTimer = 0f;
                                    return; // Bloqueia o início
                                }
                            }
                        }

                        meditationTimer = 0f; // Reseta timer para o próximo ciclo

                        int hash = 0;
                        string fx = "";
                        string name = "";

                        switch (meditatonFocus)
                        {
                            case MageAffinity.Arcane:
                                hash = Hash_ArcaneAffinity;
                                fx = "fx_VL_ReplicaCreate";
                                name = "Arcane";
                                break;
                            case MageAffinity.Frost:
                                hash = Hash_FrostAffinity;
                                fx = "fx_Potion_frostresist";
                                name = "Frost";
                                break;
                            case MageAffinity.Fire:
                                hash = Hash_FireAffinity;
                                fx = "fx_VL_Flames";
                                name = "Fire";
                                break;
                        }

                        SE_MageAffinityBase affinity = player.GetSEMan().GetStatusEffect(hash) as SE_MageAffinityBase;

                        // Cálculo de cargas máximas
                        float evocationLevel = Class_Mage.GetEvocationLevel(player);
                        int maxCharges = 10 + Mathf.FloorToInt(evocationLevel / 7.5f);
                        if (maxCharges > 30) maxCharges = 30;

                        if (affinity != null)
                        {
                            if (affinity.m_currentCharges < maxCharges)
                            {
                                affinity.AddCharges(1);

                                // Efeitos Visuais e Sonoros
                                if (!string.IsNullOrEmpty(fx))
                                {
                                    GameObject prefab = ZNetScene.instance.GetPrefab(fx);
                                    if (prefab) UnityEngine.Object.Instantiate(prefab, player.transform.position + Vector3.up, UnityEngine.Quaternion.identity);
                                }
                                
                                // Som específico para Arcane (conforme padrão da classe)
                                if (meditatonFocus == MageAffinity.Arcane)
                                {
                                    GameObject prefabSound = ZNetScene.instance.GetPrefab("sfx_Potion_eitr_minor");
                                    if (prefabSound) UnityEngine.Object.Instantiate(prefabSound, player.transform.position, UnityEngine.Quaternion.identity);
                                }

                                player.Message(MessageHud.MessageType.TopLeft, $"Meditated: +1 {name} Charge");
                            }
                            else
                            {
                                player.Message(MessageHud.MessageType.TopLeft, $"{name} Charges Full");
                            }
                        }
                    }
                }
                else
                {
                    // Sem stamina para continuar
                    player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina to meditate");
                    isMeditating = false;
                    meditationTimer = 0f;
                }
            }
        }
        // --- LOGICA CENTRAL DE GELO (SLOW -> FREEZE -> SHATTER) ---
        public static void ApplyFrostProgression(Character attacker, Character victim, HitData hit, float skillLevel, bool canShatter, bool forceShatter, bool forceFreeze)
        {
            // 1. Verifica Imunidade (Cooldown Global de Status)
            int hashImmunity = "SE_VL_FrostImmunity".GetStableHashCode();
            bool isImmune = victim.GetSEMan().HaveStatusEffect(hashImmunity);
            // Debug.Log($"[Mage] Check 0: Immune? {isImmune}"); 
            if (isImmune) return;

            bool appliedEffect = false;

            // Busca efeitos existentes
            StatusEffect seFrozen = victim.GetSEMan().GetStatusEffect(Hash_Frozen);
            StatusEffect seSlow = victim.GetSEMan().GetStatusEffect(Hash_Slow);

            // 2. Tenta SHATTER (Se já tem Frozen)
            if (seFrozen != null)
            {
                // Debug.Log("[Mage] Check 1: Target is Frozen.");
                if (canShatter)
                {
                    bool success = forceShatter || (UnityEngine.Random.value < 0.2f);
                    // Debug.Log($"[Mage] Check 2: Shatter Attempt. Force: {forceShatter}, Success: {success}");

                    if (success)
                    {
                        // Resgata o dano acumulado
                        float accumulatedDmg = GetFrozenDamage(victim);
                        float triggerDmg = hit.GetTotalDamage();

                        // Remove Frozen e Slow (Busca a instância real para remover)
                        // IMPORTANTE: RemoveStatusEffect retorna true se removeu. 
                        // O Patch 'Frozen_Cleanup_Patch' cuidará de limpar o registro de dano.
                        victim.GetSEMan().RemoveStatusEffect(seFrozen);
                        if (seSlow != null) victim.GetSEMan().RemoveStatusEffect(seSlow);

                        // Calcula Dano: (Acumulado + HitAtual) * 1
                        float finalTotal = (accumulatedDmg + triggerDmg) * (1f + (skillLevel / 150f));

                        // Debug.Log($"[Mage] Check 3: Shatter Calc. Acc: {accumulatedDmg}, Trig: {triggerDmg}, Final: {finalTotal}");

                        // Modifica o Hit atual para refletir esse total
                        if (triggerDmg > 0.1f)
                        {
                            float modifier = finalTotal / triggerDmg;
                            hit.ApplyModifier(modifier);
                        }
                        else
                        {
                            hit.m_damage.m_frost = finalTotal;
                        }

                        attacker.Message(MessageHud.MessageType.TopLeft, $"Shattered!");

                        // VFX
                        victim.m_critHitEffects.Create(hit.m_point, UnityEngine.Quaternion.identity, victim.transform);
                        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_DvergerMage_Ice_hit"), hit.m_point, UnityEngine.Quaternion.identity);
                        UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_ice_destroyed"), hit.m_point, UnityEngine.Quaternion.identity);
                        GameObject sfx = ZNetScene.instance.GetPrefab("sfx_ice_destroyed");
                        if (sfx) UnityEngine.Object.Instantiate(sfx, hit.m_point, UnityEngine.Quaternion.identity);

                        appliedEffect = true;
                    }
                }
            }
            // 3. Tenta FREEZE (Se já tem Slow)
            else if (seSlow != null)
            {
                // Debug.Log("[Mage] Check 4: Target is Slowed.");
                bool success = forceFreeze || (UnityEngine.Random.value < (0.1f + skillLevel / 600f));
                if (success)
                {
                    // Remove Slow
                    victim.GetSEMan().RemoveStatusEffect(seSlow);

                    // Adiciona Frozen
                    SE_Frozen newFrozen = (SE_Frozen)ScriptableObject.CreateInstance(typeof(SE_Frozen));
                    newFrozen.name = "SE_VL_Frozen";
                    newFrozen.m_ttl = 6f + 9f * (skillLevel / 150f);
                    victim.GetSEMan().AddStatusEffect(newFrozen, true);

                    // Debug.Log("[Mage] Check 5: Applied Frozen.");
                    UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_DvergerMage_Ice_hit"), hit.m_point, UnityEngine.Quaternion.identity);
                    appliedEffect = true;
                }
            }
            // 4. Aplica SLOW (Se limpo)
            else
            {
                // Debug.Log("[Mage] Check 6: Target is Clean. Applying Slow.");
                SE_Slow newSlow = (SE_Slow)ScriptableObject.CreateInstance(typeof(SE_Slow));
                newSlow.name = "SE_VL_Slow";
                newSlow.m_ttl = 4f + 6f * (skillLevel / 150f);
                newSlow.speedAmount = 0.7f - (skillLevel / 250f);
                victim.GetSEMan().AddStatusEffect(newSlow, true);
                appliedEffect = true;
            }

            // 5. Cooldown de Imunidade
            if (appliedEffect)
            {
                // Debug.Log("[Mage] Check 7: Applied Immunity.");
                StatusEffect immunity = ScriptableObject.CreateInstance<StatusEffect>();
                immunity.name = "SE_VL_FrostImmunity";
                immunity.m_ttl = 1.0f; // 1 segundo de intervalo
                victim.GetSEMan().AddStatusEffect(immunity);
            }
        }

        // --- SISTEMA DE SINCRONIZAÇÃO VISUAL ---
        private static void SyncVisualCooldowns(Player player, MageAffinity focus)
        {
            string ability1_ID = ""; if (focus == MageAffinity.Fire) ability1_ID = "Fireball"; else if (focus == MageAffinity.Frost) ability1_ID = "IceShard"; else if (focus == MageAffinity.Arcane) ability1_ID = "ElementalMastery";
            ApplyVisualCD(player, Hash_CD1, "SE_Ability1_CD", ability1_ID);
            string ability2_ID = ""; if (focus == MageAffinity.Fire) ability2_ID = "FlameNova"; else if (focus == MageAffinity.Frost) ability2_ID = "FrostNova"; else if (focus == MageAffinity.Arcane) ability2_ID = "ArcaneIntellect";
            ApplyVisualCD(player, Hash_CD2, "SE_Ability2_CD", ability2_ID);
            string ability3_ID = ""; if (focus == MageAffinity.Fire) ability3_ID = "Meteor"; else if (focus == MageAffinity.Frost) ability3_ID = "Blizzard"; else if (focus == MageAffinity.Arcane) ability3_ID = "ManaShield";
            ApplyVisualCD(player, Hash_CD3, "SE_Ability3_CD", ability3_ID);
        }

        private static void ApplyVisualCD(Player p, int hashCD, string className, string logicID)
        {
            bool isOnLogic = !string.IsNullOrEmpty(logicID) && IsOnCooldown(logicID);
            bool hasVisual = p.GetSEMan().HaveStatusEffect(hashCD);
            if (isOnLogic)
            {
                float rem = GetRemainingCooldown(logicID);
                if (!hasVisual)
                {
                    Type type = Type.GetType("ValheimLegends." + className);
                    if (type != null)
                    {
                        StatusEffect se = (StatusEffect)ScriptableObject.CreateInstance(type);
                        se.m_ttl = rem;
                        p.GetSEMan().AddStatusEffect(se);
                    }
                }
                else
                {
                    StatusEffect se = p.GetSEMan().GetStatusEffect(hashCD);
                    if (se.m_ttl < rem - 0.5f) se.m_ttl = rem;
                }
            }
            else if (hasVisual)
            {
                StatusEffect se = p.GetSEMan().GetStatusEffect(hashCD);
                if (se != null) p.GetSEMan().RemoveStatusEffect(se);
            }
        }

        // --- FIRE SKILLSET ---
        private static void Process_Fire_Input(Player player, float altitude)
        {
            float level = GetEvocationLevel(player);

            if (VL_Utility.Ability1_Input_Down && !IsOnCooldown("Fireball"))
            {
                SE_MageFireAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
                if (affinity != null && affinity.m_currentCharges >= 1 && player.GetStamina() >= VL_Utility.GetFireballCost)
                {
                    affinity.ConsumeCharges(1);
                    AddCooldown("Fireball", VL_Utility.GetFireballCooldownTime);
                    player.UseStamina(VL_Utility.GetFireballCost);

                    //((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
                    //((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetSpeed(3f);
                    player.StartEmote("cheer");

                    GameObject fx = ZNetScene.instance.GetPrefab("fx_VL_Flames");
                    if (fx) GO_CastFX = UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);

                    SpawnFireball(player, level);
                    player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain);
                }
            }

            if (VL_Utility.Ability2_Input_Down && !IsOnCooldown("FlameNova"))
            {
                SE_MageFireAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
                if (affinity != null && affinity.m_currentCharges >= 3 && player.GetStamina() >= VL_Utility.GetFrostNovaCost)
                {
                    affinity.ConsumeCharges(3);
                    AddCooldown("FlameNova", VL_Utility.GetFrostNovaCooldownTime * 2.0f);
                    player.UseStamina(VL_Utility.GetFrostNovaCost);

                    ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_sledge");
                    ValheimLegends.isChargingDash = true;
                    ValheimLegends.dashCounter = 0;
                    QueuedAttack = MageAttackType.FlameNova;
                }
            }

            Process_Meteor_Logic(player, level);
        }

        private static void Process_Meteor_Logic(Player player, float level)
        {
            void FinishAndCastMeteor()
            {
                meteorCharging = false;
                CastMeteor(player, meteorCount);
                AddCooldown("Meteor", VL_Utility.GetMeteorCooldownTime * (meteorCount));
                meteorCount = 0;
                ValheimLegends.isChanneling = false;
                ValheimLegends.channelingBlocksMovement = true;
                player.RaiseSkill(ValheimLegends.EvocationSkill, meteorSkillGain);
            }

            if (VL_Utility.Ability3_Input_Down && !meteorCharging)
            {
                SE_MageFireAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
                if (!IsOnCooldown("Meteor"))
                {
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        if (player.GetStamina() >= VL_Utility.GetMeteorCost)
                        {
                            ValheimLegends.shouldUseGuardianPower = false;
                            ValheimLegends.isChanneling = true;

                            affinity.ConsumeCharges(1);

                            meteorSkillGain = 0f;
                            player.UseStamina(VL_Utility.GetMeteorCost);
                            ValheimLegends.shouldUseGuardianPower = false;
                            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
                            ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetSpeed(0.5f);

                            GameObject fx = ZNetScene.instance.GetPrefab("fx_VL_Flames");
                            if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);

                            meteorCharging = true;
                            meteorCount = 1;
                            meteorTimer = 0f;
                            meteorSkillGain += VL_Utility.GetMeteorSkillGain;
                        }
                        else player.Message(MessageHud.MessageType.TopLeft, "Not enough stamina");
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 1 Fire Charge");
                }
            }
            else if (VL_Utility.Ability3_Input_Pressed && meteorCharging)
            {
                if (player.GetStamina() <= 1f)
                {
                    FinishAndCastMeteor();
                    return;
                }

                SE_MageFireAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
                if (affinity == null)
                {
                    FinishAndCastMeteor();
                    return;
                }

                int nextTickCost = meteorCount + 1;

                if (affinity.m_currentCharges < nextTickCost)
                {
                    player.Message(MessageHud.MessageType.TopLeft, "Out of Charges!");
                    FinishAndCastMeteor();
                    return;
                }

                player.UseStamina(VL_Utility.GetMeteorCostPerUpdate);
                ValheimLegends.isChanneling = true;
                meteorTimer += Time.deltaTime;

                float chargeInterval = Mathf.Max(0.5f, 8.0f - (level * 0.2f));

                if (meteorTimer >= chargeInterval)
                {
                    affinity.ConsumeCharges(nextTickCost);

                    meteorCount++;
                    meteorTimer = 0f;

                    GameObject fx = ZNetScene.instance.GetPrefab("fx_VL_Flames");
                    if (fx) GO_CastFX = UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);
                    ValheimLegends.shouldUseGuardianPower = false;
                    ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
                    ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetSpeed(0.5f);

                    meteorSkillGain += 0.2f;
                }
            }
            else if (VL_Utility.Ability3_Input_Up && meteorCharging)
            {
                FinishAndCastMeteor();
            }
        }

        // --- FROST SKILLSET ---
        private static void Process_Frost_Input(Player player, float altitude)
        {
            float level = GetEvocationLevel(player);

            if (VL_Utility.Ability1_Input_Down && !IsOnCooldown("IceShard"))
            {
                if (player.GetStamina() >= VL_Utility.GetFireballCost * 0.5f)
                {
                    SE_MageFrostAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
                    if (affinity.m_currentCharges == 0)
                    {
                        player.Message(MessageHud.MessageType.TopLeft, "Need at least 1 Frost Charge");
                        return;
                    }

                    AddCooldown("IceShard", 0.5f);
                    player.UseStamina(VL_Utility.GetFireballCost * 0.25f);
                    player.StartEmote("point");

                    GameObject prefab = ZNetScene.instance.GetPrefab("VL_FrostDagger");
                    if (prefab == null) prefab = ZNetScene.instance.GetPrefab("ice_arrow");

                    if (prefab != null)
                    {
                        UnityEngine.Vector3 vector = player.GetEyePoint() + player.GetLookDir() * 0.2f + player.transform.up * 0.1f + player.transform.right * 0.28f;
                        GameObject gameObject = UnityEngine.Object.Instantiate(prefab, vector, UnityEngine.Quaternion.identity);
                        Projectile component = gameObject.GetComponent<Projectile>();
                        component.name = "IceShard";
                        component.m_respawnItemOnHit = false;
                        component.m_ttl = 2.0f * GetCooldownReduction(player);
                        component.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector));
                        gameObject.transform.localScale = UnityEngine.Vector3.one * 0.8f;

                        RaycastHit hitInfo = default(RaycastHit);
                        UnityEngine.Vector3 target = ((!Physics.Raycast(vector, player.GetLookDir(), out hitInfo, float.PositiveInfinity, ScriptChar_Layermask) || !hitInfo.collider)
                            ? (player.transform.position + player.GetLookDir() * 1000f) : hitInfo.point);

                        HitData hitData2 = new HitData();
                        hitData2.m_damage.m_pierce = UnityEngine.Random.Range(2f + 0.1f * level, 3f + 0.2f * level) * VL_GlobalConfigs.c_mageFrostDagger * VL_GlobalConfigs.g_DamageModifer;
                        hitData2.m_damage.m_frost = UnityEngine.Random.Range(2f + 0.1f * level, 3f + 0.2f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFrostDagger;
                        if (player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery)) AddElementalMasteryDamage(player, ref hitData2, 0.8f);
                        hitData2.m_skill = ValheimLegends.EvocationSkill;
                        hitData2.SetAttacker(player);

                        UnityEngine.Vector3 vector2 = UnityEngine.Vector3.MoveTowards(gameObject.transform.position, target, 1f);
                        component.Setup(player, (vector2 - gameObject.transform.position) * 55f, -1f, hitData2, null, null);
                        Traverse.Create(component).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
                    }
                    player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFireballSkillGain * 0.1f);
                    if (affinity != null) affinity.ConsumeCharges(1);
                }
            }

            if (VL_Utility.Ability2_Input_Down && !IsOnCooldown("FrostNova"))
            {
                SE_MageFrostAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
                if (affinity != null && affinity.m_currentCharges >= 3 && player.GetStamina() >= VL_Utility.GetFrostNovaCost)
                {
                    affinity.ConsumeCharges(3);
                    player.UseStamina(VL_Utility.GetFrostNovaCost);
                    AddCooldown("FrostNova", VL_Utility.GetFrostNovaCooldownTime);
                    ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("swing_axe1");

                    GameObject fx = ZNetScene.instance.GetPrefab("fx_guardstone_activate");
                    if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);
                    if (player.GetSEMan().HaveStatusEffect("Burning".GetStableHashCode()))
                        player.GetSEMan().RemoveStatusEffect("Burning".GetStableHashCode());

                    bool hasElementalMastery = player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery);
                    foreach (Character item in Character.GetAllCharacters())
                    {
                        if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 10f + 0.1f * level &&
                            VL_Utility.LOS_IsValid(item, player.GetCenterPoint(), player.transform.position + player.transform.up * 0.15f))
                        {
                            HitData hitData2 = new HitData();
                            hitData2.m_damage.m_frost = UnityEngine.Random.Range(15f + 1.5f * level, 30f + 2.4f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFrostNova;
                            if (hasElementalMastery) AddElementalMasteryDamage(player, ref hitData2, 1.0f);
                            hitData2.m_pushForce = 20f;
                            hitData2.m_dir = item.transform.position - player.transform.position;
                            hitData2.m_skill = ValheimLegends.EvocationSkill;
                            hitData2.SetAttacker(player);
                            item.Damage(hitData2);

                            // Frost Nova logic is simpler (doesn't trigger full chain, just freeze if slow)
                            if (item.GetSEMan().HaveStatusEffect(Hash_Slow))
                            {
                                item.GetSEMan().RemoveStatusEffect(Hash_Slow);
                                SE_Frozen sE_Frozen = (SE_Frozen)ScriptableObject.CreateInstance(typeof(SE_Frozen));
                                sE_Frozen.name = "SE_VL_Frozen";
                                sE_Frozen.m_ttl = 6f + 9f * (level / 150f);
                                item.GetSEMan().AddStatusEffect(sE_Frozen, true);
                            }
                            else
                            {
                                SE_Slow sE_Slow = (SE_Slow)ScriptableObject.CreateInstance(typeof(SE_Slow));
                                sE_Slow.name = "SE_VL_Slow";
                                sE_Slow.m_ttl = 4f + 6f * (level / 150f);
                                sE_Slow.speedAmount = 0.7f - (level / 250f);
                                item.GetSEMan().AddStatusEffect(sE_Slow, true);
                            }

                            GameObject vfx = ZNetScene.instance.GetPrefab("fx_DvergerMage_Ice_hit");
                            if (vfx) UnityEngine.Object.Instantiate(vfx, hitData2.m_point, UnityEngine.Quaternion.identity);
                        }
                    }
                    player.RaiseSkill(ValheimLegends.EvocationSkill, VL_Utility.GetFrostNovaSkillGain);
                }
            }

            Process_Blizzard_Logic(player, level);
        }

        // --- ARCANE SKILLSET ---
        private static void Process_Arcane_Input(Player player)
        {
            SE_MageArcaneAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;

            void ToggleArcaneBuff(string buffHashName, string buffClassName, string msgName, string vfxPrefab, string cdName)
            {
                int buffHash = buffHashName.GetStableHashCode();
                if (!player.GetSEMan().HaveStatusEffect(buffHash))
                {
                    if (IsOnCooldown(cdName)) return;

                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        Type type = Type.GetType("ValheimLegends." + buffClassName);
                        if (type != null)
                        {
                            StatusEffect se = (StatusEffect)ScriptableObject.CreateInstance(type);
                            if (se != null)
                            {
                                affinity.ConsumeCharges(1);
                                player.GetSEMan().AddStatusEffect(se);
                                GameObject vfx = ZNetScene.instance.GetPrefab(vfxPrefab);
                                if (vfx) UnityEngine.Object.Instantiate(vfx, player.GetCenterPoint(), UnityEngine.Quaternion.identity);
                                ValheimLegends.shouldUseGuardianPower = false;
                                ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
                                ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetSpeed(5f);
                                player.Message(MessageHud.MessageType.TopLeft, $"{msgName}: ON");
                                AddCooldown(cdName, 1f);
                            }
                        }
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 1 Arcane Charge");
                }
                else
                {
                    GameObject vfx = ZNetScene.instance.GetPrefab("vfx_HitSparks");
                    if (vfx) UnityEngine.Object.Instantiate(vfx, player.GetEyePoint(), UnityEngine.Quaternion.LookRotation(Vector3.up));
                    vfx = ZNetScene.instance.GetPrefab("sfx_lootspawn");
                    if (vfx) UnityEngine.Object.Instantiate(vfx, player.GetEyePoint(), UnityEngine.Quaternion.LookRotation(Vector3.up));
                    player.GetSEMan().RemoveStatusEffect(buffHash);
                    player.Message(MessageHud.MessageType.TopLeft, $"{msgName}: OFF");
                    AddCooldown(cdName, 1f);
                }
            }

            if (VL_Utility.Ability1_Input_Down) ToggleArcaneBuff("SE_VL_ElementalMastery", "SE_ElementalMastery", "Elemental Mastery", "fx_guardstone_activate", "ElementalMastery");
            if (VL_Utility.Ability2_Input_Down) ToggleArcaneBuff("SE_VL_ArcaneIntellect", "SE_ArcaneIntellect", "Arcane Intellect", "fx_guardstone_permitted_removed", "ArcaneIntellect");
            if (VL_Utility.Ability3_Input_Down) ToggleArcaneBuff("SE_VL_ManaShield", "SE_ManaShield", "Eitr Shield", "fx_shield_start", "ManaShield");
        }

        // --- BLIZZARD LOGIC ---
        private static void Process_Blizzard_Logic(Player player, float level)
        {
            if (VL_Utility.Ability3_Input_Down && !blizzardCharging)
            {
                if (!IsOnCooldown("Blizzard"))
                {
                    SE_MageFrostAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        ValheimLegends.shouldUseGuardianPower = false;
                        ValheimLegends.isChanneling = true;

                        // Inicialização
                        blizzardCharging = true;
                        blizzardTickCount = 0; // Reseta o contador ao iniciar
                        blizzardChargeTimer = 1.1f; // Garante que o primeiro tick ocorra imediatamente no próximo update
                        blizzardSpawnTimer = 0f;

                        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
                        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetSpeed(0.8f);
                    }
                    else player.Message(MessageHud.MessageType.TopLeft, "Need 1 Frost Charge to start");
                }
            }
            else if (VL_Utility.Ability3_Input_Pressed && blizzardCharging)
            {
                // SAÍDA 1: Sem Stamina
                if (player.GetStamina() <= 5f)
                {
                    blizzardCharging = false;
                    ValheimLegends.isChanneling = false;
                    ValheimLegends.channelingBlocksMovement = true;
                    typeof(Player).GetMethod("StopEmote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(player, null);

                    // Nova Lógica de CD
                    float finalCD = (float)blizzardTickCount * VL_Utility.GetMeteorCooldownTime;
                    AddCooldown("Blizzard", finalCD);
                    return;
                }

                ValheimLegends.isChanneling = true;
                player.UseStamina(10f * Time.deltaTime);

                blizzardSpawnTimer += Time.deltaTime;
                if (blizzardSpawnTimer >= 0.05f)
                {
                    blizzardSpawnTimer = 0f;
                    SpawnBlizzardShard(player, level);
                }

                blizzardChargeTimer += Time.deltaTime;
                if (blizzardChargeTimer >= 1.0f)
                {
                    SE_MageFrostAffinity affinity = player.GetSEMan().GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
                    if (affinity != null && affinity.m_currentCharges >= 1)
                    {
                        ValheimLegends.shouldUseGuardianPower = false;
                        affinity.ConsumeCharges(1);

                        blizzardTickCount++; // Incrementa o contador de ticks

                        blizzardChargeTimer = 0f;
                        ValheimLegends.isChanneling = true;
                        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetTrigger("gpower");
                        ((ZSyncAnimation)typeof(Player).GetField("m_zanim", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player)).SetSpeed(0.8f);
                    }
                    else
                    {
                        // SAÍDA 2: Sem Cargas
                        blizzardCharging = false;
                        ValheimLegends.isChanneling = false;
                        ValheimLegends.channelingBlocksMovement = true;
                        typeof(Player).GetMethod("StopEmote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(player, null);

                        // Nova Lógica de CD
                        float finalCD = (float)blizzardTickCount * VL_Utility.GetMeteorCooldownTime;
                        AddCooldown("Blizzard", finalCD);

                        player.Message(MessageHud.MessageType.TopLeft, "Out of Frost Charges");
                    }
                }
            }
            // SAÍDA 3: Soltou o botão
            else if (blizzardCharging && (VL_Utility.Ability3_Input_Up || player.GetStamina() <= 5f))
            {
                blizzardCharging = false;
                ValheimLegends.isChanneling = false;
                ValheimLegends.channelingBlocksMovement = true;
                typeof(Player).GetMethod("StopEmote", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?.Invoke(player, null);

                // Nova Lógica de CD
                float finalCD = (float)blizzardTickCount * VL_Utility.GetMeteorCooldownTime;
                AddCooldown("Blizzard", finalCD);
            }
        }

        // --- EXECUTE & SPAWNERS ---
        public static void Execute_Attack(Player player)
        {
            bool hasElementalMastery = player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery);
            float level = GetEvocationLevel(player);

            if (QueuedAttack == MageAttackType.FlameNova)
            {
                GameObject fx = ZNetScene.instance.GetPrefab("fx_VL_FlameBurst");
                if (fx) UnityEngine.Object.Instantiate(fx, player.transform.position, UnityEngine.Quaternion.identity);

                foreach (Character item in Character.GetAllCharacters())
                {
                    if (BaseAI.IsEnemy(player, item) && (item.transform.position - player.transform.position).magnitude <= 8f + 0.1f * level &&
                        VL_Utility.LOS_IsValid(item, player.GetCenterPoint(), player.transform.position + player.transform.up * 0.2f))
                    {
                        HitData hitData = new HitData();
                        hitData.m_damage.m_fire = UnityEngine.Random.Range(25f + 2.0f * level, 40f + 6.0f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageInferno;
                        if (hasElementalMastery) AddElementalMasteryDamage(player, ref hitData, 2.0f);
                        hitData.m_skill = ValheimLegends.EvocationSkill;
                        hitData.SetAttacker(player);
                        item.Damage(hitData);
                    }
                }
            }
            QueuedAttack = MageAttackType.None;
        }

        private static void SpawnBlizzardShard(Player player, float level)
        {
            UnityEngine.Vector3 targetPoint;
            RaycastHit hitInfo;
            if (Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, 30f, Script_Layermask)) targetPoint = hitInfo.point;
            else targetPoint = player.GetEyePoint() + player.GetLookDir() * 30f;

            UnityEngine.Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * 6f;
            UnityEngine.Vector3 spawnPos = targetPoint + new UnityEngine.Vector3(randomCircle.x, 20f, randomCircle.y);

            GameObject prefab = ZNetScene.instance.GetPrefab("VL_FrostDagger");
            if (prefab == null) prefab = ZNetScene.instance.GetPrefab("ice_arrow");

            if (prefab != null)
            {
                UnityEngine.Vector2 drift = UnityEngine.Random.insideUnitCircle * 0.4f;
                UnityEngine.Vector3 fallDir = new UnityEngine.Vector3(drift.x, -1f, drift.y).normalized;
                GameObject go = UnityEngine.Object.Instantiate(prefab, spawnPos, UnityEngine.Quaternion.LookRotation(fallDir));
                Projectile p = go.GetComponent<Projectile>();
                p.name = "BlizzardShard";
                p.m_gravity = 0.1f;
                p.m_ttl = 8f;

                HitData hit = new HitData();
                hit.m_damage.m_pierce = UnityEngine.Random.Range(7f + 0.25f * level, 13f + 0.5f * level) * VL_GlobalConfigs.c_mageFrostDagger * VL_GlobalConfigs.g_DamageModifer;
                hit.m_damage.m_frost = UnityEngine.Random.Range(3f + 0.75f * level, 7f + 1.0f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFrostDagger;
                if (player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery)) AddElementalMasteryDamage(player, ref hit, 0.2f);
                hit.SetAttacker(player);
                hit.m_skill = ValheimLegends.EvocationSkill;

                p.Setup(player, fallDir * 6f, -1f, hit, null, null);
            }
        }

        private static void SpawnFireball(Player player, float level)
        {
            UnityEngine.Vector3 vector3 = player.transform.position + player.transform.up * 2.5f + player.GetLookDir() * 0.5f;
            GameObject prefab2 = ZNetScene.instance.GetPrefab("Imp_fireball_projectile");
            GameObject GO_Fireball = UnityEngine.Object.Instantiate(prefab2, new UnityEngine.Vector3(vector3.x, vector3.y, vector3.z), UnityEngine.Quaternion.identity);
            Projectile P_Fireball = GO_Fireball.GetComponent<Projectile>();
            P_Fireball.name = "Fireball";
            P_Fireball.m_respawnItemOnHit = false;
            P_Fireball.m_ttl = 60f;
            P_Fireball.m_gravity = 2.5f;
            P_Fireball.m_rayRadius = 0.1f;
            P_Fireball.m_aoe = 3f + 0.03f * level;
            P_Fireball.transform.localRotation = UnityEngine.Quaternion.LookRotation(player.GetAimDir(vector3));
            GO_Fireball.transform.localScale = UnityEngine.Vector3.zero;
            RaycastHit hitInfo2 = default(RaycastHit);
            UnityEngine.Vector3 position2 = player.transform.position;
            UnityEngine.Vector3 target2 = ((!Physics.Raycast(vector3, player.GetLookDir(), out hitInfo2, float.PositiveInfinity, Script_Layermask) || !hitInfo2.collider)
                ? (position2 + player.GetLookDir() * 1000f) : hitInfo2.point);
            HitData hitData3 = new HitData();
            hitData3.m_damage.m_fire = UnityEngine.Random.Range(5f + 1.6f * level, 10f + 3.8f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFireball;
            hitData3.m_damage.m_blunt = UnityEngine.Random.Range(5f + 0.9f * level, 10f + 2.1f * level) * VL_GlobalConfigs.g_DamageModifer * VL_GlobalConfigs.c_mageFireball;
            hitData3.m_pushForce = 2f;
            hitData3.m_skill = ValheimLegends.EvocationSkill;
            hitData3.SetAttacker(player);
            if (player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery)) AddElementalMasteryDamage(player, ref hitData3, 1.0f);
            UnityEngine.Vector3 vector4 = UnityEngine.Vector3.MoveTowards(GO_Fireball.transform.position, target2, 1f);
            P_Fireball.Setup(player, (vector4 - GO_Fireball.transform.position) * 25f, -1f, hitData3, null, null);
            Traverse.Create(P_Fireball).Field("m_skill").SetValue(ValheimLegends.EvocationSkill);
        }

        private static void CastMeteor(Player player, int count)
        {
            float level = GetEvocationLevel(player);
            System.Random random = new System.Random();
            UnityEngine.Vector3 targetBase = player.transform.position + player.transform.up * 2f + player.GetLookDir() * 10f;
            RaycastHit hitInfo = default(RaycastHit);
            if (Physics.Raycast(player.GetEyePoint(), player.GetLookDir(), out hitInfo, 1000f, ScriptChar_Layermask)) targetBase = hitInfo.point;
            GameObject prefab = ZNetScene.instance.GetPrefab("projectile_meteor");
            bool hasElementalMastery = player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery);
            for (int i = 0; i < count; i++)
            {
                UnityEngine.Vector3 spawnPos = new UnityEngine.Vector3(targetBase.x + (float)random.Next(-8, 8), targetBase.y + 100f, targetBase.z + (float)random.Next(-8, 8));
                GameObject go = UnityEngine.Object.Instantiate(prefab, spawnPos, UnityEngine.Quaternion.identity);
                Projectile p = go.GetComponent<Projectile>();
                p.m_respawnItemOnHit = false;
                p.m_rayRadius = 0.1f;
                p.m_aoe = 8f + 0.03f * level;
                HitData hitData = new HitData();
                hitData.m_damage.m_fire = UnityEngine.Random.Range(8f + 1.5f * level, 30f + 3.0f * level) * VL_GlobalConfigs.g_DamageModifer;
                hitData.m_damage.m_blunt = UnityEngine.Random.Range(20f + 3.0f * level, 60f + 6.0f * level) * VL_GlobalConfigs.g_DamageModifer;
                hitData.SetAttacker(player);
                hitData.m_skill = ValheimLegends.EvocationSkill;
                if (hasElementalMastery) AddElementalMasteryDamage(player, ref hitData, 1.2f / count);
                UnityEngine.Vector3 target = targetBase;
                target.x += random.Next(-8, 8);
                target.z += random.Next(-8, 8);
                p.Setup(player, (target - spawnPos).normalized * 50f, -1f, hitData, null, null);
            }
        }

        public static void ResetCooldowns(Player p)
        {
            // --- 1. Lógica de Cooldowns ---
            InternalCooldowns.Clear();

            p.GetSEMan().RemoveStatusEffect("SE_VL_Ability1_CD".GetStableHashCode());
            p.GetSEMan().RemoveStatusEffect("SE_VL_Ability2_CD".GetStableHashCode());
            p.GetSEMan().RemoveStatusEffect("SE_VL_Ability3_CD".GetStableHashCode());

            // --- 2. Lógica de Recarga de Afinidades ---
            float level = GetEvocationLevel(p);
            int maxRNG = 3 + Mathf.FloorToInt(level / 5f);

            void RestoreAffinity(int hash, string name)
            {
                var affinity = p.GetSEMan().GetStatusEffect(hash) as SE_MageAffinityBase;
                if (affinity != null)
                {
                    int amountToAdd = UnityEngine.Random.Range(1, maxRNG + 1);
                    affinity.AddCharges(amountToAdd);
                }
            }

            RestoreAffinity(Hash_FireAffinity, "Fire");
            RestoreAffinity(Hash_FrostAffinity, "Frost");
            RestoreAffinity(Hash_ArcaneAffinity, "Arcane");

            p.Message(MessageHud.MessageType.Center, "Surge: Cooldowns & Charges Restored!");
        }

        private static void AddElementalMasteryDamage(Player player, ref HitData hitData, float factor)
        {
            if (player.GetSEMan().HaveStatusEffect(Hash_ElementalMastery))
            {
                float level = 0f;
                level = GetEvocationLevel(player);
                float masteryMult = 0.5f + (level * 0.01f);
                masteryMult = Mathf.Clamp(masteryMult, 0.5f, 2.0f);
                float finalFactor = factor * masteryMult;
                HitData.DamageTypes weaponDamage = player.GetCurrentWeapon().GetDamage();
                hitData.m_damage.m_fire += weaponDamage.m_fire * finalFactor;
                hitData.m_damage.m_frost += weaponDamage.m_frost * finalFactor;
                hitData.m_damage.m_lightning += weaponDamage.m_lightning * finalFactor;
                hitData.m_damage.m_poison += weaponDamage.m_poison * finalFactor;
                hitData.m_damage.m_spirit += weaponDamage.m_spirit * finalFactor;
            }
        }

        public static float GetEvocationLevel(Player player)
        {
            return player.GetSkills().GetSkillList().FirstOrDefault((Skills.Skill x) => x.m_info == ValheimLegends.EvocationSkillDef).m_level
                  * (1f + Mathf.Clamp((EpicMMOSystem.LevelSystem.Instance.getAddCriticalChance() / 40f) + (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f), 0f, 0.5f));
        }

        public static float GetCooldownReduction(Player player)
        {
            return (1f - (EpicMMOSystem.LevelSystem.Instance.getAddMagicDamage() / 80f));
        }

        private static void EnsureAffinities(Player player)
        {
            var seman = player.GetSEMan();
            if (!seman.HaveStatusEffect(Hash_FireAffinity)) seman.AddStatusEffect(ScriptableObject.CreateInstance<SE_MageFireAffinity>());
            if (!seman.HaveStatusEffect(Hash_FrostAffinity)) seman.AddStatusEffect(ScriptableObject.CreateInstance<SE_MageFrostAffinity>());
            if (!seman.HaveStatusEffect(Hash_ArcaneAffinity)) seman.AddStatusEffect(ScriptableObject.CreateInstance<SE_MageArcaneAffinity>());

            if (GetCurrentFocus(player) == MageAffinity.None)
            {
                var arcane = seman.GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;
                if (arcane != null) arcane.SetFocus(true);
            }
            ValheimLegends.NameCooldowns();
            if (ValheimLegends.abilitiesStatus != null)
            {
                foreach (RectTransform item2 in ValheimLegends.abilitiesStatus) { if (item2 != null && item2.gameObject != null) UnityEngine.Object.Destroy(item2.gameObject); }
                ValheimLegends.abilitiesStatus.Clear();
            }
        }

        private static void SetAffinityFocus(Player player, MageAffinity target)
        {
            var seman = player.GetSEMan();
            var fire = seman.GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
            var frost = seman.GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
            var arcane = seman.GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;

            SE_MageAffinityBase targetAffinity = null;
            string fxPrefabName = "";
            switch (target)
            {
                case MageAffinity.Fire: targetAffinity = fire; fxPrefabName = "fx_VL_Flames"; break;
                case MageAffinity.Frost: targetAffinity = frost; fxPrefabName = "fx_Potion_frostresist"; break;
                case MageAffinity.Arcane: targetAffinity = arcane; fxPrefabName = "vfx_Potion_eitr_minor"; break;
            }

            if (targetAffinity != null && targetAffinity.isFocused)
            {
                float evocationLevel = Class_Mage.GetEvocationLevel(player);
                int maxCharges = 10 + Mathf.FloorToInt(evocationLevel / 7.5f);
                if (maxCharges > 30) maxCharges = 30;
                if (targetAffinity.m_currentCharges >= maxCharges)
                {
                    player.Message(MessageHud.MessageType.TopLeft, $"Charges are already full. ({targetAffinity.m_currentCharges}/{maxCharges})");
                    return;
                }

                float cost = Mathf.Max(50f, player.GetMaxStamina() * 0.8f);
                if (player.GetStamina() >= cost)
                {
                    player.UseStamina(cost);
                    targetAffinity.AddCharges(1);

                    if (!string.IsNullOrEmpty(fxPrefabName))
                    {
                        if (target == MageAffinity.Arcane)
                        {
                            GameObject prefab = ZNetScene.instance.GetPrefab(fxPrefabName);
                            if (prefab) UnityEngine.Object.Instantiate(prefab, player.transform.position, UnityEngine.Quaternion.identity);
                            UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_staff_lightning_charge"), player.GetEyePoint(), UnityEngine.Quaternion.identity);
                        } else { 
                            GameObject prefab = ZNetScene.instance.GetPrefab(fxPrefabName);
                            if (prefab) UnityEngine.Object.Instantiate(prefab, player.transform.position, UnityEngine.Quaternion.identity);
                        }
                    }
                }
                else player.Message(MessageHud.MessageType.TopLeft, $"Not enough stamina to recharge ({player.GetStamina():0}/{cost:0})");
                return;
            }

            if (fire == null || frost == null || arcane == null) return;
            if ((target == MageAffinity.Fire && fire.isFocused) || (target == MageAffinity.Frost && frost.isFocused) || (target == MageAffinity.Arcane && arcane.isFocused)) return;
            fire.SetFocus(target == MageAffinity.Fire);
            frost.SetFocus(target == MageAffinity.Frost);
            arcane.SetFocus(target == MageAffinity.Arcane);
            ValheimLegends.NameCooldowns();
            if (ValheimLegends.abilitiesStatus != null)
            {
                foreach (RectTransform item2 in ValheimLegends.abilitiesStatus) { if (item2 != null && item2.gameObject != null) UnityEngine.Object.Destroy(item2.gameObject); }
                ValheimLegends.abilitiesStatus.Clear();
            }
            string msg = "";
            switch (target)
            {
                case MageAffinity.Fire:
                    UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("vfx_FireAddFuel"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
                    UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_FireAddFuel"), player.GetCenterPoint(), UnityEngine.Quaternion.identity);
                    msg = "<color=#FF8000>🔥</color>"; 
                    break;
                case MageAffinity.Frost:
                    UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_iceshard_launch"), player.GetCenterPoint(), UnityEngine.Quaternion.LookRotation(Vector3.up));
                    msg = "<color=#00FFFF>❄</color>"; 
                    break;
                case MageAffinity.Arcane:
                    UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("fx_VL_ReplicaCreate"), player.GetEyePoint(), UnityEngine.Quaternion.identity);
                    UnityEngine.Object.Instantiate(ZNetScene.instance.GetPrefab("sfx_staff_lightning_charge"), player.GetEyePoint(), UnityEngine.Quaternion.identity);
                    msg = "<color=#9966CC>🪄</color>"; 
                    break;
            }
            player.Message(MessageHud.MessageType.Center, msg);

        }

        public static MageAffinity GetCurrentFocus(Player player)
        {
            var seman = player.GetSEMan();
            var fire = seman.GetStatusEffect(Hash_FireAffinity) as SE_MageFireAffinity;
            if (fire != null && fire.isFocused) return MageAffinity.Fire;
            var frost = seman.GetStatusEffect(Hash_FrostAffinity) as SE_MageFrostAffinity;
            if (frost != null && frost.isFocused) return MageAffinity.Frost;
            var arcane = seman.GetStatusEffect(Hash_ArcaneAffinity) as SE_MageArcaneAffinity;
            if (arcane != null && arcane.isFocused) return MageAffinity.Arcane;
            return MageAffinity.None;
        }
    }
}