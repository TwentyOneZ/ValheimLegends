PerspexCharacterAuthority 1.0.4 - teste Valheim 1.0.15

1. Copie PerspexCharacterAuthority.dll para BepInEx/plugins/PerspexCharacterAuthority/ no client e no servidor.
2. Remova DLLs PCA antigas das demais pastas de plugins para evitar carregamento duplicado.
3. Use no client e no servidor:

[Diagnostics]
Level = Trace
WriteDiagnosticFile = true
MirrorToGameLog = true
PendingStateIntervalSeconds = 1

[Authority]
RequireServerAuthority = true

4. Faça UMA tentativa de conexão com o mesmo personagem usado no teste anterior.
5. Colete:
   - BepInEx/config/PerspexCharacterAuthority/diagnostics/PCA_SERVER_*.log
   - BepInEx/config/PerspexCharacterAuthority/diagnostics/PCA_CLIENT_*.log
   - LogOutput/console correspondente do servidor
   - BepInEx/LogOutput.log do client

Sequência esperada: ZDO_ADD_PEER CALL ALLOW -> NATIVE_AUTHENTICATED -> SEND_HELLO -> RECV_IDENTIFY -> SEND_SNAPSHOT -> SNAPSHOT_APPLIED -> PLAYER_SPAWN ALLOW.

ProtocolVersion = 1 (wire format inalterado).
