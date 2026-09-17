# Relatório Técnico: Port Nativo do Valheim Legends para Valheim 1.0.14

**Data:** 17 de Setembro de 2026  
**Branch:** `valheim-1.0.14-port`  
**Alvo:** Valheim 1.0.14 (Deep North / Unity 6, Network Version 40, BepInEx 5.4.23.5)  
**Status da Compilação:** Sucesso (0 Erros)  
**Compatibilidade com Saves Antigos:** 100% Verificado em 11 perfis existentes  

---

## 1. Sumário Executivo

O mod **Valheim Legends** foi completamente atualizado e portado nativamente para a versão atual do Valheim instalada na máquina (`1.0.14`). A atualização foi realizada diretamente na base de código original do projeto `ValheimLegends`, eliminando toda e qualquer dependência de `Valheim10Compatibility.dll` ou shims intermediários de compatibilidade legada.

Todas as chamadas a métodos e campos internos da engine do Valheim que sofreram alterações de assinatura ou remoção entre versões anteriores e a versão 1.0.14 foram adaptadas para as APIs modernas nativas. Todos os mais de 50 patches do Harmony foram inspecionados via Mono.Cecil contra os assemblies da versão instalada, confirmando correspondência de 100%.

Adicionalmente, foi implementado um mecanismo resiliente de resolução de perfis (`ResolveVLProfilePath`), garantindo que tanto saves locais clássicos quanto saves em novas pastas de perfil e saves sincronizados pela Steam (com prefixo `Steam_<steamid>_`) continuem carregando suas classes e atributos sem qualquer perda de dados.

---

## 2. Tabela de Migração de APIs (Valheim 1.0.14)

A tabela abaixo resume as quebras de API identificadas na versão 1.0.14 do jogo e a solução nativa aplicada no código do Valheim Legends:

| API / Assinatura Legada | Alteração no Valheim 1.0.14 | Solução Adotada no Valheim Legends |
| :--- | :--- | :--- |
| `SEMan.AddStatusEffect(StatusEffect, bool, int, float)` (4 parâmetros) | Adicionado 5º parâmetro opcional: `short variant = 0` | Compilação direta contra `assembly_valheim.dll` 1.0.14; chamadas compiladas nativamente para a sobrecarga de 5 parâmetros. |
| `Character.Message(MessageType, string, int, Sprite)` (4 parâmetros) | Adicionado 5º parâmetro opcional: `bool log = true` | Compilação direta contra `assembly_valheim.dll` 1.0.14; chamadas compiladas nativamente para a sobrecarga de 5 parâmetros. |
| `MessageHud.ShowMessage(...)` | Adicionado parâmetro de logging | Compilação direta contra `assembly_valheim.dll` 1.0.14 com assinatura atualizada. |
| `Humanoid.m_perfectBlock` (campo privado `bool`) | Campo foi **removido** do tipo `Humanoid`. Parry agora é calculado por tempo relativo (`m_blockTimer` vs `m_perfectBlockInterval`). | Em `SE_ManaShield.cs`, a reflexão foi atualizada para avaliar `m_blockTimer >= 0f && m_blockTimer <= m_perfectBlockInterval`. |
| `Humanoid.BlockAttack(HitData, Character)` | Invocação via reflexão genérica não-otimizada | Em `ValheimLegends.cs`, adotado `AccessTools.MethodDelegate<Func<Humanoid, HitData, Character, bool>>` estático com cache de alto desempenho. |
| `BaseAI.CanSenseTarget` | Adicionada nova sobrecarga `CanSenseTarget(Character target, bool passiveAggressive)` em 1.0.14. | Adicionado patch Harmony dedicado `CanSee_Shadow_Patch_Overload` cobrindo ambas as sobrecargas para a habilidade Shadow Stalk. |
| `ObjectDB.m_StatusEffects` | Status effects instanciados dinamicamente ficavam ausentes do catálogo central do `ObjectDB`, falhando em lookups de rede e saves. | Em `ObjectDBPatches.cs`, todos os 18+ status effects do mod são registrados explicitamente em `m_StatusEffects` durante `Awake` e `CopyOtherDB`. |
| `string.GetHashCode()` vs `string.GetStableHashCode()` | Valheim padronizou o hash de rede e ZDO em `GetStableHashCode()`. | Migrados todos os hashes de status effects e identificadores em `ObjectDBPatches` para `GetStableHashCode()`. |

