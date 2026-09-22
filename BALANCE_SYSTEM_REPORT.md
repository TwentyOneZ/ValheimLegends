# Migração do balanceamento das habilidades

## A. Resumo

As habilidades ofensivas nomeadas usam potência de referência (`RP = 4 + 0,9 × nível`) ou dano da arma, com skill e atributo secundário na escala 0,75–1,25. Cura usa `BaseHeal × √RP × Body × skill`. Cooldown usa INT bruto e custo usa Agility bruta, ambos com redução máxima de 50%. A mecânica de cada habilidade, seus projéteis, intervalos e durações foram mantidos, salvo o transporte de potência calculada pelo caster para efeitos e summons. As alterações são apenas neste repositório; o perfil Perspex não foi modificado.

## B. Arquitetura

`ValheimLegends/VL_BalanceMath.cs` contém a matemática pura e testável: `ReferencePower`, `Progression`, `Magic`, `Physical`, `Heal`, `Cooldown` e `Cost`. `VL_Utility` adapta essas operações a `Player`, EpicMMO, `HitData.DamageTypes` e configs. Os parâmetros de atributo secundário são `Body` para Endurance, `Agility` para Agility, `Vigour` e `Special` para os outros secundários. Os pontos de damage usam a skill bruta, não a antiga skill inflada por atributos.

Para magia, o helper inclui uma vez `1 + getAddMagicDamage()/100`. Para físico, `GetCurrentWeapon().GetDamage()` fornece os tipos da arma, já modificados pelo EpicMMO se o item pertence ao inventário; só mãos vazias recebem o STR explicitamente. `g_DamageModifer` e o multiplicador da habilidade entram uma vez. Efeitos posteriores que apenas governam raio, quantidade, duração, chance, stagger ou ganho de skill ainda podem usar seus cálculos antigos: não são a camada de dano/cura.

## C. Abilities ofensivas

Coeficientes são por pacote indicado; nos tipos compostos a soma é o total, exceto Sanctify. As classes e métodos correspondem aos arquivos `ValheimLegends/Class_<classe>.cs`; buffs de ataque também passam pelo hook em `ValheimLegends.cs`.

