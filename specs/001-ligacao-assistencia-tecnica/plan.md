# Implementation Plan: Ligação para Assistência Técnica

**Branch**: `carlos/ft_clarify_ligacao_assistencia` | **Date**: 2026-09-13 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-ligacao-assistencia-tecnica/spec.md`

## Summary

Construir uma aplicação desktop Windows, local e de usuário único, que permita preparar uma tentativa,
validar e revisar o preflight, obter confirmação de uso único e executar toda a conversa por adaptadores
simulados. Um núcleo independente da interface aplica os guardrails, a máquina de estados, a autorização
vinculada à revisão e a prevenção persistente de discagem duplicada. O painel WPF mantém o takeover
visível e prioritário. Registros são redigidos antes da persistência e expiram ao completar sete dias.

Este incremento termina com simulação completa. Produção permanece indisponível até aprovação de
fornecedor, adaptadores reais, roteamento de áudio validado, evidências dos portões de chamada real e
uma solução aprovada para retenção quando o computador estiver indisponível.

## Technical Context

**Language/Version**: C# 14 em .NET 10.0 (SDK local 10.0.401)

**Primary Dependencies**: WPF e bibliotecas da plataforma .NET; sem pacotes externos neste incremento

**Storage**: JSON versionado para estado e Markdown para registro; escrita temporária e substituição no
mesmo diretório; marcador durável de intenção antes de qualquer efeito de discagem

**Testing**: runner de regressão console em .NET, com asserções, relógios e adaptadores simulados;
validação manual complementar da acessibilidade e do painel

**Target Platform**: Windows com .NET 10 Desktop Runtime; Microsoft Phone Link real desabilitado

**Project Type**: aplicação desktop de processo único com bibliotecas de domínio e aplicação,
infraestrutura local, simuladores e executável de testes

**Performance Goals**: cessar voz e devolver áudio simulado em até 2 segundos após takeover; iniciar
encerramento aos 10 segundos sem confirmação; manter o painel responsivo

**Constraints**: simulação por padrão e única composição disponível; uma tentativa ativa; nenhum retry,
áudio gravado, telemetria, sincronização ou serviço externo; dados reais somente em `.env`; falha
fechada diante de estado, foco, dado ou resultado ambíguo

**Scale/Scope**: um operador, computador e telefone pareado; uma tentativa por autorização; poucos
registros locais por no máximo sete dias

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Gate | Before research | After design | Evidence |
|------|-----------------|--------------|----------|
| Identidade transparente e português brasileiro | PASS | PASS | Diálogo começa com apresentação literal e testes validam ordem. |
| Autoridade por chamada e supervisão | PASS | PASS | Confirmação de uso único vinculada à revisão; takeover visível; produção bloqueada. |
| Limites determinísticos | PASS | PASS | Guardrail avaliado antes de fala, dado ou efeito; intenção desconhecida transfere. |
| Privacidade, minimização e retenção | PASS | PASS | Escritores aceitam tipos redigidos; conteúdo em `records/calls/`; expiração em 7 dias. |
| Simulação e evidência | PASS | PASS | Sem composição real; simuladores verificam no máximo uma emissão de discagem. |
| Observar–agir–observar | PASS | PASS | Toda ação invalida a observação e exige nova leitura do estado. |
| Takeover e falha segura | PASS | PASS | Canal prioritário, 10 segundos monotônicos e encerramento fixo. |
| Sem fornecedor/dependência não autorizada | PASS | PASS | Somente runtime instalado; integrações reais são interfaces sem implementação. |

Não há violação constitucional. A retenção durante desligamento é bloqueio de produção, sem enfraquecer
FR-029.

## Project Structure

### Documentation (this feature)

```text
specs/001-ligacao-assistencia-tecnica/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   ├── adapters.md
│   ├── operator-ui.md
│   └── persistence.md
└── tasks.md                 # criado somente por $speckit-tasks
```

### Source Code (repository root)

```text
Joana.sln
src/
├── Joana.Domain/
│   ├── Calls/
│   ├── Guardrails/
│   ├── Handoff/
│   └── Reporting/
├── Joana.Application/
│   ├── Commands/
│   ├── Orchestration/
│   └── Ports/
├── Joana.Infrastructure/
│   ├── Configuration/
│   ├── Persistence/
│   ├── Redaction/
│   └── Time/
├── Joana.Simulators/
│   ├── PhoneLink/
│   ├── Audio/
│   ├── Dialogue/
│   └── Transcription/
└── Joana.Desktop/
    ├── Views/
    ├── ViewModels/
    └── Composition/

