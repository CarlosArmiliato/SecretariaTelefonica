# Modelo de dados: Ligação para Assistência Técnica

**Data**: 2026-09-13 | **Referência**: [spec.md](spec.md)

Instantes persistidos usam `DateTimeOffset` em UTC. Datas e caminhos apresentados usam
`America/Sao_Paulo`. IDs são opacos e não derivam de nomes, telefones ou transcrições.

## Solicitação de Chamada

| Campo | Tipo | Regra |
|-------|------|-------|
| `requestId` | UUID | Imutável e único. |
| `createdAtUtc` | instante | Relógio injetável. |
| `mode` | `Simulation` | Único valor aceito neste incremento. |
| `status` | enum | `Draft`, `NeedsClarification`, `ReadyForReview`, `Closed`. |
| `activeRevisionId` | UUID opcional | Revisão imutável corrente. |
| `activeAttemptId` | UUID opcional | No máximo uma tentativa não terminal. |

Uma solicitação cria várias revisões, mas autorização e tentativa referenciam exatamente uma revisão.

## Revisão do Preflight

| Campo | Tipo | Regra |
|-------|------|-------|
| `preflightRevisionId` | UUID | Novo a cada alteração. |
| `requestId` | UUID | Solicitação proprietária. |
| `companyName`, `dialNumber`, `objective` | valores | Obrigatórios e exibidos na confirmação. |
| `equipment`, `problemDescription` | texto | Somente fatos fornecidos. |
| `approvedFacts` | lista | ID, texto e origem Carlos. |
| `approvedTestsAndErrors` | lista | “Nenhum” precisa ser explícito. |
| `pricePolicy` | objeto | Teto/faixa opcional e `mayDisclose`. |
| `availability` | lista | Informação; não autoriza agenda final. |
| `questions`, `successCriteria` | listas | Não vazias. |
| `disclosureGrants` | lista | Permissão por dado e por revisão. |
| `handoffTriggers` | lista | Inclui dúvida, recusa de IA e decisão humana. |
| `validation` | lista | Vazia somente quando válida. |
| `createdAtUtc` | instante | Auditoria. |

Concessões usam `CarlosName` e `CarlosPhone` como referências ao ambiente, nunca valores copiados.
`EquipmentModel` e `EquipmentSerial` exigem concessões separadas; outros dados começam negados.

## Autorização de Discagem

| Campo | Tipo | Regra |
|-------|------|-------|
| `authorizationId`, `attemptId` | UUID | Uso único para uma tentativa. |
| `preflightRevisionId` | UUID | Revisão mostrada e confirmada. |
| `bindingDigest` | bytes | Calculado e mantido somente em memória; não aparece em arquivos ou logs. |
| `confirmedAtUtc` | instante | Confirmação imediata. |
| `supervisionConfirmed` | booleano | Deve ser verdadeiro. |
| `status` | enum | `PendingUse`, `Consumed`, `Invalidated`. |
| `invalidationReason` | código opcional | Mudança, recusa, reinício, conflito ou fim. |

O digest vincula número, empresa, objetivo e revisão somente durante o processo atual. Reinício invalida `PendingUse`. Consumo e
intenção pertencem à mesma operação local; falha bloqueia a chamada.

## Tentativa de Chamada

| Campo | Tipo | Regra |
|-------|------|-------|
| `attemptId` | UUID | Chave de idempotência imutável. |
| `requestId`, `preflightRevisionId` | UUID | Vínculos obrigatórios. |
| `state` | enum | Estado abaixo. |
| `sequence` | inteiro crescente | Rejeita evento antigo ou duplicado. |
| `dialEmissionCount` | 0 ou 1 | Nunca excede 1. |
| `startedAtUtc`, `endedAtUtc` | instantes opcionais | Fim só com estado terminal observado. |
| `observableOutcome` | enum opcional | Resultado demonstrado, não inferido. |
| `reconciliationReason` | código opcional | Sem conteúdo sensível. |