| Classe | Ability | Base | School | Secondary | Coef. | Regra especial / local |
|---|---|---|---|---|---:|---|
| Berserker | Dash | arma | Discipline | — | 1,00 | AoE; `Class_Berserker`, Execute_Dash |
| Berserker | Berserk | ataque existente | — | — | 1,15 | multiplicador, não hit novo; hook `ValheimLegends` |
| Berserker | Execute | arma | Discipline | Special | 1,25 | somente hits de arma, preserva tipos da arma; ×2 se HP alvo <20%; `Class_Berserker`, hook e `ExecuteThreshold_Patch` |
| Druid | Roots/Vines | RP | Conjuration | Body | 0,35 | por root; `Class_Druid`, Execute_Vines |
| Druid | Root Defender | RP | Conjuration | Body | 0,40 | por ataque, snapshot no ZDO; `Class_Druid`, Execute_Defender |
| Druid | Drusquito | RP | Conjuration | Body | 0,22 | por ataque, snapshot no ZDO; `Class_Druid`, Execute_Defender |
| Duelist | Seismic Slash | arma | Discipline | — | 0,90 | `Class_Duelist`, Execute_SeismicSlash |
| Duelist | Riposte | arma | Discipline | Special | 1,10 | counter continua dependente de ativação, não do dano recebido; `Class_Duelist` |
| Duelist | Quick/Coin Shot | arma | Discipline | Agility | 0,70 | `Class_Duelist`, Execute_QuickShot |
| Enchanter | Biome Shock | RP | Alteration | Special | 0,80 | fator do tempo restante do buff limitado a 1–2×; `Class_Enchanter` |
| Mage | Fireball | RP | Evocation | — | 0,80 | Fire/Blunt, total dividido; `Class_Mage` e borrowed `VL_AbilityBorrow` |
| Mage | Flame Nova | RP | Evocation | — | 1,20 | por alvo; `Class_Mage` |
| Mage | Meteor | RP | Evocation | — | 2,00 | por meteor, Fire/Blunt dividido; `Class_Mage`, também caminho borrowed |
| Mage | Ice Shard | RP | Evocation | Agility | 0,40 | Pierce/Frost dividido, ×3 contra Frozen; `Class_Mage` e hook |
| Mage | Frost Nova | RP | Evocation | Vigour | 0,45 | `Class_Mage` |
| Mage | Blizzard | RP | Evocation | Body | 0,10 | por shard, Pierce/Frost dividido; `Class_Mage` |
| Mage | Elemental Mastery | elemento da arma | — | — | 0,25 | adicional elemental, sem RP; hook `ValheimLegends` |
| Metavoker | Warp | RP | Evocation | Agility | 0,55 | fator de distância adicional limitado a 1–2×; `Class_Metavoker` |
| Metavoker | Replica | RP | Illusion | Body | 0,25 | por ataque/summon, snapshot; `Class_Metavoker` |
| Metavoker | Light/Release | RP | Illusion | — | 0,65 | Lightning/Pierce total dividido; `Class_Metavoker` |
| Metavoker | Force Wave | RP | Evocation | Vigour | 0,35 | dano direto; refletidos mantêm dano próprio; `Class_Metavoker` |
| Monk | Chi/Meteor Punch | arma/unarmed | Discipline | Special | 1,35 | `Class_Monk` |
| Monk | Chi Slam/Impact | arma/unarmed | Discipline | Vigour | 1,00 | altitude adicional limitada a 2×; `Class_Monk` |
| Monk | Psi Bolt/Chi Blast | arma/unarmed | Discipline | Special | 0,30/Chi | `Class_Monk` |
| Monk | Flying Kick, contato | arma/unarmed | Discipline | Agility | 0,22 | por contato; `Class_Monk` |
| Monk | Flying Kick, final | arma/unarmed | Discipline | Agility | 0,55 | `Class_Monk` |
| Priest | Purge, dano | RP | Evocation | Body | 0,60 | Spirit/Fire total dividido; `Class_Priest` |
| Priest | Sanctify | RP | Evocation | Body | 1,10+1,10 | Fire/Blunt total 1,10 e Spirit adicional 1,10; `Class_Priest` |
| Ranger | Power Shot | arma | Discipline | Agility | 1,15 | por tiro afetado; `Class_Ranger` |
| Ranger | Shadow Wolf | RP | Conjuration | Body | 0,30 | por ataque/summon, snapshot; `Class_Ranger` |
| Rogue | Poison Bomb | arma | Alteration | Special | 0,20 | pacote Poison por contato do AoE, sem hit direto do projétil; duração/intervalo existentes; `Class_Rogue` |
| Rogue | Throwing Dagger | arma | Discipline | Agility | 0,60 | `Class_Rogue` |
| Rogue | Backstab | arma | Discipline | Special | 1,60 | condições originais; `Class_Rogue` |
| Shaman | Spirit Shock | RP | Evocation | — | 0,75 | Spirit/Lightning total dividido; `Class_Shaman` |
| Shaman | Spirit Drain | RP | Evocation | Body | 0,10 | por tick; `Class_Shaman`, `SE_SpiritDrain` |
| Valkyrie | Shield Release | RP | Abjuration | Vigour | 0,22/carga | Frost/Spirit divididos; `Class_Valkyrie` |
| Valkyrie | Harpoon Pull | arma | Discipline | Agility | 0,75 | `Class_Valkyrie` |
| Valkyrie | Leap/Impact | arma | Discipline | Vigour | 0,75 | altitude adicional limitada a 2×; `Class_Valkyrie` |

Abilities sem dano direto (Challenge, Charm, Weaken, buffs de zona/bioma, Arcane Intellect, Mana/Eitr Shield, Reactive Armor, Shadow Stalk, Fade, Shell, Enrage, Bulwark) não receberam nova fórmula ofensiva. Fenring Form e skills emprestadas continuam usando o caminho da habilidade original. Procs passivos de arma/bioma do Enchanter e bônus passivos de classe não foram convertidos em novas abilities.

## D. Healing

| Ability | BaseHeal | Skill | Secondary | Tick/channel | Cooldown-base |
|---|---:|---|---|---|---:|
| Priest Purge | 5 | Alteration | Body | evento único | 15 s |
| Priest Heal | 15 | Alteration | Body | pulso inicial; posteriores preservam proporção antiga, sem repetir cura cheia | 30 s |
| Shaman Chain Healing | 12 | Alteration | Body | 0,70× por próximo alvo | 30 s |
| Druid Regeneration | 2 | Alteration | Body | ~2 s por tick durante ~20 s, potência do caster no `SetLevel` | 60 s |
| Monk Surge | 1 | Discipline | Body | 1 Chi/tick, intervalo ~2 s | conforme ativação existente |

