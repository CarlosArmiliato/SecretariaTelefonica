---
description: "Tarefas de implementação do MVP simulado de ligação para assistência técnica"
---

# Tasks: Ligação para Assistência Técnica

**Input**: Design documents from `/specs/001-ligacao-assistencia-tecnica/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Obrigatórios porque a especificação e a constituição exigem regressões para cenários
permitidos, proibidos e ambíguos em mudanças de discagem, dados, transferência, gravação e compromissos.

**Organization**: As tarefas são agrupadas por história de usuário. Cada história termina em um
incremento testável exclusivamente com simuladores e dados sintéticos.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: pode ser executada em paralelo após suas dependências, em arquivos distintos
- **[Story]**: história coberta pela tarefa
- Todos os caminhos são relativos à raiz do repositório

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Inicializar a solução .NET 10 sem pacotes externos e estabelecer os limites de arquivos.

- [x] T001 Criar `Joana.sln` e os projetos .NET 10 `src/Joana.Domain/Joana.Domain.csproj`, `src/Joana.Application/Joana.Application.csproj`, `src/Joana.Infrastructure/Joana.Infrastructure.csproj`, `src/Joana.Simulators/Joana.Simulators.csproj`, `src/Joana.Desktop/Joana.Desktop.csproj` e `tests/Joana.Acceptance/Joana.Acceptance.csproj` sem PackageReference
- [x] T002 Configurar referências unidirecionais entre os projetos e registrar todos os projetos em `Joana.sln`, mantendo `src/Joana.Domain/Joana.Domain.csproj` independente de WPF, infraestrutura e simuladores
- [x] T003 [P] Atualizar `.gitignore` para excluir `.env`, `state/`, `records/`, saídas .NET e criar `.env.example` somente com placeholders `CARLOS_NAME` e `CARLOS_PHONE`
- [x] T004 [P] Configurar nullable, warnings como erros, análise do SDK, C# 14 e formatação determinística em `Directory.Build.props` e `.editorconfig`

**Checkpoint**: solução vazia compila localmente com o SDK .NET 10 sem baixar dependências.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Criar os controles e portas compartilhados que bloqueiam todas as histórias.

**CRITICAL**: Nenhuma história começa antes desta fase estar completa.

- [x] T005 [P] Implementar IDs opacos, resultados, erros redigidos e relógios UTC/monotônico injetáveis em `src/Joana.Domain/Primitives/DomainPrimitives.cs` e `src/Joana.Application/Ports/ITimeSource.cs`
- [x] T006 [P] Implementar a máquina de estados da tentativa, sequência crescente, estados terminais e regra `dialEmissionCount` restrita a 0 ou 1 em `src/Joana.Domain/Calls/CallAttempt.cs`
- [x] T007 [P] Definir decisões `Allow`, `Refuse`, `Handoff`, `End`, categorias proibidas e falha fechada para intenção desconhecida em `src/Joana.Domain/Guardrails/GuardrailDecision.cs`
- [x] T008 Definir as portas `IPhoneLinkAdapter`, `IAudioControlAdapter`, `IDialogueAdapter`, `ITranscriptionAdapter`, `IRedactionService`, `IAttemptStore` e `ICallReportStore` conforme `specs/001-ligacao-assistencia-tecnica/contracts/adapters.md` em `src/Joana.Application/Ports/CallPorts.cs`
- [x] T009 Implementar escrita JSON versionada com temporário no mesmo diretório, flush durável, substituição e bloqueio exclusivo de processo em `src/Joana.Infrastructure/Persistence/AtomicJsonFileStore.cs`
- [x] T010 Implementar composição exclusiva de simulação e rejeição `ProductionUnavailable` sem inicializar adaptador real em `src/Joana.Desktop/Composition/ApplicationComposition.cs`
- [x] T011 Criar runner de regressão com seleção `--suite`, asserções explícitas, saída JSON redigida e exit code não zero em `tests/Joana.Acceptance/Program.cs` e `tests/Joana.Acceptance/Support/ScenarioRunner.cs`

**Checkpoint**: núcleo compila, produção falha fechada e runner vazio executa sem acessar Phone Link, áudio, rede ou `.env`.

---

## Phase 3: User Story 1 — Preparar e autorizar a chamada (Priority: P1) 🎯 MVP

**Goal**: Carlos consegue preencher, revisar e confirmar uma única tentativa simulada; dados incompletos,
alterações, repetição e incerteza impedem discagem.

**Independent Test**: executar a suíte `authorization` com preflights completos, incompletos e alterados,
confirmação aceita/recusada, clique duplo, duas instâncias, falhas e reinício; somente um cenário válido
emite uma ação simulada, nunca mais de uma.

### Tests for User Story 1

- [x] T012 [P] [US1] Criar casos inicialmente falhos para campos obrigatórios, “nenhum” explícito, revisão imutável e invalidação após alterar empresa, número ou objetivo em `tests/Joana.Acceptance/Cases/Authorization/PreflightCases.cs`
- [x] T013 [P] [US1] Criar casos inicialmente falhos para confirmação de uso único, clique duplo, replay, duas instâncias, crash antes/depois da intenção, disco cheio, arquivo truncado, retorno perdido e reinício sem retry em `tests/Joana.Acceptance/Cases/Authorization/IdempotencyCases.cs`
- [x] T014 [P] [US1] Criar casos inicialmente falhos para os comandos `ReviewPreflight`, `RequestDialConfirmation`, `ConfirmAndDial` e `CancelDial`, incluindo bloqueio fora de simulação e supervisão ausente, em `tests/Joana.Acceptance/Cases/Authorization/OperatorCommandCases.cs`

### Implementation for User Story 1

- [x] T015 [P] [US1] Implementar `CallRequest`, `PreflightRevision`, fatos, testes, política de preço, disponibilidade, perguntas, concessões separadas para modelo/série, critérios de sucesso e gatilhos de transferência em `src/Joana.Domain/Calls/Preflight.cs`; `questions` e `successCriteria` devem ser listas não vazias e “nenhum teste” deve ser explícito
- [x] T016 [US1] Implementar validação completa/ambígua e geração de revisão imutável com novo `preflightRevisionId` em `src/Joana.Application/Orchestration/PreflightCoordinator.cs`
- [x] T017 [US1] Implementar `DialAuthorization` de uso único vinculada a `attemptId`, revisão, empresa, número, objetivo e supervisão, com digest somente em memória e invalidação em mudança/reinício, em `src/Joana.Domain/Calls/DialAuthorization.cs`
- [x] T018 [US1] Implementar intenção com `FileMode.CreateNew`, consumo antes do efeito, snapshot mínimo sem telefone/nome/texto/hash e reconciliação de estado ilegível ou incerto em `src/Joana.Infrastructure/Persistence/FileAttemptStore.cs`
- [x] T019 [P] [US1] Implementar simulador Phone Link com tokens de observação descartáveis, contador de emissão e injeção de foco, destino, não atendimento, timeout, retorno perdido e falha de encerramento em `src/Joana.Simulators/PhoneLink/SimulatedPhoneLinkAdapter.cs`
- [x] T020 [US1] Implementar o ciclo observar–validar–persistir intenção–emitir uma vez–reobservar e bloquear retry em `src/Joana.Application/Orchestration/CallAttemptCoordinator.cs`
- [x] T021 [US1] Implementar estado e comandos de preflight, revisão e confirmação final com revalidação no controlador em `src/Joana.Desktop/ViewModels/CallPreparationViewModel.cs`
- [x] T022 [US1] Construir formulário, erros, revisão somente leitura e diálogo final com empresa, número e objetivo em `src/Joana.Desktop/Views/CallPreparationView.xaml` e `src/Joana.Desktop/Views/DialConfirmationDialog.xaml`
- [x] T023 [US1] Registrar serviços da US1 somente na composição de simulação e executar a suíte `authorization` em `src/Joana.Desktop/Composition/ApplicationComposition.cs` e `tests/Joana.Acceptance/Program.cs`
- [x] T024 [US1] Criar a skill operacional após a implementação, preservando guardrails no código e testes, em `.agents/skills/joana-call-preflight/SKILL.md`

**Checkpoint**: US1 funciona isoladamente e entrega o MVP de preparação/autorização sem chamada real.

---

## Phase 4: User Story 2 — Conduzir a consulta sem assumir compromisso (Priority: P2)

**Goal**: Em chamada simulada atendida, Joana se identifica, usa somente fatos autorizados, coleta a
proposta e bloqueia pedidos proibidos ou ambíguos sem aceitar compromissos.

**Independent Test**: executar `guardrails` e `dialogue`; a apresentação é a primeira fala, fatos e
dados não autorizados nunca saem, propostas são coletadas sem aceite e não atendimento encerra sem retry.

### Tests for User Story 2

- [x] T025 [P] [US2] Criar casos inicialmente falhos para apresentação literal na primeira fala, fidelidade aos fatos, teto não divulgável, permissões separadas e instrução maliciosa do interlocutor em `tests/Joana.Acceptance/Cases/Dialogue/DisclosureCases.cs`
- [x] T026 [P] [US2] Criar casos inicialmente falhos para categorias proibidas/ambíguas, recusa de IA, pedido de Carlos, proposta completa sem aceite e não atendimento sem voicemail/retry em `tests/Joana.Acceptance/Cases/Dialogue/DialogueCases.cs`

### Implementation for User Story 2

- [x] T027 [P] [US2] Implementar `ServiceProposal` com todos os campos opcionais da proposta, sem estado de aceite, e origem obrigatória `CallerConfirmed`, `CallerEstimate` ou `AgentInference` em `src/Joana.Domain/Calls/ServiceProposal.cs`
- [x] T028 [US2] Implementar política determinística para divulgação, emergência, saúde, finanças, pagamentos, autenticação, documentos, jurídico, penalidades, segurança e compromissos em `src/Joana.Domain/Guardrails/DialoguePolicy.cs`
- [x] T029 [P] [US2] Implementar roteiros estruturados de atendida, não atendida, permitida, proibida e ambígua sem geração livre em `src/Joana.Simulators/Dialogue/SimulatedDialogueAdapter.cs`
- [x] T030 [US2] Implementar orquestração que emite primeiro a apresentação literal, expõe apenas `allowedFactIds`, coleta proposta com origem e transfere em dúvida em `src/Joana.Application/Orchestration/DialogueCoordinator.cs`
- [x] T031 [US2] Exibir roteiro, fatos liberados, campos coletados e estado bloqueado sem controles de aceite, pagamento, compra ou agenda em `src/Joana.Desktop/ViewModels/DialogueViewModel.cs` e `src/Joana.Desktop/Views/DialogueView.xaml`
- [x] T032 [US2] Criar a skill operacional após a implementação, mantendo decisões de segurança determinísticas fora da skill, em `.agents/skills/joana-tech-support-dialogue/SKILL.md`

**Checkpoint**: US2 é demonstrável com simulador e não amplia a autoridade concedida na US1.

---

## Phase 5: User Story 3 — Carlos assumir ou encerrar com segurança (Priority: P3)

**Goal**: Carlos interrompe a automação com prioridade; transferências são confirmadas em até 10 segundos
ou encerradas com a frase fixa e resultado observado.

**Independent Test**: executar `handoff` com voz enfileirada, geração travada, persistência lenta,
confirmação antes/no/depois do limite, falha antecipada e falha de hangup; takeover cessa voz e devolve
áudio observado em até dois segundos.

### Tests for User Story 3

- [x] T033 [P] [US3] Criar casos inicialmente falhos de takeover durante fala, áudio enfileirado, geração travada e persistência bloqueada, medindo voz cessada e ambos os sentidos do headset em até 2 segundos, em `tests/Joana.Acceptance/Cases/Handoff/TakeoverCases.cs`
- [x] T034 [P] [US3] Criar casos inicialmente falhos para confirmação correlacionada em 9,999 s, no limite e depois, timer não renovável, confirmação antiga, falha antecipada e encerramento não observado em `tests/Joana.Acceptance/Cases/Handoff/HandoffDeadlineCases.cs`

### Implementation for User Story 3

- [x] T035 [P] [US3] Implementar `HumanHandoff` com `handoffId`, motivo, UTC, timestamp monotônico, voz parada, prazo constante de 10 segundos, confirmação correlacionada e outcomes `Pending`, `TakenOver`, `TimedOut`, `Failed`, `SafelyEnded` em `src/Joana.Domain/Handoff/HumanHandoff.cs`
- [x] T036 [P] [US3] Implementar áudio simulado idempotente com cancelamento, limpeza de buffer e observação separada de voz, entrada e saída em `src/Joana.Simulators/Audio/SimulatedAudioControlAdapter.cs`
- [x] T037 [US3] Implementar canal prioritário de takeover, prazo monotônico não renovável, precedência da confirmação válida e frase fixa de encerramento em `src/Joana.Application/Orchestration/HandoffCoordinator.cs`
- [x] T038 [US3] Integrar cancelamento prioritário ao diálogo e impedir retomada por evento tardio ou falha de roteamento em `src/Joana.Application/Orchestration/DialogueCoordinator.cs`
- [x] T039 [US3] Manter takeover visível, nomeado e acessível por teclado e mostrar evidências de voz/áudio/transferência em `src/Joana.Desktop/ViewModels/HandoffViewModel.cs` e `src/Joana.Desktop/Views/HandoffView.xaml`
- [x] T040 [US3] Criar a skill operacional após a implementação, sem substituir confirmação e interrupção do código, em `.agents/skills/joana-human-handoff/SKILL.md`

**Checkpoint**: US3 funciona com qualquer roteiro simulado e falha fechada quando takeover/encerramento não é observável.

---

## Phase 6: User Story 4 — Auditar a chamada sem reter áudio (Priority: P4)

**Goal**: Cada tentativa gera registro textual redigido, completo e local; nenhum áudio é criado e
artefatos vencidos são removidos somente dentro da área permitida.

**Independent Test**: executar `reporting` e `retention` com dado proibido inteiro/fragmentado,
relatório parcial, limites temporais e caminhos adversariais; todo conteúdo sensível é redigido,
nenhum áudio existe e somente alvos inequívocos vencidos são excluídos.

### Tests for User Story 4

- [x] T041 [P] [US4] Criar casos inicialmente falhos para CPF, token, endereço e série inteiros ou divididos entre segmentos, conteúdo em resumo/erro/nome de arquivo e falha do redator em `tests/Joana.Acceptance/Cases/Reporting/RedactionCases.cs`
- [x] T042 [P] [US4] Criar casos inicialmente falhos para relatório completo, distinção de fato/estimativa/inferência, ausência de áudio e colisão de nome sem sobrescrita em `tests/Joana.Acceptance/Cases/Reporting/CallReportCases.cs`
- [x] T043 [P] [US4] Criar casos inicialmente falhos imediatamente antes/no/depois de 7 dias, meia-noite local, relógio alterado, manifesto inválido, fim ausente, arquivo ocupado, alvo externo e reparse point em `tests/Joana.Acceptance/Cases/Retention/RetentionCases.cs`

### Implementation for User Story 4

- [x] T044 [P] [US4] Implementar tipos distintos `UntrustedTranscriptSegment`, somente em memória, e `RedactedTranscriptSegment`, único aceito por escritores, mais `RedactedCallReport` e manifesto em `src/Joana.Domain/Reporting/ReportModels.cs`
- [x] T045 [US4] Implementar consolidação entre fronteiras de segmentos, regras de dados proibidos e fallback integral `[DADO REDIGIDO]` com transferência em falha/dúvida em `src/Joana.Infrastructure/Redaction/DeterministicRedactionService.cs`
- [x] T046 [US4] Implementar relatório Markdown em `records/calls/YYYY-MM-DD/HHMM-destino.md`, slug sem dado pessoal, colisão com sufixo opaco, manifesto e escrita apenas de tipos redigidos em `src/Joana.Infrastructure/Persistence/MarkdownCallReportStore.cs`
- [x] T047 [US4] Implementar expiração `endedAtUtc + 7 × 24 horas`, comparação `nowUtc >= expiresAtUtc`, validação canônica, rejeição de link/junction/reparse point, exclusão não recursiva e auditoria sem conteúdo em `src/Joana.Infrastructure/Persistence/CallRecordRetentionService.cs`
- [x] T048 [US4] Exibir resultado observável, classificações, pendências e caminho relativo do registro sem conteúdo proibido em `src/Joana.Desktop/ViewModels/CallReportViewModel.cs` e `src/Joana.Desktop/Views/CallReportView.xaml`
- [x] T049 [US4] Criar a skill operacional após a implementação, permitindo somente relatório redigido e resultado evidenciado, em `.agents/skills/joana-call-report/SKILL.md`

**Checkpoint**: US4 audita todos os desfechos simulados sem áudio e aplica a retenção segura em relógio controlado.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Integrar as histórias, provar os portões de simulação e documentar bloqueios de produção.

- [x] T050 [P] Implementar diagnóstico de inicialização para modo, fuso `America/Sao_Paulo`, segunda instância, estado incerto, retenção e bloqueios de produção em `src/Joana.Desktop/Composition/StartupDiagnostics.cs`
- [x] T051 [P] Completar fixtures sintéticas permitidas, proibidas, ambíguas e de falhas sem dados reais em `tests/Joana.Acceptance/Fixtures/SimulationScenarios.json`
- [x] T052 Integrar todas as suítes e provar ausência de rede, arquivo de áudio, retry, voicemail, compromisso e adaptador real em `tests/Joana.Acceptance/Cases/CrossCutting/ConstitutionGateCases.cs`
- [x] T053 Executar a regressão completa de `specs/001-ligacao-assistencia-tecnica/quickstart.md` e registrar somente comandos, tempos, códigos e resultados sintéticos em `specs/001-ligacao-assistencia-tecnica/evidence/simulation-results.md`
- [ ] T054 Validar manualmente teclado, foco, nome acessível, takeover visível e responsividade WPF, registrando resultados sintéticos em `specs/001-ligacao-assistencia-tecnica/evidence/operator-ui.md`
- [x] T055 Atualizar `specs/001-ligacao-assistencia-tecnica/quickstart.md` com comandos finais e manter chamadas reais bloqueadas; não criar `joana-phonelink-call` ou `joana-voice-runtime` antes das integrações correspondentes existirem

**Checkpoint**: todas as histórias passam em simulação, com evidência revisável; produção continua indisponível.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1**: começa imediatamente.
- **Phase 2**: depende da Phase 1 e bloqueia todas as histórias.
- **US1 (Phase 3)**: depende da Phase 2 e constitui o MVP.
- **US2 (Phase 4)**: depende da fundação e usa revisão/autorização da US1 para integração; seus guardrails e simulador podem ser desenvolvidos em paralelo depois da fundação.
- **US3 (Phase 5)**: depende da fundação; integração final depende do coordenador de diálogo da US2.
- **US4 (Phase 6)**: modelos/redação podem começar após a fundação; relatório integrado depende dos eventos finais das US1–US3.
- **Phase 7**: depende das histórias incluídas na entrega.

### Within Each User Story

- Escrever os testes da história e confirmar que falham pelo comportamento ainda ausente.
- Implementar modelos antes de coordenadores.
- Implementar adaptadores antes da integração e interface.
- Executar a suíte da história no checkpoint.
- Criar a skill planejada somente depois que o código e testes correspondentes existirem.

### User Story Dependency Graph

```text
Setup → Foundation → US1 (MVP)
                    ├─→ US2 ─→ US3
                    └─→ US4 ←─┘
                              ↓
                            Polish