```text
Draft → PreflightReady → AwaitingConfirmation
AwaitingConfirmation → Authorized → DialIntentPersisted → DialActionIssued
DialActionIssued → ObservingOutcome → Connected → DialogueActive
ObservingOutcome → NotAnswered | OutcomeUnknown
DialogueActive → HandoffPending → HumanControlled
DialogueActive → Ending → Ended
HandoffPending → HumanControlled | Ending
OutcomeUnknown → Reconciling → ResolvedNoCall | ResolvedCallOccurred | Blocked
qualquer falha antes do efeito → Blocked
```

`ResolvedNoCall` não autoriza retry. Estados incertos, reconciliação e conteúdo ilegível bloqueiam
nova discagem.

## Decisão de Guardrail

| Campo | Tipo | Regra |
|-------|------|-------|
| `decisionId`, `attemptId` | UUID | Correlação. |
| `inputIntent` | código | Sem texto bruto no log. |
| `result` | enum | `Allow`, `Refuse`, `Handoff`, `End`. |
| `policyRule` | código | Regra determinística. |
| `allowedFactIds` | lista | Somente em `Allow`. |
| `createdAtUtc` | instante | Auditoria. |

Entrada desconhecida equivale a `Handoff`. Modelo, simulador e interlocutor nunca alteram política,
concessões ou estado diretamente.

## Transcrição e proposta

`UntrustedTranscriptSegment` existe apenas em memória. `RedactedTranscriptSegment` contém texto
liberado ou `[DADO REDIGIDO]`, classificação, falante e instante; somente este é aceito pelos
escritores. A consolidação examina fronteiras entre fragmentos. Áudio não é entidade persistida.

A proposta tem empresa/atendente, entendimento, testes, total e composição, condições, prazos,
garantias, validade, atendimento, protocolo e próximos passos. Cada valor tem origem
`CallerConfirmed`, `CallerEstimate` ou `AgentInference`. Não existe estado de aceite.

## Transferência Humana

| Campo | Tipo | Regra |
|-------|------|-------|
| `handoffId`, `attemptId` | UUID | Correlação com transferência atual. |
| `reason` | código | Dúvida, proibido, Carlos, recusa de IA ou decisão humana. |
| `startedAtUtc` | instante | Auditoria civil. |
| `startedTimestamp` | monotônico | Base do prazo. |
| `voiceStoppedAtTimestamp` | monotônico opcional | Mede SC-004. |
| `deadline` | duração | 10 segundos. |
| `confirmation` | objeto opcional | Carlos/evidência, correlacionada. |
| `outcome` | enum | `Pending`, `TakenOver`, `TimedOut`, `Failed`, `SafelyEnded`. |

O prazo não reinicia. Confirmação anterior ao limite prevalece mesmo processada depois; confirmação
tardia não reativa automação. Takeover de Carlos interrompe voz antes de aviso.

## Registro, manifesto e retenção

O Markdown segue `records/calls/YYYY-MM-DD/HHMM-destino.md`. O manifesto na mesma área contém esquema,
`attemptId`, `endedAtUtc`, `expiresAtUtc`, caminhos relativos e estado de retenção, sem replicar
telefone ou transcrição.

`expiresAtUtc = endedAtUtc + 7 × 24 horas`. Preservar em `nowUtc < expiresAtUtc`; excluir em
`nowUtc >= expiresAtUtc`, somente após comprovar raiz, manifesto, alvo e ausência de reparse point.
Falha mantém produção bloqueada. Artefato sem fim confirmado não recebe vencimento inventado.

## Estado antirrepetição

Fica fora de `records/calls/` para sobreviver à retenção da transcrição. Contém apenas IDs, revisão,
sequência, intenção, resultado codificado e instantes. Não contém destino, telefone, nomes, texto,
proposta nem hash desses valores.