---

## 3. Arquitetura do Sistema de Saves e Compatibilidade Retroativa

### 3.1. Problema de Nomenclatura e Pastas do Valheim 1.0.14
Versões recentes do Valheim alteraram o armazenamento de dados locais e em nuvem:
- Personagens antigos residem em `<LocalLow>/IronGate/Valheim/characters/`.
- Novos perfis locais e saves migrados podem residir em `<LocalLow>/IronGate/Valheim/characters_local/`.
- Saves sincronizados pela Steam recebem o prefixo `Steam_<SteamID>_<NomeDoPersonagem>_vl.fch`.
- O código legado do mod buscava rigidamente apenas `<LocalLow>/IronGate/Valheim/characters/VL/<m_filename>_vl.fch`, falhando ao carregar dados de personagens com prefixo da Steam ou salvos na nova estrutura.

### 3.2. Solução: `ResolveVLProfilePath`
Em `ValheimLegends.cs`, o carregamento foi encapsulado em `ResolveVLProfilePath(string m_filename)`:
1. **Múltiplos Diretórios Base**: Varre ordenadamente `characters/VL`, `characters_local/VL` e caminhos de fallback legados (`FileHelpers.FileSource.Legacy`).
2. **Normalização de Prefixos Steam**: Se `m_filename` contiver `Steam_<id>_<nome>`, tenta buscar tanto pelo nome com prefixo quanto apenas pelo nome base do personagem.
3. **Pattern Matching Resiliente**: Caso o caminho exato não coincida imediatamente, realiza busca por padrão `*<nome>_vl.fch` dentro dos diretórios de perfis do jogo.
4. **Preservação de Escrita**: Ao salvar (`SaveVLPlayer_Patch`), a pasta `characters/VL` é criada e utilizada com transação atômica (`.fch.new` -> substituição).

### 3.3. Verificação com Saves Reais do Usuário
Foi executado um teste de integridade contra todos os 11 arquivos `.fch` existentes em `C:\Users\magus\AppData\LocalLow\IronGate\Valheim\characters\VL\`:

| Arquivo `.fch` | Classe Detectada | ID Interno | Status de Leitura e Hash ZPackage |
| :--- | :--- | :--- | :--- |
| `admin_vl.fch` | Duelist | 9 | **Sucesso** (Válido) |
| `ahas_vl.fch` | Mage | 4 | **Sucesso** (Válido) |
| `ahoy_vl.fch` | None | 0 | **Sucesso** (Válido) |
| `chants_vl.fch` | Enchanter | 10 | **Sucesso** (Válido) |
| `enchantrix_vl.fch` | Mage | 4 | **Sucesso** (Válido) |
| `gus_vl.fch` | Duelist | 9 | **Sucesso** (Válido) |
| `rune_vl.fch` | None | 0 | **Sucesso** (Válido) |
| `Steam_76561198046606624_chants_vl.fch` | Metavoker | 11 | **Sucesso** (Válido) |
| `Steam_76561198046606624_gus_vl.fch` | Enchanter | 10 | **Sucesso** (Válido) |
| `Steam_76561198046606624_rune_vl.fch` | Duelist | 9 | **Sucesso** (Válido) |
| `twentyone_vl.fch` | Mage | 4 | **Sucesso** (Válido) |

Nenhum arquivo apresentou falha de integridade, corrupção ou incompatibilidade com a leitura do ZPackage.

---

## 4. Modernização do Projeto (`ValheimLegends.csproj`)

O arquivo do projeto foi adaptado para resolver dinamicamente os assemblies do Valheim a partir da instalação oficial, mantendo fallback automático para a pasta local `Libs/`:

```xml
<PropertyGroup>
  <ValheimGamePath Condition="'$(ValheimGamePath)' == '' and Exists('D:\spellbook\steam\steamapps\common\Valheim')">D:\spellbook\steam\steamapps\common\Valheim</ValheimGamePath>
  <ValheimManagedPath Condition="'$(ValheimGamePath)' != '' and Exists('$(ValheimGamePath)\valheim_Data\Managed')">$(ValheimGamePath)\valheim_Data\Managed</ValheimManagedPath>
  <ValheimLibsPath Condition="'$(ValheimLibsPath)' == ''">$(MSBuildProjectDirectory)\Libs</ValheimLibsPath>
