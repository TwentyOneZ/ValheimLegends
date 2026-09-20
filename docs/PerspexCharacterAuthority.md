# PerspexCharacterAuthority 1.0.4

Estado desta revisão: **automated: PASS; runtime: NOT RUN**.

## Autoridade e lifecycle

O plugin BepInEx `TwentyOneZ.PerspexCharacterAuthority` mantém `ProtocolVersion = 1`: o envelope e os tipos de mensagem não mudaram; a correção é de lifecycle, estado e barreiras server-side.

1. O prefixo de `ZNet.OnNewConnection` registra `PCA_Message` nos dois lados, antes de qualquer mensagem PCA.
2. O Valheim valida `RPC_PeerInfo`. A chegada nativa a `ZDOMan.AddPeer` comprova que versão, whitelist, ticket/identidade, lotação, senha e UID passaram; o patch PCA é apenas observador e sempre permite a chamada. A identidade vem do socket autenticado, sem usar `peer.IsReady()` como gate. `Hello` é enviado uma única vez.
3. O cliente só envia `Identify` ao receber um `Hello` válido. Hello duplicado não gera outro Identify.
4. O servidor valida binding/ownership, cria ou carrega `current.pca` e envia o snapshot autoritativo.
5. `ZDOMan.AddPeer` e `ZRoutedRpc.AddPeer` nunca são bloqueados pelo PCA. `RPC_PlayerID` apenas registra identidade vanilla; `RPC_CharacterID`, emitido por `SetCharacterID` dentro do spawn real, exige sessão autorizada.
6. O cliente aplica `PlayerProfile.m_playerData`, executa `FreshProgression` durante `LoadPlayerData` quando solicitado e só então libera `_RequestRespawn`. `Game.SpawnPlayer` é uma segunda barreira fail-closed.

Estados do cliente: `Vanilla`, `WaitingForHello`, `WaitingForSnapshot`, `Allowed` e `Denied`. Submit só existe em `Allowed`. Com autoridade obrigatória, timeout ou mensagem inválida resulta em `Denied` e desconexão. Com autoridade opcional, fallback vanilla só pode ocorrer antes de qualquer Hello; depois de detectar PCA, a falha sempre é fechada.

O servidor expira separadamente espera por autenticação nativa, Identify ou autorização. O networking vanilla pode ficar operacional antes da sessão PCA; o personagem não pode spawnar nem submeter estado nesse intervalo.

Na assembly real do Valheim 1.0.15, `ZNetPeer.IsReady()` é exatamente `m_uid != 0`. `m_uid`, `m_playerName` e `m_playfabId` são atribuídos no fim bem-sucedido de `RPC_PeerInfo`, antes de `ZDOMan.AddPeer` e `ZRoutedRpc.AddPeer`; portanto `IsReady` não depende direta ou indiretamente desses `AddPeer`. O deadlock observado vinha da fronteira PCA incorreta: infraestrutura nativa era bloqueada por `sessions`, enquanto `sessions` só surgia depois do handshake PCA. A 1.0.4 remove essa dependência circular e usa o ponto nativo de aceitação como evento, não como gate.

Limite de segurança: nenhum mod exclusivamente server-side consegue provar criptograficamente que um cliente deliberadamente modificado aplicou bytes recebidos. O PCA bloqueia cliente ausente, protocolo inválido, identidade/binding incorretos e acesso ao mundo antes da sessão; proteção contra um cliente que implementa o protocolo e mente exige anticheat/atestado remoto fora do escopo do Valheim/PCA.

## Identidade

`AccountId` enviado pelo cliente é sempre não confiável e ignorado. Para Steam, o servidor usa `ZSteamSocket.GetHostName()`, que nesta versão retorna o SteamID remoto associado ao ticket validado antes de `m_uid`. Para PlayFab, usa `m_remotePlayerId` como `playfab/<id>`, não o `PlatformUserID` informado pelo cliente. Backends desconhecidos são rejeitados.

## Configuração

```ini
[Authority]
RequireServerAuthority = true
HandshakeTimeoutSeconds = 15

[Access]
SingleCharacterPerAccount = true

[FirstJoin]
Mode = PreserveCharacter

[General]
Enabled = true
DebugLogging = false

[Diagnostics]
Level = Basic
WriteDiagnosticFile = true
MirrorToGameLog = true
PendingStateIntervalSeconds = 1

[Limits]
MaxSnapshotSizeMB = 16

[Saving]
AutosaveIntervalSeconds = 300
SaveOnLogout = true
SaveOnDisconnect = true
SaveOnServerShutdown = true

[Backups]
Enabled = true
MaximumBackupsPerCharacter = 10
```

`HandshakeTimeoutSeconds` é limitado a 5–60. `RequireServerAuthority=true` é o padrão seguro para o profile Perspex. Para conectar este cliente a um servidor realmente sem PCA, configure explicitamente `false`; singleplayer continua vanilla/local sem handshake de peer remoto.

`FreshProgression` grava o primeiro snapshot já marcado numa única operação atômica. No cliente, remove equipamento/inventário, progressão vanilla, comida, Guardian Power e classe VL antes de `OnSpawned`, salva o blob resetado e o envia imediatamente. A marca fica server-side até o Submit incluir `PCA.FreshApplied`.

## Saves e integração Valheim Legends

O root é `BepInEx/config/PerspexCharacterAuthority`:

```text
Accounts/<sha256(AccountId)>.pcb
Owners/<CharacterId>.owner
Characters/<sha256(AccountId)>/<CharacterId>/current.pca
Characters/<sha256(AccountId)>/<CharacterId>/backups/<timestamp>.pca
Exports/<account-hash>_<CharacterId>_<utc>.pca
```

Snapshots têm schema/checksum SHA-256 e substituição atômica com backup. Submit ocorre em autosave, `PlayerProfile.Save`, logout, desconexão e encerramento quando configurados.

O PCA usa `VLCharacterPersistence.ExportCharacterState`, `ImportCharacterState` e `ResetCharacterState` por reflexão. Com PCA ativo, os patches de classe e skills não leem sidecars locais depois do snapshot. Sem PCA, o fluxo legado/local do VL permanece ativo. Skills ficam no blob nativo de `PlayerProfile`; a extensão VL transporta a classe.

## Diagnóstico e administração

`Diagnostics.Level` aceita `Off`, `Basic`, `Verbose` e `Trace`. `General.DebugLogging=true` mantém compatibilidade e equivale no mínimo a `Verbose`. Os arquivos ficam em `BepInEx/config/PerspexCharacterAuthority/diagnostics/PCA_SERVER_*.log` ou `PCA_CLIENT_*.log`; contas são mascaradas e payloads nunca são despejados.

`pca status` mostra o resumo global. `pca diag`, `pca diag connections` e `pca diag C001` mostram conexões pendentes/autorizadas sem secrets.

Comandos existentes: `list`, `info`, `status`, `diag`, `binding`, `characters`, `unbind`, `bind`, `backup`, `backups`, `restore`, `reset`, `delete` e `export`. Administração exige console server/admin.

## Validação

```text
dotnet build PerspexCharacterAuthority/PerspexCharacterAuthority.csproj -c Release
dotnet run --project PerspexCharacterAuthority.Tests/PerspexCharacterAuthority.Tests.csproj -c Release
dotnet build ValheimLegends.csproj -c Release
powershell -ExecutionPolicy Bypass -File tools/audit-valheim-api.ps1
```

Resultado automatizado: **PASS**. Testes de jogo/rede: **RUNTIME NOT RUN**. Veja `PCA_TEST_PLAN.md`.
