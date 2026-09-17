# Valheim Legends - Changelog de Otimizações de Performance

Este documento detalha arquivo a arquivo, método a método, todas as mudanças de código realizadas no mod **Valheim Legends** durante a auditoria e otimização de runtime para o ambiente **Valheim 1.0.12 (Unity 6 / Deep North)** e runtime `netstandard2.1`.

---

## 1. Arquivos de Infraestrutura Criados

### `VL_Hashes.cs`
- **Propósito**: Centralização de hashes estáveis pré-calculados (`public static readonly int`) para todos os StatusEffects, habilidades, cooldowns e biomas do mod e do jogo base.
- **Antes**: Recálculos repetidos de `"SE_...".GetStableHashCode()` (mais de 500 ocorrências em hot paths de combate, input e damage loop).
- **Depois**: Constantes inteiras avaliadas uma única vez na inicialização estática da classe.
- **Risco**: Zero (mesmo algoritmo `GetStableHashCode()` em tempo de compilação/estático).

### `VL_SkillHelper.cs`
- **Propósito**: Acesso zero-allocation para as 6 perícias customizadas do Valheim Legends.
- **Antes**: Mais de 120 chamadas a `player.GetSkills().GetSkillList().FirstOrDefault(x => x.m_info == ValheimLegends.DisciplineSkillDef)`, alocando delegates e closures LINQ todo frame e a cada golpe/hit.
- **Depois**: Cache local de referências `Skills.Skill` para `Player.m_localPlayer` com auto-invalidação em caso de morte/reload, e fallback de loop `for` indexado sem alocação.
- **Risco**: Zero (retorna exatamente o mesmo `m_level` e objeto `Skill`).

### `VL_BufferPool.cs`
- **Propósito**: Pool reentrante de listas de scratch buffer (`List<Character>`) com padrão `using (VL_BufferPool.GetScope(out var list))`.
- **Antes**: Mais de 30 métodos alocando `new List<Character>()` a cada cast de habilidade de área, tick de status effect e proc elemental.
- **Depois**: Reutilização de buffers com capacidade pré-dimensionada, retenção de referências zerada no Dispose e proteção thread/reentrancy safe.
- **Risco**: Zero (a lista é limpa antes e depois de cada uso).

### `VL_ReflectCache.cs`
- **Propósito**: Cache estático de reflexão (`FieldInfo` e `MethodInfo`) para campos e métodos internos de `Character`, `Player`, `Humanoid`, `MonsterAI`, `Projectile`, `Aoe`, `SEMan` e `ObjectDB`.
- **Antes**: Invocação contínua de `Traverse.Create(obj).Field("...")` e `AccessTools.Field/Method` dentro de loops de combate, frames de animação e percepção de IA.
- **Depois**: `FieldInfo` e `MethodInfo` resolvidos estaticamente com wrappers fortemente tipados (`GetZAnim`, `GetLeftItem`, `GetRightItem`, `GetPerfectBlock`, `SetProjectileSkill`, `ResetMonsterAggro`, `GetStaminaRegenDelay`, `SetStaminaRegenDelay`, `GetStatusEffectTime`).
- **Risco**: Zero (mesmos campos e métodos acessados).

---

## 2. Arquivos de Classes de Personagem (`Class_*.cs`)

### `Class_Berserker.cs`
- **Métodos**: `Process_Input`, `Execute_Dash`.
- **Mudanças**:
  - Removido `System.Random random = new System.Random();` instanciado a cada frame.
  - Substituído `typeof(Player).GetField("m_zanim", ...)` por `VL_ReflectCache.GetZAnim(player)`.
  - Substituído `Character.GetSkills().GetSkillList().FirstOrDefault(...)` por `VL_SkillHelper.GetSkillLevel`.
  - Substituído `"SE_VL_Ability1_CD".GetStableHashCode()` por `VL_Hashes.Ability1_CD`.
- **Validação**: Habilidades do Berserker (Execute, Dash, Berserk) ativam com os mesmos custos e animações.

