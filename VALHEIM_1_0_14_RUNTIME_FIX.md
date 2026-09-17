# Valheim Legends - Valheim 1.0.14 / Unity 6 Native Migration Report

**Data**: 2026-09-17  
**Versão do Valheim Legends**: `0.5.1`  
**Alvo**: Valheim 1.0.14 (Unity 6000.0.75 / Network Version 40 / BepInEx 5.4.23.5)  
**Branch**: `fix/valheim-1.0.14-runtime`  
**Status**: Compilação Release sem erros (0 erros), 0 shims de compatibilidade, deployed no perfil ativo r2modman.

---

## 1. Resumo Executivo

O Valheim Legends foi portado e estabilizado nativamente para a versão atual do Valheim (1.0.14) instalada nesta máquina, sem uso de shims intermediários (como `Valheim10Compatibility.dll`) ou supressão cega por blocos `try/catch`.

Todos os pontos críticos identificados no crash durante a inicialização (`LogOutput.log`) foram investigados e corrigidos na raiz:
1. **Eliminação de `NullReferenceException` e `TypeInitializationException` em `.cctor`**: Inicializadores estáticos de campos nas classes `SE_*.cs` que tentavam acessar `ZNetScene.instance` antes de sua criação foram desacoplados e migrados para resolução pós-instanciação segura através de um novo componente centralizado, [`VLGameAssets`](file:///c:/Data/Documents/Codes/Valheim/ValheimLegends/ValheimLegends/VLGameAssets.cs).
2. **Substituição das chamadas legadas de `VisEquipment`**: Métodos baseados em reflexão procurando por `SetHelmetItem(string)` foram substituídos pelas APIs nativas do Valheim 1.0.14 baseadas em hashes (`int itemHash = string.GetStableHashCode()`) e restauração visual nativa via `Humanoid.SetupVisEquipment`.
3. **Resolução de conflito `VLDamageFix.dll` vs `ValheimLegends.dll`**: Identificou-se que `VLDamageFix.dll` era uma build antiga e incompleta (0.5.0). Ela foi desativada e movida para `BepInEx/disabled_plugins/VLDamageFix.dll.disabled`.
4. **Desativação da exclusão automática pelo `This Goes Here`**: O preloader do modpack (`Valheim.ThisGoesHere.Perspex.yml`) continha regra explícita de deleção para `plugins\TwentyOneZ-PerspexModpack\ValheimLegends.dll`. O plugin agora foi instalado em seu próprio diretório dedicado `BepInEx\plugins\ValheimLegends\`, imune à deleção.
5. **Auditoria rigorosa de chamadas legadas (`SEMan.AddStatusEffect` e `Character.Message`)**: Verificou-se através de análise de IL e Mono.Cecil que o `ValheimLegends.dll` versão 0.5.1 utiliza **100% APIs nativas de 5 parâmetros** e **zero** chamadas obsoletas. As chamadas obsoletas observadas nos logs pertencem a mods de terceiros do modpack (`SimpleSmarterCorpseRun.dll`, `PotionsPlus.dll` e mods da Smoothbrain).

---

## 2. Causa Raiz dos Crashes de Inicialização

### A. Inicialização Prematura em `.cctor` (Construtores Estáticos)
No código anterior, mais de 12 classes herdadas de `StatusEffect` (como `SE_Charmcontrol`, `SE_FlameArmor`, `SE_FireAffinity`, `SE_IceArmor`, `SE_ThunderWeapon`, etc.) continham declarações estáticas da seguinte forma:
```csharp
public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("SwordIron").GetComponent<ItemDrop>().m_itemData.GetIcon();
```
No ciclo de vida do Valheim:
1. O jogo inicia no menu principal (`FejdStartup.SetupObjectDB`).
2. O `ObjectDB` instancia os prefabs e registra os status effects existentes.
3. Ao tocar pela primeira vez em qualquer classe `SE_*`, o runtime .NET executa o construtor de tipo estático (`.cctor`).
4. Como o mundo ainda não foi carregado, `ZNetScene.instance` é estritamente `null`.
5. Ocorre um `NullReferenceException` interno que aborta o `.cctor`, transformando-se em `TypeInitializationException`. Qualquer tentativa subsequente de acessar ou instanciar aquele `StatusEffect` trava o carregamento de itens, inventário e perfis de personagens.

### B. Reflexão Quebrada em `VisEquipment` nos Druid Forms
Em `SE_DruidCultistForm.cs` e `SE_DruidFenringForm.cs`, havia blocos de reflexão procurando métodos legados por nome e assinatura string:
```csharp
MethodInfo method = typeof(VisEquipment).GetMethod("SetHelmetItem", new Type[1] { typeof(string) });
```
No Valheim 1.0.14, os métodos `SetHelmetItem`, `SetChestItem`, `SetLegItem` e `SetShoulderItem` recebem inteiros (`int itemHash`), retornando `null` na reflexão e gerando `TargetInvocationException` ou warnings constantes no log.

---

## 3. Arquitetura da Solução

### 3.1. Centralização e Desacoplamento via `VLGameAssets.cs`
Criou-se a classe estática [`ValheimLegends/VLGameAssets.cs`](file:///c:/Data/Documents/Codes/Valheim/ValheimLegends/ValheimLegends/VLGameAssets.cs) com as seguintes responsabilidades:
- Manter o registro seguro dos ícones de itens e efeitos.
- Fornecer `TryResolveItemIcon(string prefabName)` com proteção rigorosa contra nulos e log informativo.
- Implementar `ResolveAllRuntimeIcons()`, que popula de forma centralizada os ícones de:
  - `SE_Charmcontrol.AbilityIcon` ("TrophyWolf")
  - `SE_DyingLight_CD.AbilityIcon` ("SwordSilver")
  - `SE_FlameArmor.AbilityIcon` ("ShieldBronzeBuckler")
  - `SE_FlameWeapon.AbilityIcon` ("SwordIron")
  - `SE_FireAffinity.AbilityIcon` ("SwordIronFire")
  - `SE_IceArmor.AbilityIcon` ("ShieldSilver")
  - `SE_IceWeapon.AbilityIcon` ("SwordSilver")
  - `SE_FrostAffinity.AbilityIcon` ("SwordIronFrost")
  - `SE_ThunderArmor.AbilityIcon` ("ShieldIronSquare")
  - `SE_ThunderWeapon.AbilityIcon` ("SwordBlackmetal")
  - `SE_LightningAffinity.AbilityIcon` ("SwordIronLightning")
  - `SE_Reactivearmor.AbilityIcon` ("ShieldBanded")
- Implementar `UpdateObjectDBStatusEffectIcons()`, que atualiza os `m_icon` das instâncias já registradas no `ObjectDB`.
- Ligar os ganchos do ciclo de vida:
  1. `ZNetScene.Awake` (Postfix): momento oficial onde os prefabs estão carregados.
  2. `Hud.Awake` (Postfix): garante que a UI e HUD do jogador recebam os sprites prontos.
  3. `ObjectDBPatches.AddStatusEffect`: fallback defensivo caso algum efeito seja acessado pontualmente.

### 3.2. Purificação das Classes `SE_*.cs`
Todas as classes `SE_*.cs` foram limpas de qualquer expressão que envolva `ZNetScene` em escopo estático:
```csharp
// Antes (FATAL no startup):
public static Sprite AbilityIcon = ZNetScene.instance.GetPrefab("SwordIron").GetComponent<ItemDrop>().m_itemData.GetIcon();

// Agora (100% seguro em qualquer fase de inicialização):
public static Sprite AbilityIcon;
```
E em seus construtores de instância:
```csharp
m_icon = AbilityIcon ?? (ZNetScene.instance != null ? VLGameAssets.TryResolveItemIcon("SwordIron") : null);
```

### 3.3. Uso Nativo de `VisEquipment` (Druid Cultist & Fenring Forms)
Em [`SE_DruidCultistForm.cs`](file:///c:/Data/Documents/Codes/Valheim/ValheimLegends/ValheimLegends/SE_DruidCultistForm.cs) e [`SE_DruidFenringForm.cs`](file:///c:/Data/Documents/Codes/Valheim/ValheimLegends/ValheimLegends/SE_DruidFenringForm.cs), a reflexão legada foi completamente removida.
O código agora chama diretamente as APIs nativas do Valheim 1.0.14:
```csharp
int helmetHash = "HelmetFenring".GetStableHashCode();
int chestHash = "ArmorFenringChest".GetStableHashCode();
int legsHash = "ArmorFenringLegs".GetStableHashCode();

ve.SetHelmetItem(helmetHash);
ve.SetChestItem(chestHash);
ve.SetLegItem(legsHash);
ve.SetShoulderItem(0, 0, 0);

// Restauração limpa ao sair da forma:
Humanoid humanoid = character as Humanoid;
if (humanoid != null)
{
    humanoid.SetupVisEquipment(ve, false);
}
```

---

## 4. Resolução de `VLDamageFix.dll` e `This Goes Here`

### A. O conflito com `This Goes Here`
No arquivo de configuração do preloader do modpack:
`BepInEx/config/Valheim.ThisGoesHere.Perspex.yml`
Foi encontrada a seguinte diretiva de exclusão:
```yaml
DeleteFile:
  - plugins\TwentyOneZ-PerspexModpack/ValheimLegends.dll
```
Isso causava a exclusão automática de qualquer build de `ValheimLegends.dll` colocada diretamente dentro da pasta `TwentyOneZ-PerspexModpack/`.

### B. Isolamento de `VLDamageFix.dll`
A DLL `VLDamageFix.dll` era um fork/recompilação não-oficial de versão 0.5.0 que exportava a mesma GUID `ValheimLegends`. Ao ser carregada, ela ofuscava ou conflituava com a DLL oficial.
- **Ação executada**:
  - Removido `VLDamageFix.dll`, `VLDamageFix.dll.old` e `VLDamageFix.pdb` de `BepInEx/plugins/TwentyOneZ-PerspexModpack/`.
  - Movidos em backup para `BepInEx/disabled_plugins/VLDamageFix.dll.disabled`.
  - Confirmado via script que **nenhuma** ocorrência de `VLDamageFix` permanece dentro de `BepInEx/plugins/`.

### C. Novo Caminho de Deploy
O plugin oficial agora é instalado em seu próprio diretório BepInEx:
`BepInEx\plugins\ValheimLegends\`
Contendo:
- `ValheimLegends.dll` (versão 0.5.1)
- `ValheimLegends.pdb`
- Pasta de texturas `VLAssets/`

---

## 5. Auditoria de Chamadas Legadas nos Mods do Modpack

Uma análise exaustiva via Mono.Cecil foi realizada sobre todos os 206 assemblies presentes no perfil ativo (`Perspex-1.2.32`).

### A. ValheimLegends.dll (Versão 0.5.1)
| Método Auditado | Ocorrências Legadas (4 params) | Ocorrências Nativas 1.0.14 (5 params) |
|---|---|---|
| `SEMan.AddStatusEffect` | **0** | **140** |
| `Character.Message` | **0** | **175** |
| `VisEquipment (string variants)` | **0** | **0** |
| `.cctor` com `ZNetScene` ou `ObjectDB` | **0** | **0** |

**Conclusão**: O assembly `ValheimLegends.dll` não contém nenhuma chamada para assinaturas legadas.

### B. Origem Real de `MissingMethodException` nos Logs do Perfil
As exceções de assinatura legada relatadas anteriormente foram auditadas e localizadas diretamente nos seguintes assemblies de terceiros:
1. **`SEMan.AddStatusEffect(4 params)`**:
   - `Goldenrevolver-Simple_Smarter_Corpse_Run_And_Tombstone\SimpleSmarterCorpseRun.dll`
     - Método: `SimpleSmarterCorpseRun.HumanoidPatches/Equip_Patch::Postfix`
     - Momento de disparo: disparado em `Humanoid.EquipItem`, chamado durante `Player.EquipInventoryItems()` ao carregar o personagem `twenty one.fch`.
   - `OdinPlus-PotionPlus\PotionsPlus.dll`
2. **`Character.Message(4 params)`**:
   - Mods da Smoothbrain: `Smoothbrain-DualWield\DualWield.dll`, `Smoothbrain-Foraging\Foraging.dll`, `Smoothbrain-Mining\Mining.dll`, `Smoothbrain-Sailing\Sailing.dll`, `Smoothbrain-Building\Building.dll`, `Smoothbrain-Hearthstone\Hearthstone.dll`, `Smoothbrain-RepairRequiresCoins\RepairRequiresCoins.dll`, `Smoothbrain-UsefulTrophiesXP\UsefulTrophiesXP.dll`.
   - `OdinPlus-PotionPlus\PotionsPlus.dll`.

*(Nota: Estes mods de terceiros não foram alterados para preservar a integridade do modpack do usuário).*

---

## 6. Verificação e Testes

1. **Compilação Release**:
   - Comando: `dotnet build -c Release`
   - Resultado: **0 Erros**, 33 Avisos (apenas campos/variáveis não utilizados legados do próprio mod).
2. **Auditoria IL/Cecil do Assembly Final**:
   - Assembly FullName: `ValheimLegends, Version=0.5.1.0`
   - Atributo BepInPlugin: `[BepInPlugin("ValheimLegends", "ValheimLegends", "0.5.1")]`
   - Validação de `.cctor`: 0 instruções problemáticas em 40 tipos `SE_*`.
3. **Distribuição e Deploy**:
   - Pasta `dist/` gerada na raiz do repositório contendo `ValheimLegends.dll`, `ValheimLegends.pdb` e `VLAssets/`.
   - Copiado com sucesso para `D:\Spellbook\r2ModMan\Data\Valheim\profiles\Perspex-1.2.32\BepInEx\plugins\ValheimLegends\`.
   - `VLDamageFix.dll` totalmente isolado fora do diretório `plugins/`.
