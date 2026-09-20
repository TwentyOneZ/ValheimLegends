# PerspexCharacterAuthority

Guia operacional do estado atual do plugin `PerspexCharacterAuthority` (PCA) e da integração com `ValheimLegends`.

> **Estado da validação:** a compilação e os checks de storage abaixo foram executados. Nenhum servidor/jogo foi iniciado nesta auditoria; os fluxos de login, spawn, RPC e troca de personagem continuam dependentes de teste runtime.

## Arquitetura real

O sistema é formado por dois assemblies:

- `PerspexCharacterAuthority.dll`: plugin BepInEx (`TwentyOneZ.PerspexCharacterAuthority`, versão `1.0.0`), protocolo e armazenamento server-side.
- `ValheimLegends.dll`: mod VL com a API pública `VLCharacterPersistence` e integração opcional via reflexão.

O snapshot PCA é um envelope binário versionado (`SchemaVersion = 1`, magic `PCA1`) com:

```text
AccountId, CharacterId (long), CharacterName,
CreatedUtcTicks, UpdatedUtcTicks,
VanillaPlayerData (blob nativo de PlayerProfile),
Extensions["ValheimLegends"] (schema 1, classe VL)
SHA-256 do envelope
```

O `CharacterId` usado hoje é o `long` retornado por `PlayerProfile.GetPlayerID()`. O nome é metadata e pode ser atualizado sem trocar o binding. O PCA intercepta `ZNet.RPC_PeerInfo`, `ZNet.RPC_PlayerID`, `Game._RequestRespawn`, `Player.OnSpawned`, `PlayerProfile.Save`, `PlayerProfile.LoadPlayerData` e `ZNet.OnDestroy`.

Fluxo implementado:

1. Depois que o Valheim valida o ticket Steam e marca o peer como pronto, o servidor envia `Hello`.
2. Só então o cliente envia `PCA_Message` com `ProtocolVersion = 1` e um snapshot local para identificar o personagem. O `AccountId` do envelope é o marcador não confiável `client-untrusted`.
3. O servidor ignora esse marcador, obtém a conta de `rpc.GetSocket()?.GetHostName()` e aplica o binding.
4. Se houver snapshot, o servidor o devolve; se não houver, cria o snapshot recebido e o torna a fonte autoritativa.
5. O cliente espera a resposta PCA antes de liberar o respawn e só permite `RPC_PlayerID` quando o `CharacterId` da sessão coincide.
6. O snapshot VL é importado no prefixo de `Player.OnSpawned`; saves e teardown enviam o snapshot local de volta ao servidor.

RPCs internos atuais (todos no pacote `PCA_Message`): `Hello`, `Identify`, `Snapshot`, `Submit`, `Denied` e `SaveAck`. O pacote valida versão, tamanho, schema, identidade do personagem e SHA-256.

## Instalação

1. Compile na solution `ValheimLegends.sln`, ou use os assemblies de `bin/Release/netstandard2.1/`.
2. Instale `PerspexCharacterAuthority.dll` no `BepInEx/plugins/` do servidor e de cada cliente.
3. Instale a build correspondente de `ValheimLegends.dll` no mesmo local quando o VL for usado.
4. Remova/desative `ServerSideSave.dll`; o PCA não depende dele e não deve coexistir com a lógica que zera `PlayerProfile.m_playerData`.
5. Não copie `assembly_valheim.dll`, BepInEx, Unity ou outras referências para o diretório de plugins; elas são referências de compilação (`Private=False`).

O modo singleplayer e um servidor sem PCA seguem o fallback vanilla/VL local depois do timeout de handshake. A restrição de conta só é aplicada quando a sessão PCA é detectada.

## Configuração

O arquivo é criado pelo BepInEx em `BepInEx/config/PerspexCharacterAuthority.cfg`:

```ini
[General]
Enabled = true
DebugLogging = false

[Access]
SingleCharacterPerAccount = true

[FirstJoin]
Mode = PreserveCharacter

[Saving]
AutosaveIntervalSeconds = 300
SaveOnLogout = true
SaveOnDisconnect = true
SaveOnServerShutdown = true

[Backups]
Enabled = true
MaximumBackupsPerCharacter = 10

[Limits]
MaxSnapshotSizeMB = 16
```

`SingleCharacterPerAccount` é `true` por padrão. `PreserveCharacter` mantém o blob vanilla recebido no primeiro login. `FreshProgression` marca o snapshot para que o cliente remova inventário/equipamento, skills e progressão vanilla, comida e poder, resete a classe VL e conceda itens padrão antes de salvar. Aparência e `m_customData` de outros mods são preservados para não destruir customização desconhecida. O nome do modo é comparado sem diferenciar maiúsculas/minúsculas; valores inválidos não têm validação dedicada e devem ser tratados como configuração incorreta.