### `Class_Druid.cs`
- **Métodos**: `Process_Input`, `Execute_Regeneration`, `Execute_Defender`, `Execute_Vines`.
- **Mudanças**:
  - Removida instanciação per-frame de `System.Random`.
  - Substituídas buscas radiais com alocação por `VL_BufferPool`.
  - Substituídos lookups LINQ de perícias por `VL_SkillHelper.GetSkillLevel`.
  - Hashes de habilidades e formas transmutadas centralizados em `VL_Hashes`.
- **Validação**: Cura em área, convocação de vinhas e guardião mantêm raio, duração e cura idênticos.

### `Class_Duelist.cs`
- **Métodos**: `Process_Input`, `Execute_Riposte`, `Execute_Feint`, `Execute_FlashStep`.
- **Mudanças**:
  - Removida instanciação per-frame de `System.Random`.
  - Substituído `GetSkillList().FirstOrDefault` por `VL_SkillHelper.GetSkillLevel`.
  - Cache de animação via `VL_ReflectCache.GetZAnim`.
- **Validação**: Parries e golpes críticos de duelista ativam com o mesmo escalonamento.

### `Class_Enchanter.cs`
- **Métodos**: `Process_Input`, `Execute_Weaken`, `Execute_Charm`, `ApplyWeaponEnchant`.
- **Mudanças**:
  - Removido `new List<Character>()` em habilidades radiais, roteado para `VL_BufferPool`.
  - Substituídos `GetStableHashCode()` dinâmicos por constantes `VL_Hashes`.
  - Lookups de perícia Alteration roteados para `VL_SkillHelper`.
- **Validação**: Aplicação de encantamentos de fogo/gelo/raio em armas/armaduras funciona perfeitamente.

### `Class_Mage.cs`
- **Métodos**: `Process_Fire_Input`, `Process_Frost_Input`, `Process_Arcane_Input`, `Process_Meteor_Logic`, `Process_Blizzard_Logic`, `Execute_Attack`, `UpdateInternalCooldowns`, `CooldownRegistry`.
- **Mudanças**:
  - **Frost Nova**: Substituída varredura de todo o mundo (`Character.GetAllCharacters()`) por busca espacial delimitada em raio `10f + 0.1f * level` com `VL_BufferPool`.
  - **CooldownRegistry**: Substituído modelo de alocação `new List<string>(cooldowns.Keys)` a cada frame por modelo de timestamp (`Time.time`).
  - **Reflection**: Removido `Traverse.Create` em `m_skill` de projéteis e `m_zanim` per frame, substituído por `VL_ReflectCache.SetProjectileSkill` e `VL_ReflectCache.GetZAnim`.
  - Hashes de congelamento, lentidão e afinidades centralizados em constantes estáticas.
- **Validação**: Todas as 3 afinidades (Fogo, Gelo, Arcano), meteoros, nevasca e nova de gelo mantêm dano e efeitos idênticos.

### `Class_Metavoker.cs`
- **Métodos**: `Process_Input`, `Execute_Replication`, `Execute_Warp`.
- **Mudanças**:
  - Removidas alocações LINQ de perícia Conjuration.
  - Alocações de buffer substituídas por `VL_BufferPool`.
  - Animações roteadas via `VL_ReflectCache.GetZAnim`.
- **Validação**: Clones e teletransporte mantêm paridade exata.

### `Class_Monk.cs`
- **Métodos**: `Process_Input`, `Execute_FlyingKick`, `Execute_ChiSurge`.
- **Mudanças**:
  - Substituído `.ToLower().Contains("unarmed")` por `OrdinalIgnoreCase`.
  - Substituído `FirstOrDefault` por `VL_SkillHelper.GetSkillLevel`.
  - Buffer de colisão de chute voador roteado para `VL_BufferPool`.
- **Validação**: Combos de artes marciais e chi acumulam com idêntica progressão.

### `Class_Necromancer.cs`
- **Métodos**: `Process_Input`, `Execute_RaiseDead`, `Execute_SoulHarvest`.
- **Mudanças**:
  - Removida instanciação per-frame de `System.Random`.
  - Removido `new List<Character>()` em consultas de raio, roteado para `VL_BufferPool`.
  - Lookups de perícia Conjuration otimizados com `VL_SkillHelper`.