```

US2 e partes da US4 podem avançar em paralelo após a fundação. US3 pode desenvolver modelo e simulador
em paralelo, mas sua integração usa a interrupção do diálogo. Cada suíte continua executável isoladamente
com doubles para dependências ainda não integradas.

## Parallel Execution Examples

### User Story 1

```text
T012 PreflightCases.cs
T013 IdempotencyCases.cs
T014 OperatorCommandCases.cs
→ T015–T023 implementação sequencial pelas dependências
```

### User Story 2

```text
T025 DisclosureCases.cs  ||  T026 DialogueCases.cs
T027 ServiceProposal.cs  ||  T029 SimulatedDialogueAdapter.cs
→ T028, T030, T031, T032
```

### User Story 3

```text
T033 TakeoverCases.cs  ||  T034 HandoffDeadlineCases.cs
T035 HumanHandoff.cs   ||  T036 SimulatedAudioControlAdapter.cs
→ T037–T040
```

### User Story 4

```text
T041 RedactionCases.cs  ||  T042 CallReportCases.cs  ||  T043 RetentionCases.cs
→ T044 → T045/T046 → T047/T048 → T049
```

## Implementation Strategy

### MVP First

1. Completar Setup e Foundation.
2. Implementar US1 e executar `--suite authorization`.
3. Demonstrar revisão, confirmação vinculada e no máximo uma emissão simulada.
4. Parar no checkpoint sem iniciar chamada real.

### Incremental Delivery

1. **US1**: preparação e autorização seguras.
2. **US2**: conversa e proposta simuladas sem compromisso.
3. **US3**: takeover e encerramento seguro.
4. **US4**: relatório redigido e retenção.
5. **Polish**: portões completos e evidências.

Nenhuma etapa instala dependências, habilita serviço, lê segredo, transmite dado ou adiciona adaptador
real. Qualquer futura produção exigirá nova especificação/planejamento para os bloqueios registrados.

## Notes

- Tarefas `[P]` modificam arquivos distintos e podem rodar em paralelo após dependências.
- Todos os testes automatizados usam composição `Simulation`.
- Fixtures, logs e evidências nunca contêm valores reais.
- Não marcar tarefa como concluída sem evidência observável.
- Não criar skill planejada antes da implementação e dos testes correspondentes.