Não existem atualmente `BackupOnLogin`, `SaveInterval` separado por personagem ou `MaxSnapshotSizeBytes`; não adicione essas chaves ao arquivo esperando efeito.

## Diretório de saves

O root real é `Paths.ConfigPath/PerspexCharacterAuthority` (normalmente `BepInEx/config/PerspexCharacterAuthority`):

```text
PerspexCharacterAuthority/
  Accounts/<sha256(AccountId)>.pcb
  Characters/<sha256(AccountId)>/<CharacterId>/
    current.pca
    backups/<timestamp>.pca
  Owners/<CharacterId>.owner
  Exports/<sha256(AccountId)>_<CharacterId>_<utc>.pca
```

Os nomes de conta são hash SHA-256 no caminho. O conteúdo `AccountId` ainda é gravado no envelope/binding e validado contra o arquivo. `.pca` e `.pcb` têm schema 1 e checksum SHA-256; não são JSON e não devem ser editados manualmente.

O `Owners/<CharacterId>.owner` impede que o mesmo ID seja reivindicado por outra conta. Os locks são por `account`, `owner` ou `character` dentro do processo do servidor. Um lease em memória também impede duas conexões de gravarem simultaneamente o mesmo par conta/personagem; reconnect substitui apenas uma conexão cujo RPC já esteja fechado.

## Binding, unbind e rebind

Com `SingleCharacterPerAccount = true`, o primeiro personagem aceito cria `Accounts/<hash>.pcb`, contendo `AccountId`, `CharacterId`, `LastKnownCharacterName` e `BoundUtcTicks`. O mesmo ID com nome novo é aceito e atualiza o nome; outro ID é negado antes da autorização de `RPC_PlayerID`.

Com `false`, novos IDs da mesma conta podem ser aceitos e cada um mantém snapshot e extensão separados. Reativar a chave não escolhe arquivo por ordem: se não houver `.pcb`, o primeiro login válido após a reativação cria o binding. Os snapshots não vinculados permanecem no disco.

`unbind` remove somente o `.pcb`; não apaga snapshots nem `Owners`. O próximo personagem aceito cria o novo binding. `bind` grava explicitamente um binding para um `CharacterId` numérico existente ou recém-reivindicado.

O PCA aceita autoridade somente em peers `ZSteamSocket`: no Valheim 1.0.15 o ticket Steam é validado antes de `peer.m_uid` tornar o peer pronto, e o postfix exige esse estado. Backends não-Steam são rejeitados porque a autenticação PlayFab é assíncrona nesse ponto do lifecycle e ainda não oferece a mesma garantia. Isso deve ser coberto no teste runtime do servidor dedicado.

## Snapshots, backups e restore

Cada substituição atômica grava um `.tmp.<guid>`, faz `Flush(true)` e usa `File.Replace` quando já existe `current.pca`. Com backups ativos, o current anterior é copiado para `backups/` e a rotação mantém até `MaximumBackupsPerCharacter` arquivos. Se o current falhar no checksum, o loader tenta os backups mais recentes e sinaliza recuperação no log.

O limite de retenção é internamente clamped para pelo menos 1 quando backups estão ativos; `MaximumBackupsPerCharacter=0` não pode apagar a única cópia exigida por uma operação destrutiva.

Comandos destrutivos (`reset`, `delete`, `restore`) criam backup prévio mesmo com `Backups.Enabled=false`. `reset` preserva o snapshot e agenda `FreshProgression` para o próximo login; `delete` faz backup e remove `current.pca`, permitindo que o modo de primeiro login seja aplicado novamente. Os três revogam uma sessão ativa do personagem e exigem reconnect antes de qualquer novo save.

Ao receber um save, o servidor preserva extensões autoritativas que o cliente não reconhece/exporta. A marca one-shot de `FreshProgression` só é removida quando o cliente devolve `PCA.FreshApplied` junto do blob já resetado; saves precoces mantêm o reset pendente.

Faça backup do diretório inteiro antes de alterar DLL/configuração. Para restaurar manualmente, pare o servidor, preserve o diretório original, copie o arquivo `.pca` desejado para o diretório do personagem como `current.pca` e reinicie. O caminho seguro e auditável é o comando `pca restore`.

## Comandos administrativos

O comando é registrado como `onlyServer: true, onlyAdmin: true`; use o console do servidor. Os argumentos são `AccountId` e `CharacterId` numérico, não nome de jogador:

