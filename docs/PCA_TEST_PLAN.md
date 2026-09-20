# PCA — plano de testes

Os testes abaixo são os 20 cenários obrigatórios. `Automatizado` significa que há cobertura sem iniciar Valheim; `Runtime` exige servidor dedicado e cliente(s). A existência do teste automatizado não substitui a etapa runtime.

| # | Cenário | Automatizado atual | Runtime necessário / critério |
|---:|---|---|---|
| 1 | Aparência (cabelo, barba, cores) | Não | Sair/entrar e confirmar a aparência no mundo e na seleção. |
| 2 | Inventário | Não | Itens e equipamentos permanecem iguais após logout/login. |
| 3 | Hammer/recipes/pieces | Não | Materiais, recipes e known pieces permanecem após relogin. |
| 4 | Classe VL | Não | Classe escolhida permanece no mesmo personagem. |
| 5 | Skill VL | Não | Evocation (e as demais skills) mantém valor e accumulator. |
| 6 | Single-character padrão | Parcial: binding é coberto pelo check de storage | Com default `true`, A vincula; B é negado antes do spawn e não cria snapshot ativo. |
| 7 | Reconnect do personagem vinculado | Não | A reconecta e é permitido; nenhum novo binding é criado. |
| 8 | Rename | Parcial: atualização de nome está no storage | Renomear A mantém `CharacterId`, atualiza `LastKnownCharacterName` e não duplica snapshot. |
| 9 | `pca unbind` | Parcial: remoção do `.pcb` está no storage | `unbind` preserva snapshots; B vira binding no próximo login válido. |
| 10 | Modo múltiplos personagens | Parcial: múltiplos IDs são cobertos pelo storage | Com `false`, A e B entram; B não herda classe/skills/inventário; A volta intacto. |
| 11 | Reabilitar restrição | Parcial: regra de primeiro binding é coberta pelo storage | Com A/B e sem binding, primeiro login válido (B no teste) vincula B; A é negado; nenhum save é apagado. |
| 12 | Dois SteamIDs/contas | Parcial: ownership de ID é coberto pelo storage | Duas contas têm bindings independentes e não bloqueiam uma à outra. |
| 13 | Spoof de CharacterId | Parcial: owner file rejeita ID de outra conta | Tentar ID alheio não devolve snapshot nem permite spawn; há warning de segurança. |
| 14 | Spoof de AccountId | Parcial: o envelope cliente usa marcador ignorado e peers não-Steam são rejeitados | Alterar o campo enviado pelo cliente não altera a conta derivada do `ZSteamSocket`; confirmar em runtime com ticket Steam válido. |
| 15 | Crash durante save / `.tmp` incompleto | Sim, parcialmente: escrita atômica e backup são exercitados | Interromper processo durante save; `current.pca` anterior continua carregável. |
| 16 | `current.pca` corrompido | Sim: checksum + recuperação de backup no `PerspexCharacterAuthority.Tests` | Corromper arquivo no servidor e confirmar detecção, fallback válido e ausência de bytes inválidos carregados. |
| 17 | Reconnect rápido/concor­rência | Parcial: locks, rotação e lease de sessão estão implementados | Dois reconnects/saves rápidos não corrompem o snapshot; a segunda sessão ativa do mesmo personagem é negada. |
| 18 | Singleplayer | Não | Sem servidor PCA, vanilla e VL local funcionam; single-character não bloqueia personagens locais. |
| 19 | Cliente sem servidor PCA | Não | Cliente PCA faz fallback limpo ao vanilla/VL após handshake timeout. |
| 20 | Servidor sem Valheim Legends | Não | PCA opera com snapshot vanilla; extensão VL ausente não impede login/save. |

## Check automatizado existente

Executar da raiz:

```text
dotnet run --project PerspexCharacterAuthority.Tests --no-restore
```

O programa verifica serialização/deserialização do envelope, SHA-256 inválido, binding single/multiple, ownership entre contas, save/load, binding corrompido fail-closed, snapshot corrompido sem backup fail-closed, recuperação via backup e rejeição de restore com identidade errada. Resultado observado nesta revisão: `PCA storage/binding checks passed.`

Também foram executadas verificações estáticas:

```text
dotnet build ValheimLegends.csproj --no-restore
dotnet build PerspexCharacterAuthority/PerspexCharacterAuthority.csproj --no-restore
powershell -ExecutionPolicy Bypass -File tools/audit-valheim-api.ps1
```

Resultado: ambos compilam com zero erros; o audit reporta zero chamadas legadas para o alvo Valheim 1.0.14. Os avisos de compilação permanecem e não foram tratados como testes runtime.

## Ordem recomendada do ensaio runtime

1. Faça backup de `BepInEx/config/PerspexCharacterAuthority` e dos saves vanilla/VL.
2. Use duas contas reais ou duas conexões controladas; registre `AccountId`, `CharacterId`, nome e timestamp em cada passo.
3. Comece pelos testes 1–5 com `PreserveCharacter`.
4. Rode 6–9 com `SingleCharacterPerAccount = true`.
5. Rode 10–12 com `false`, depois o 11 com `true` e nenhum binding.
6. Rode 13–17 com cópias de backup e uma janela de manutenção; nunca corrompa o único original sem cópia.
7. Rode 18–20 com PCA/VL alternadamente habilitados.
8. Após cada caso, capture `BepInEx/LogOutput.log`, estrutura de saves e resultado do comando PCA.

## Critérios de falha imediata

- B ou qualquer personagem proibido aparece no mundo antes da negação.
- O servidor aceita `AccountId` informado pelo cliente em vez da identidade autenticada do peer.
- Um snapshot de outra conta é devolvido, mesmo que o `CharacterId` coincida.
- `current.pca` inválido é carregado sem recuperação/log.
- A classe, skills ou dados de A aparecem em B.
- `reset`, `delete` ou `restore` destrói o único backup antes de confirmar a nova escrita.

## Lacunas conhecidas a resolver antes de declarar aceite

- O mapeamento de `ZSteamSocket.GetHostName()` para o SteamID validado deve ser confirmado no servidor dedicado; backends não-Steam são deliberadamente rejeitados.
- A extensão VL transporta somente `Class`; as skills VL ficam no blob nativo por `Skills.Save/Load`, o que ainda requer validação runtime.
- O script de auditoria existente se identifica como 1.0.14; as APIs críticas foram verificadas separadamente na DLL instalada 1.0.15.
- Nenhum dos testes runtime desta matriz foi executado nesta revisão.