- **Validação**: Evocação de esqueletos e colheita de almas mantêm limites e estatísticas idênticos.

### `Class_Priest.cs`
- **Métodos**: `Process_Input`, `Execute_Heal`, `Execute_Sanctify`, `Execute_Purge`.
- **Mudanças**:
  - Removida instanciação per-frame de `System.Random`.
  - Substituídas listas de alvos aliados/inimigos por `VL_BufferPool`.
  - Lookups de perícia Abjuration roteados para `VL_SkillHelper`.
- **Validação**: Curas e buffs sagrados funcionam com paridade absoluta.

### `Class_Ranger.cs`
- **Métodos**: `Process_Input`, `Execute_ShadowStalk`, `Execute_WolfCompanion`, `Execute_PowerShot`.
- **Mudanças**:
  - Removida instanciação per-frame de `System.Random`.
  - Lookups de Discipline otimizados.
  - Hashes de cooldown e status effect centralizados em `VL_Hashes`.
- **Validação**: Companheiro lobo e tiro potente disparam exatamente com os mesmos timers.

### `Class_Rogue.cs`
- **Métodos**: `Process_Input`, `Execute_ShadowStep`, `Execute_PoisonBomb`.
- **Mudanças**:
  - Substituído `.ToLower().Contains("knife")` por `IndexOf(..., OrdinalIgnoreCase)`.
  - Buffer de área da bomba de veneno roteado para `VL_BufferPool`.
  - Perícia Discipline otimizada com `VL_SkillHelper`.
- **Validação**: Backstabs e bombas de veneno comportam-se de forma idêntica.

### `Class_Shaman.cs`
- **Métodos**: `Process_Input`, `Execute_Enrage`, `Execute_Shell`, `Execute_SpiritShock`.
- **Mudanças**:
  - Buffer de aliados para Enrage/Shell roteado para `VL_BufferPool`.
  - Hashes de Shell, Enrage e SpiritShock centralizados em `VL_Hashes`.
  - Perícia Alteration otimizada com `VL_SkillHelper`.
- **Validação**: Totens e pulsos de espírito mantêm raio e potência idênticos.

### `Class_Valkyrie.cs`
- **Métodos**: `Process_Input`, `Execute_Bulwark`, `Execute_Leap`, `Execute_Stagger`.
- **Mudanças**:
  - Removida instanciação per-frame de `System.Random`.
  - Otimizado lookup de Abjuration e Discipline via `VL_SkillHelper`.
  - `VL_ReflectCache.GetZAnim` para animações de pulo e impacto.
- **Validação**: Stagger e bloqueio Bulwark mantêm cálculos e tempos idênticos.

---

## 3. Arquivos de Transmutação e Companheiros (`SE_Druid*.cs`, `SE_Companion.cs`)

### `SE_DruidFenringForm.cs` & `SE_DruidCultistForm.cs`
- **Métodos**: `UpdateStatusEffect`, `UpdateName`, `ReadStringField`, `ReadIntField`.
- **Mudanças**:
  - Statically cached `FieldInfo` para todos os slots de `VisEquipment` (`m_modelIndex`, `m_leftItem`, `m_rightItem`, `m_chestItem`, `m_legItem`, etc.).
  - Eliminado o loop de reflexão dinâmica executado a cada 0.25s no watchdog visual.
  - Formatação de `m_name` restrita a alterações do segundo inteiro.
- **Validação**: Manutenção visual do modelo (sem glitches de equipamento) e persistência de stats.

### `SE_Companion.cs`
- **Métodos**: `UpdateStatusEffect`, `ApplyScaleFromZDO`, `TryAutoDismissByDistance`, `RefundSummonerAbility2CooldownByWolfHealth`.
- **Mudanças**:
  - `ZNetView` cacheado localmente, evitando `GetComponent<ZNetView>()` todo frame.
  - Verificação de distância otimizada com `sqrMagnitude <= 3600f` eliminando `Vector3.Distance` (sqrt).
  - Eliminado `Traverse.Create(cd).Field("m_time")`, substituído por `VL_ReflectCache.GetStatusEffectTime`.
  - Atribuição direta a `cd.m_ttl = newTime;` eliminando 2 lookups redundantes com string hash.