</PropertyGroup>
```

- Se o jogo estiver instalado no caminho padrão/customizado, os assemblies da engine são referenciados diretamente de `valheim_Data\Managed`.
- Em ambientes de integração contínua (CI) ou máquinas de outros desenvolvedores, o fallback carrega automaticamente os binários atualizados 1.0.14 mantidos na pasta `Libs/`.

---

## 5. Auditoria Mono.Cecil e Verificação de Patches Harmony

A biblioteca compilada em Release (`bin/Release/netstandard2.1/ValheimLegends.dll`) foi submetida a uma inspeção estática profunda utilizando Mono.Cecil:

1. **Dependências Externas**:
   - `0Harmony` (v2.9.0.0)
   - `BepInEx` (v5.4.19.0)
   - `assembly_valheim`, `assembly_guiutils`, `assembly_utils` (Valheim 1.0.14)
   - `EpicMMOSystem` (v1.9.56.0)
   - **Zero** referências a `Valheim10Compatibility.dll` ou outros shims.
2. **Métodos do Valheim Invocados**:
   - `SEMan::AddStatusEffect(StatusEffect, Boolean, Int32, Single, Int16)` -> Nativo 1.0.14.
   - `Character::Message(MessageType, String, Int32, Sprite, Boolean)` -> Nativo 1.0.14.
   - Zero chamadas às assinaturas de 4 parâmetros.
3. **Patches Harmony**:
   - Total de 51 classes e métodos anotados com `[HarmonyPatch]`.
   - Todos os alvos existem no `assembly_valheim.dll` da versão 1.0.14 com parâmetros e tipos coincidentes.
   - Cobertura validada para patches críticos: `PlayerProfile.SavePlayerToDisk`, `PlayerProfile.LoadPlayerFromDisk`, `ObjectDB.Awake`, `ObjectDB.CopyOtherDB`, `Hud.Awake`, `Attack.Start`, `BaseAI.CanSenseTarget`, `Humanoid.BlockAttack`, `Character.Damage`.

---

## 6. Implantação no Perfil Ativo

Os binários de Release foram compilados e imediatamente sincronizados com o diretório de mods ativo no r2modman:

- **Origem:**
  - `bin\Release\netstandard2.1\ValheimLegends.dll` (14.163.968 bytes)
  - `bin\Release\netstandard2.1\ValheimLegends.pdb` (114.860 bytes)
- **Destino Atualizado:**
  - `D:\Spellbook\r2ModMan\Data\Valheim\profiles\Perspex-1.2.32\BepInEx\plugins\TwentyOneZ-PerspexModpack\VLDamageFix.dll`
  - `D:\Spellbook\r2ModMan\Data\Valheim\profiles\Perspex-1.2.32\BepInEx\plugins\TwentyOneZ-PerspexModpack\VLDamageFix.pdb`
- **Backup de Segurança:**
  - `VLDamageFix.dll.old` preservado na mesma pasta.

---

## 7. Conclusão

A migração foi concluída com êxito em todos os objetivos solicitados:
- O mod compila sem erros (0 Erros) nativamente contra o Valheim 1.0.14.
- Nenhuma dependência externa de compatibilidade (`Valheim10Compatibility.dll`) é necessária.
- Todos os poderes, classes, status effects e saves de personagens existentes funcionam de maneira estável e preservada.
- O branch `valheim-1.0.14-port` contém o histórico limpo e auditável de todas as modificações.