tests/
└── Joana.Acceptance/
    ├── Cases/
    ├── Doubles/
    ├── Fixtures/
    └── Program.cs

records/
└── calls/                   # ignorado pelo Git e sujeito à retenção

.env.example                 # placeholders, sem valores reais
```

**Structure Decision**: separar domínio, aplicação, infraestrutura, simuladores e interface impede que
WPF, texto gerado ou automação decidam sobre discagem e divulgação. `Joana.Desktop` referencia a
aplicação; adaptadores implementam portas definidas por ela. O runner reutiliza o núcleo e compõe
exclusivamente simuladores.

## Design Decisions

### Máquina de estados e autorização

Cada alteração cria uma revisão imutável do preflight. A confirmação associa tentativa, revisão,
destino, número e objetivo; alteração ou reinício a invalida. Discagem consome a confirmação e persiste
`DialIntent` antes de chamar o adaptador. Erro ou retorno incerto entra em reconciliação e bloqueia
nova discagem. Mesmo quando nenhuma chamada for comprovada, nova tentativa exige nova autorização.

### Segurança do diálogo

O diálogo simulado usa intenções e fatos estruturados. A política decide `Allow`, `Refuse`,
`Handoff` ou `End` antes da saída. Somente fatos aprovados e dados explicitamente autorizados podem
virar fala. Texto não classificado transfere. Adaptadores não recebem autoridade para compromisso.

### Interrupção e transferência

Takeover entra por comando separado e prioritário. O controlador cancela voz, limpa saída simulada e
devolve entrada/saída ao operador sem aguardar relatório ou diálogo. Transferência iniciada por Joana
usa duração monotônica: confirmação correlacionada antes do limite conclui; falha antecipada ou 10
segundos sem evidência iniciam encerramento. Evento tardio não reativa automação.

### Persistência, redação e retenção

Estado antirrepetição contém IDs opacos, sequência, estado e instantes, sem telefone, nome, transcrição
ou hashes. Conteúdo fica sob `records/calls/` e somente APIs que aceitam tipos redigidos podem
escrevê-lo. Temporários e derivados usam a mesma raiz. Retenção não segue links nem faz exclusão
recursiva genérica; compara `endedAtUtc + 7 dias` e registra alvo relativo, instante e resultado.

### Diagnósticos e liberação

Logs usam códigos e IDs opacos. Estado corrompido, fuso indisponível, relógio inconsistente, erro de
exclusão ou segunda instância bloqueiam efeitos. O executável resolve `America/Sao_Paulo` no início.
Composição de produção só poderá ser adicionada após os portões da especificação.

## Sequência de Implementação

1. Criar solução, projetos, tipos de domínio, relógio injetável e máquina de estados.
2. Implementar guardrails, autorização vinculada à revisão e estado antirrepetição.
3. Implementar redação, relatório, manifesto e retenção segura.
4. Implementar adaptadores simulados e roteiros estruturados.
5. Construir painel WPF com preflight, confirmação, takeover e estado observável.
6. Integrar transferência, timers monotônicos e encerramento seguro.
7. Executar regressões permitidas, proibidas, ambíguas, de falha e limites temporais.
8. Validar acessibilidade, foco e responsividade com dados sintéticos.

## Complexity Tracking

Nenhuma violação constitucional requer exceção.