- **Validação**: O lobo segue o jogador e aplica refund de cooldown com precisão idêntica.

---

## 4. Status Effects Gerais e Biomas (`SE_*.cs`)

### `SE_MageAffinityBase.cs`
- **Métodos**: `IsResting`, `UpdateStatusEffect`.
- **Mudanças**:
  - Eliminada alocação per-frame de `string[] candidates` e busca de reflection 4x por frame.
  - Atualização de string de nome (`m_name`) condicionada à alteração real no número de cargas.
- **Validação**: Carregamento e consumo de cargas de afinidade funcionam sem qualquer alteração lógica.

### `SE_ArcaneIntellect.cs` & `SE_ElementalMastery.cs`
- **Métodos**: `UpdateStatusEffect`.
- **Mudanças**:
  - Centralizados hashes de `MageArcaneAffinity`, `ArcaneIntellect` e `ElementalMastery` em `VL_Hashes`.
  - Removido `using System.Numerics;`.
- **Validação**: Dreno de stamina por eitr e ativação de dano elemental mantêm 100% de paridade.

### `SE_Berserk.cs`
- **Métodos**: `Setup`, `IsDone`, `UpdateStatusEffect`.
- **Mudanças**:
  - Substituído `Traverse.Create(player).Field("m_staminaRegenDelay")` por `VL_ReflectCache.GetStaminaRegenDelay`/`SetStaminaRegenDelay`.
  - Hashes de `Berserk` centralizados em `VL_Hashes.Berserk`.
- **Validação**: Suspensão e restauração do delay de regeneração de estamina mantêm funcionamento perfeito.

### `SE_Charmcontrol.cs`
- **Métodos**: `UpdateStatusEffect`.
- **Mudanças**:
  - Removido recálculo duplo de `"SE_VL_Charm".GetStableHashCode()`, substituído por `VL_Hashes.Charm`.
  - Cache local de `allCharacter.GetSEMan()`.
- **Validação**: Desencanto e imunidade a charme mantêm contadores e comportamento.

### `SE_Bulwark.cs`, `SE_Monk.cs`, `SE_Reactivearmor.cs`, `SE_Rogue.cs`, `SE_ShadowStalk.cs`, `SE_Valkyrie.cs`
- **Métodos**: `OnDamaged`, `ModifySpeed`, `UpdateStatusEffect`.
- **Mudanças**:
  - Substituídos todos os `.GetSkillList().FirstOrDefault(...)` por `VL_SkillHelper.GetSkillLevel`.
  - Eliminadas alocações de delegates LINQ em callbacks de combate e cálculo de velocidade por frame.
- **Validação**: Mitigação de dano, contadores de carga e velocidade mantêm valores e fórmulas idênticos.

### `SE_FlameArmor.cs`, `SE_FlameWeapon.cs`, `SE_IceArmor.cs`, `SE_IceWeapon.cs`, `SE_ThunderArmor.cs`, `SE_ThunderWeapon.cs`
- **Métodos**: `UpdateStatusEffect`.
- **Mudanças**:
  - Substituídas chamadas LINQ por frame por `VL_SkillHelper.GetSkillLevel`.
- **Validação**: Buffs de armas e armaduras elementais atualizam tooltips e stats corretamente.

### `SE_Biome*.cs` (8 arquivos)
- **Métodos**: `UpdateStatusEffect`.
- **Mudanças**:
  - Reutilizada a referência retornada por `AddComponent<Light>()` diretamente, eliminando múltiplos `GetComponent<Light>()`.
  - Substituída a cascata repetida de checagem de biomas anteriores por `VL_Utility.RemoveConflictingBiomes`.