No nível 100 com modifiers neutros e config 1: Purge 48,48, Heal 145,43, Chain 116,34, Regen 19,39/tick, Surge 9,70/Chi. Com INT 100, estimativas de teto sem restrição de recursos são respectivamente ~388, ~582, ~465, ~388 e ~291 HP/min (somente primeiro pulso/alvo nos dois canais indicados). Os dois modifiers mínimos/máximos produzem 0,5625×/1,5625×.

## E. EpicMMO, rede e double-dipping

Inspeção do DLL EpicMMOSystem 1.9.67 instalado: STR/INT modificam `ItemDrop.ItemData.GetDamage(int,float)` para itens no inventário; não há segundo modificador ofensivo global de `HitData` em `Character.Damage`/`RPC_Damage`. Assim arma recebe o bônus no `GetDamage`, mãos vazias recebem STR uma vez no helper, e magia criada diretamente no `HitData` recebe INT uma vez no helper. Nenhum desses caminhos aplica o getter de INT/STR uma segunda vez. Os tipos elementais da arma também são herdados do `GetDamage` já modificado.

Summons guardam potência final e ZDOID do summoner no ZDO ao nascer; `VL_Damage_Patch` normaliza o pacote para a potência gravada, inclusive quando o dono original não está mais presente. Regen passa o float calculado na aplicação via `SEMan.AddStatusEffect(hash, reset, itemLevel, skillLevel)` e `SetLevel`; Regen não usa nível/HP do recipient e restaura o TTL base quando substitui a versão do Ranger. Spirit Drain envia potência e ZDOID do caster em RPC ao dono do alvo, que aplica o StatusEffect; cada tick leva esse ID no HitData. Execute sinaliza o bit `0x4000` de `m_toolTier` no hit replicado, preserva e restaura o tier original no `RPC_Damage` do dono da vítima para aferir HP atual. Ice Shard/Blizzard continuam com os marcadores 138/137. Esses fluxos foram auditados no código e no DLL local, mas não executados em sessão host/client.

## F. Config

Permanecem ativos `g_DamageModifer`, `g_CooldownModifer`, `g_EnergyCostModifer` e os multiplicadores `c_*` aplicáveis, uma vez após a fórmula normalizada. As bases de cooldown/custo permanecem nos getters existentes; `c_berserkerDash` foi corrigido para ler `.Value`. `getAddMagicDamage()` não reduz cooldown nem infla a school skill ofensiva; `getStaminaReduction()` não representa DEX para custo. INT/Agility brutos passam pelo clamp 0–100. Scaling antigo ainda presente em raio, contagem, duração, proc passivo, chance ou skill gain não constitui a nova camada de dano/cura.

## G. Verificação

`dotnet build ValheimLegends.csproj -c Release --no-restore`: 0 erros, 33 warnings preexistentes (conflitos de assemblies e campos/variáveis antigos não usados). `dotnet run --project BalanceMath.Tests/BalanceMath.Tests.csproj -c Release`: passou. O pequeno executável testa RP L1/10/50/100, extremos da progressão, cooldown/custo, magia/físico, coeficientes representativos e exceções (Frozen, Execute, Chi e cargas), as cinco curas, falloff e HP/min. `git diff --check`: passou. A revisão independente de Sol identificou desvios de Execute, Poison Bomb, TTL de Regen e atribuição do Spirit Drain; eles foram tratados no código, com a limitação do poison vanilla descrita abaixo. Não houve smoke test no jogo nem teste automatizado das permutações host/client.

A revisão pós-correção de Sol confirmou os quatro tratamentos: filtro e marcador preservável do Execute, ausência de impacto extra da Poison Bomb, restauração do TTL de Regeneration e transporte do caster no Spirit Drain. Nenhuma regressão funcional concreta foi encontrada nessa revisão estática.

## H. Pendências reais

- Testar host→client, client→host e client→client para dano/skill attribution, snapshots de summons, Regen em player/pet e Chain Healing/Surge; o build e teste matemático não demonstram replicação real.
- A Poison Bomb entrega 0,20× em cada contato periódico do AoE, sem hit direto do projétil. O `SE_Poison` vanilla redistribui esse dano entre ticks de HP e ignora reaplicações menores que o dano restante; portanto **0,20× em cada tick de HP não está garantido** sem substituir a mecânica nativa de poison. Validar o resultado no jogo antes de tratar esse requisito literal como concluído.
- A atribuição de Spirit Drain agora leva ZDOID do caster no RPC e no HitData do tick, mas crédito real de kill/XP e interoperabilidade do marcador de Execute com outros mods ainda requerem teste host/client.
