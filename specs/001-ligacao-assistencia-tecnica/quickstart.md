# Guia de validação: Ligação para Assistência Técnica

Este guia será executável após a implementação. Nenhum cenário realiza chamada real, usa Phone Link,
grava áudio, lê valores reais de `.env` ou acessa serviço externo.

## Pré-requisitos

- Windows com SDK e Desktop Runtime .NET 10;
- raiz do repositório;
- `JOANA_MODE` ausente ou `Simulation`;
- fixtures sintéticas em `tests/Joana.Acceptance/Fixtures/`;
- raiz temporária exclusiva fornecida pelo runner.

Não preencher `.env` para testes; referências sintéticas são injetadas em memória.

## Diagnóstico

```powershell
dotnet --list-sdks
dotnet --list-runtimes
dotnet build Joana.sln --no-restore
```

Espera-se SDK/runtime 10 e compilação sem download. Se faltar assets local, executar
`dotnet restore Joana.sln --ignore-failed-sources`; sem `PackageReference`, não deve instalar pacote
externo.

## Regressão completa

```powershell
dotnet run --project tests/Joana.Acceptance -- --suite all --mode Simulation
```

Resultado: JSON redigido, zero falhas e exit code 0. Deve cobrir preflight completo/incompleto/alterado;
apresentação literal; diálogo permitido/proibido/ambíguo; ausência de compromisso e divulgação; não
atendimento, incerteza, clique duplo, crash e reinício; takeover em dois segundos; transferência antes,
no timeout de 10 segundos e falha antecipada; redação inclusive entre segmentos; relatório sem áudio;
retenção antes, no e depois de sete dias sem alvo externo.

## Suítes focadas

```powershell
dotnet run --project tests/Joana.Acceptance -- --suite authorization
dotnet run --project tests/Joana.Acceptance -- --suite guardrails
dotnet run --project tests/Joana.Acceptance -- --suite idempotency
dotnet run --project tests/Joana.Acceptance -- --suite handoff
dotnet run --project tests/Joana.Acceptance -- --suite reporting
dotnet run --project tests/Joana.Acceptance -- --suite retention
```

Cada execução cria raiz temporária, imprime IDs/códigos sintéticos e remove só o que criou. Os detalhes
seguem [modelo de dados](data-model.md) e contratos de [interface](contracts/operator-ui.md),
[adaptadores](contracts/adapters.md) e [persistência](contracts/persistence.md).

## Bloqueio de produção

```powershell
dotnet run --project src/Joana.Desktop -- --mode Production --diagnostic
```

Espera-se nenhum adaptador real inicializado, exit code não zero e códigos para fornecedor não aprovado,
Phone Link ausente, áudio não validado, portões não demonstrados e retenção indisponível não resolvida.

## Validação manual simulada

1. Executar `dotnet run --project src/Joana.Desktop -- --mode Simulation`.
2. Confirmar takeover visível e acessível por teclado.
3. Validar preflight, revisão e confirmação com empresa, número e objetivo.
4. Alterar vínculo e verificar invalidação.
5. Acionar takeover durante fala; observar cessação e retorno do headset.
6. Simular transferência sem confirmação; observar encerramento aos 10 segundos.
7. Reiniciar em resultado incerto; verificar bloqueio e ausência de ação.

Registrar só tempos, códigos e resultados sintéticos. Isso não comprova áudio ou Phone Link reais.

## Critério para avançar

Após revisão destes artefatos, executar `$speckit-tasks`. A simulação termina quando regressões e
verificações passam. Chamada real continua proibida até cumprir o `AGENTS.md` e resolver bloqueios por
decisão explícita de Carlos.