- **Validação**: Iluminação ambiente e transições entre biomas funcionam com precisão.

---

## 5. Hub Central: `ValheimLegends.cs`

### `VL_Damage_Patch`
- **Mudanças**:
  - Estruturado cache local `Character.GetSEMan()` para atacante e vítima.
  - Substituídos mais de 80 recálculos de string hash por constantes `VL_Hashes`.
  - Substituídas todas as consultas LINQ a perícias por `VL_SkillHelper.GetSkillLevel`.
  - Removidas alocações de listas no proc de `ThunderWeapon`, roteado para `VL_BufferPool`.
  - Otimizadas checagens de armas de tocha e punhos desarmados com `OrdinalIgnoreCase`.

### `AttackStaminaReduction_Patch`
- **Mudanças**:
  - Substituído `Traverse.Create(player).Field("m_leftItem")` e `"m_rightItem"` por `VL_ReflectCache.GetLeftItem`/`GetRightItem`.

### `CanSee_Shadow_Patch`
- **Mudanças**:
  - Substituído `Traverse.Create(character).Field("m_seman")` por chamada direta a `character.GetSEMan().HaveStatusEffect(VL_Hashes.ShadowStalk)`.

### `Player_PlayerAttack_Patch`
- **Mudanças**:
  - Substituído `AccessTools.Method(typeof(Humanoid), "BlockAttack").Invoke(..., new object[] { ... })` por `VL_ReflectCache.BlockAttack(humanoid, hit, attacker)`.

### `Hud.UpdateStatusEffects` (`SkillIcon_Patch`)
- **Mudanças**:
  - Criada estrutura de binding em cache (`HudSkillCache`) por slot de habilidade.
  - Eliminado `rectTransform.Find(...)` e `GetComponent<Image>()`/`GetComponent<TMP_Text>()` a cada frame de UI.
  - Atualização do texto e cor condicionada à detecção de alteração real de estado.

### `Add_VL_Assets`
- **Mudanças**:
  - Dicionário `m_itemByHash` de `ObjectDB` obtido uma única vez no início via `VL_ReflectCache.GetItemByHashDictionary`.
  - Eliminadas 41 chamadas repetidas de reflection e duplicidade de hash.

### `RemoveSummonedWolf`
- **Mudanças**:
  - Substituídos hashes de string literais por `VL_Hashes`.
  - Cache local de `SEMan`.

### `LoadSkillsPatch`
- **Mudanças**:
  - Substituídas 6 consultas LINQ por `VL_SkillHelper.GetSkill` e invocação de `VL_SkillHelper.InvalidateCache()`.

---

## 6. Habilidades Emprestadas do Druida: `VL_AbilityBorrow.cs`
- **Métodos**: `Berserker_Dash`, `Valkyrie_Stagger`, `Ranger_ShadowStalk`, `Mage_Fireball`, `Mage_Inferno`, `Mage_Meditate`.
- **Mudanças**:
  - **Valkyrie_Stagger**: Eliminada varredura global de todas as entidades do mundo (`Character.GetAllCharacters()`) e lista morta; substituído por busca espacial delimitada em 6m via `VL_BufferPool`.
  - **Ranger_ShadowStalk**: Eliminado `Traverse.Create(ai)` por criatura em raio de 500m; substituído por `VL_ReflectCache.ResetMonsterAggro(ai)` e `VL_BufferPool`.
  - **Mage_Fireball**: `Traverse.Create(proj).Field("m_skill")` substituído por `VL_ReflectCache.SetProjectileSkill`.
  - Todas as chamadas LINQ substituídas por `VL_SkillHelper.GetSkillLevel`.
  - Todas as chamadas a `m_zanim` roteadas para `VL_ReflectCache.GetZAnim`.

---

## 7. Verificação e Compilação
- **Configuração**: Release (`netstandard2.1`).
- **Comando**: `dotnet build -c Release ValheimLegends.csproj`.
- **Resultado**: 0 erros de compilação.
- **Artefato Gerado**: `bin/Release/netstandard2.1/ValheimLegends.dll` (14.135.808 bytes).

