# PCA — plano de testes

Status desta revisão: **automated: PASS; runtime: NOT RUN**.

## Cobertura automatizada

```text
dotnet run --project PerspexCharacterAuthority.Tests/PerspexCharacterAuthority.Tests.csproj -c Release
```

A suíte cobre codec/checksum, escrita/leitura/backup/restore, binding e ownership, além de:

- state machine e bloqueio de spawn antes de autorização;
- Identify somente como ação após Hello e Hello duplicado idempotente;
- Snapshot fora de ordem, protocolo incorreto e CharacterId diferente;
- Denied e timeout obrigatório sem fallback;
- fallback explícito apenas enquanto `WaitingForHello`;
- timeout após Hello sempre fail-closed;
- Submit somente em `Allowed`;
- sessão não autorizada, reconnect revogando a sessão anterior e ID incorreto;
- `FreshProgression` one-shot.
- Hello idempotente, identidade inicialmente pendente e aceitação nativa sem gate de `AddPeer`;
- múltiplos Submits e SaveAcks concorrentes por contador.

Resultado observado: `PCA handshake/session checks passed.` e `PCA storage/binding checks passed.`

## Checklist runtime obrigatório

| Cenário | Critério de aceite | Estado |
|---|---|---|
| Primeiro login / FreshProgression | `Accounts`, `Owners` e `current.pca` surgem; inventário, skills, poder e classe são resetados antes do controle | NOT RUN |
| Relogin autoritativo | alterar itens localmente offline não altera o estado carregado no servidor | NOT RUN |
| Cliente sem PCA | nunca entra no mundo; servidor registra timeout e desconecta | NOT RUN |
| Handshake lento | abaixo do timeout configurado conclui; acima falha fechado | NOT RUN |
| Servidor sem PCA | com `RequireServerAuthority=false`, fallback local explícito; com `true`, desconecta | NOT RUN |
| Singleplayer | persiste vanilla/VL localmente sem bloqueio | NOT RUN |
| Classe e skills VL | classe/skills corretas por personagem; sidecar antigo não sobrescreve PCA | NOT RUN |
| Personagens/contas | binding único, modo múltiplo, rename, unbind/rebind e ownership isolados | NOT RUN |
| Reconnect concorrente | sessão antiga não grava depois de substituída; sessão simultânea ativa é negada | NOT RUN |
| Saves | autosave, Save, logout, disconnect e shutdown atualizam `current.pca`/backups | NOT RUN |
| Corrupção/crash | current inválido recupera backup válido; ausência de backup falha fechado | NOT RUN |
| PlayFab | ID estável `playfab/<EntityKey.Id>` confirmado em conexão real | NOT RUN |

## Roteiro mínimo

1. Configure `Diagnostics.Level=Trace` e inicie servidor e cliente com PCA 1.0.4; entre com um personagem local contendo itens e classe.
2. Confirme nos logs a sequência `RPC_REGISTER_OK`, `ZDO_ADD_PEER CALL ALLOW`, `NATIVE_AUTHENTICATED`, `SEND_HELLO`, `RECV_IDENTIFY`, `SEND_SNAPSHOT`, `SNAPSHOT_APPLIED` e `PLAYER_SPAWN ALLOW`; confirme os diretórios no servidor.
3. Saia, altere o mesmo personagem em singleplayer, volte ao servidor e confirme que a alteração local não venceu o snapshot.
4. Remova PCA do cliente e confirme que ele não aparece no mundo e é desconectado após o timeout.
5. Teste singleplayer e, separadamente, servidor sem PCA com `RequireServerAuthority=false`.
6. Colete exatamente um `PCA_SERVER_*.log` e um `PCA_CLIENT_*.log` da mesma tentativa.

Falha imediata: peer aparece no mundo antes da sessão; snapshot local substitui o server-side por falha de handshake; AccountId cliente é aceito; dados de outro personagem/conta vazam; `FreshProgression` ocorre depois de `OnSpawned`.