```text
pca list
pca info <account>                  # binding e personagens conhecidos
pca status                           # estado PCA/protocolo/snapshot da sessão local
pca binding <account>
pca characters <account>
pca unbind <account>
pca bind <account> <characterId> [name]
pca backup <account> <characterId>
pca backups <account> <characterId>
pca restore <account> <characterId> <backup-file>
pca reset <account> <characterId>
pca delete <account> <characterId>
pca export <account> <characterId>
```

`export` escreve em `Exports/` e retorna o caminho. `restore` rejeita backups cuja conta/personagem interna não corresponda ao alvo. O comando `status` mostra apenas estado PCA/protocolo/timestamp no código atual; não é o painel completo sugerido no projeto.

## Integração Valheim Legends

`ValheimLegends/VLCharacterPersistence.cs` expõe:

```csharp
VLCharacterState ExportCharacterState(Player player);
void ImportCharacterState(Player player, VLCharacterState state);
void ResetCharacterState(Player player);
```

O `VLCharacterState` atual contém somente `SchemaVersion` e `PlayerClass Class`. O PCA encontra esses métodos por reflexão; sem a DLL VL, a extensão fica ausente e o vanilla continua funcionando.

Quando PCA está ativo, o patch VL de `PlayerProfile.SavePlayerToDisk` não escreve o sidecar legado e o patch de `LoadPlayerFromDisk` limpa o estado global. O reset zera `vl_player`, recria `vl_playerList`, remove buffs de classe e limpa caches/flags runtime conhecidos. Depois o PCA importa a classe do personagem autorizado; ausência da extensão significa classe `None` e também dispara reset, nunca fallback para o sidecar local.

Sem PCA, o fluxo VL legado continua usando:

```text
<save-data>/characters/VL/<filename>_vl.fch
<save-data>/characters_local/VL/<filename>_vl.fch
```

com fallback legado e gravação atômica `.fch.new` → `.fch`.

As skills customizadas (`Discipline`, `Abjuration`, `Alteration`, `Conjuration`, `Evocation`, `Illusion`) são persistidas por `Player.Save` → `Skills.Save` dentro do blob vanilla. O `VL_SkillData` em `ModData/<ModID>/char_<filename>` é fallback legado sem caller ativo de `SaveModData` e não é fonte autoritativa nem duplicado pela extensão PCA.

Também existem dados temporários em status effects, caches estáticos e ZDOs de companheiros. O envelope PCA não exporta esses ZDOs; eles não devem ser tratados como estado de personagem sem uma decisão específica.

## Troubleshooting

**PCA não aparece no log:** confirme DLL, BepInEx e Valheim compatíveis; confira `Enabled = true` e remova DLLs duplicadas/antigas.

**Cliente volta ao vanilla:** isso ocorre após o timeout de handshake ou quando o servidor não registra PCA. Confira se PCA está instalado no servidor e cliente e se o RPC não foi bloqueado por outro mod.

**“already bound to” ao entrar:** use o `CharacterId` do binding (`pca binding <account>`), ou `pca unbind <account>` para permitir um novo binding. `unbind` não apaga saves.

**Checksum mismatch:** não apague `current.pca`; preserve os arquivos e use `pca backups`/`pca restore`. O loader só deve aceitar um backup cujo checksum/schema sejam válidos.

**Classe VL não aparece/troca de personagem vaza estado:** confirme `ValheimLegends.dll` carregado, procure `[PCA] VL extension` no log com `DebugLogging`, e reproduza após logout completo. Esse fluxo requer runtime; a limpeza estática não prova isolamento das skills sidecar.

**Servidor não aceita account/character:** verifique permissões de escrita em `BepInEx/config/PerspexCharacterAuthority`, colisão em `Owners/` e o identificador de conta reportado pelo peer. Não edite `.pcb` à mão.

**API de Valheim:** as assinaturas e o lifecycle foram decompilados diretamente da instalação local 1.0.15. O script legado `audit-valheim-api.ps1` ainda se identifica como 1.0.14; mantenha o teste runtime antes do deploy.

## Validação desta documentação

Executado nesta revisão:

```text
dotnet build ValheimLegends.csproj --no-restore                         # 0 erros, 33 avisos
dotnet build PerspexCharacterAuthority/PerspexCharacterAuthority.csproj --no-restore  # 0 erros, 2 avisos
dotnet run --project PerspexCharacterAuthority.Tests --no-restore      # PCA storage/binding checks passed
powershell -ExecutionPolicy Bypass -File tools/audit-valheim-api.ps1   # 0 violações legadas (alvo 1.0.14)
```

Nenhum teste de jogo, rede, spawn, login, crash, backup em produção ou compatibilidade de modpack foi executado.
