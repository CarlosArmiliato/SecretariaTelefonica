# Pesquisa técnica: Ligação para Assistência Técnica

**Data**: 2026-09-13 | **Escopo**: decisões de implementação da simulação.
**Fontes normativas**: [spec.md](spec.md), [constituição](../../.specify/memory/constitution.md), [AGENTS.md](../../AGENTS.md).

## R1 — Runtime e interface local

**Decisão**: C# / .NET 10, WPF no painel Windows e biblioteca de domínio sem dependência da interface.
Usar somente bibliotecas do runtime neste incremento; nenhuma instalação foi feita.
Foram encontrados SDK 10.0.401, runtime WindowsDesktop 10.0.12 e os packs de referência .NET/WindowsDesktop.
Python aparece como alias WindowsApps; sua instalação funcional não foi demonstrada.

**Justificativa**: o runtime Windows já disponível permite um painel persistente de supervisão,
com comandos de teclado e acesso ao domínio sem servidor ou navegador.
WPF exige manter operações demoradas fora da thread da interface. O controlador processará cancelamento
e transferência sem aguardar uma tarefa de diálogo, gravação de relatório ou integração.

**Alternativas consideradas**: Python/Tkinter/SQLite tem testes e banco na biblioteca padrão, mas runtime
e dados de fuso precisariam ser verificados; Electron acrescenta dependências e uma camada de navegador;
console sozinho dificulta manter intervenção visível. Não se presume que WPF resolva áudio ou Phone Link.
Fontes: [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/overview/),
[modelo de threads](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/advanced/threading-model),
[zoneinfo](https://docs.python.org/3/library/zoneinfo.html).

## R2 — Testes sem novos pacotes

**Decisão**: executável de regressão .NET com casos nomeados, asserções explícitas, saída JSON redigida
e código de saída não zero quando qualquer caso falhar. Separar suítes de domínio, contratos, integração
e aceitação. Relógio, falhas de armazenamento e adaptadores são injetáveis.
O painel recebe validação manual de teclado, foco, escala e visibilidade, além dos testes de comandos.

**Justificativa**: evita instalar um framework de testes sem aprovação. O runner é uma camada pequena,
sem descoberta por reflexão, plugins ou paralelismo complexo. Casos verificam resultados observáveis,
incluindo contagem de discagens, efeitos proibidos e recuperação entre processos.
Não apresentar esse runner como execução de dotnet test.

**Alternativas consideradas**: MSTest/xUnit e Microsoft.Data.Sqlite podem ser reavaliados se Carlos
aprovar dependências; não são pré-requisitos silenciosos deste plano.
Fontes: [testes .NET](https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-dotnet-test),
[SQLite para .NET](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/).

## R3 — Persistência e prevenção de duplicidade

**Decisão**: JSON versionado e Markdown; processo único com bloqueio exclusivo de arquivo.
Criar marcador de intenção por tentativa com CreateNew e Flush(true) antes de chamar o adaptador.
A existência do marcador, mesmo incompleto, impede nova discagem daquela tentativa.
Snapshots são escritos em temporário no mesmo diretório, sincronizados e substituídos; falhas nunca
autorizam continuar. Estado ilegível ou intenção sem resultado conduz a reconciliação somente leitura.

**Justificativa**: para uma tentativa ativa e um operador, arquivos locais permitem evitar pacote
de banco. Um clique na interface não participa de transação com arquivo: a garantia pretendida é
no máximo uma emissão por tentativa, admitindo que nenhuma chamada seja feita quando há falha.
Não se promete execução exatamente uma vez nem durabilidade absoluta sob falha de hardware.

**Alternativas consideradas**: estado somente em memória perde proteção em reinício; repetir após timeout
pode duplicar uma chamada; SQLite simplifica transações internas, mas não torna o clique transacional.
Fontes: [FileMode.CreateNew](https://learn.microsoft.com/en-us/dotnet/api/system.io.filemode?view=net-10.0),
[Flush](https://learn.microsoft.com/en-us/dotnet/api/system.io.filestream.flush?view=net-10.0),
[File.Replace](https://learn.microsoft.com/en-us/dotnet/api/system.io.file.replace?view=net-10.0).
O protocolo completo e suas restrições são decisões deste projeto, a validar por injeção de falhas.

## R4 — Tempo, transferência e fuso

**Decisão**: TimeProvider injetável; duração monotônica para os 10 segundos e medição dos 2 segundos;
DateTimeOffset UTC para persistência, convertido por TimeZoneInfo para America/Sao_Paulo no relatório.
Os 10 segundos começam ao iniciar a transferência. Falha antecipada inicia encerramento sem espera.
Uma confirmação recebida antes do prazo prevalece sobre timer processado mais tarde; evento no prazo
ou depois não reativa a automação. Intervenção humana continua prioritária.

**Justificativa**: ajuste do relógio civil não deve alongar timeout. Reinício invalida confirmações e
timers anteriores; não se restaura autorização para discar ou falar.
O fuso foi resolvido no PowerShell do host; o executável final deve repetir o diagnóstico no próprio runtime.
Se a resolução falhar, bloquear geração operacional de registros, sem substituir por UTC-03 fixo.

**Alternativas consideradas**: relógio civil para durações é vulnerável a ajustes; offset fixo não representa
o fuso. Fontes: [TimeProvider](https://learn.microsoft.com/en-us/dotnet/api/system.timeprovider?view=net-10.0),
[TimeZoneInfo](https://learn.microsoft.com/en-us/dotnet/api/system.timezoneinfo.findsystemtimezonebyid?view=net-10.0).

## R5 — Diálogo limitado e dados

**Decisão**: diálogo simulado por intenções e fatos estruturados; saída somente por templates revisados e
referências a fatos aprovados. Nenhum modelo externo ou texto gerado livremente pode alcançar voz,
discagem ou armazenamento diretamente. Entrada livre não reconhecida provoca transferência.

**Justificativa**: uma lista de palavras proibidas não prova segurança semântica. O controle determinístico
autoriza ações e dados conhecidos; dúvida falha de modo fechado. A simulação valida o protocolo, não
a compreensão irrestrita de fala real.

Redigir antes de persistir, inclusive preflight, erros, proposta e texto citado. Fragmentos de transcrição
aguardam consolidação em memória para evitar vazamento de identificadores partidos. Conteúdo não
classificável é inteiramente substituído por [DADO REDIGIDO]. Nome/telefone de Carlos aparecem em
disco somente em .env; em relatórios usar referências CARLOS_NAME/CARLOS_PHONE, nunca seus valores,
mesmo se a divulgação na conversa foi autorizada. A apresentação obrigatória com o nome literal Carlos
é uma exceção textual prescrita pelo contrato de identidade, não uma cópia do valor do ambiente.

**Alternativas consideradas**: regex isolada, persistência bruta para redação posterior, prompt de segurança
como único controle e fornecedor presumido foram rejeitados. Base: FR-008 a FR-016 e FR-024 a FR-030.

## R6 — Retenção e limites de produção

**Decisão**: expiração por chamada = encerramento UTC + 7 dias corridos de 24 horas; remover ao atingir
o limite, com relógio controlado nos testes. Todos os artefatos contendo conteúdo da chamada ficam
em records/calls/; sem cópias em logs, backups, Git ou telemetria. Incluir temporários e relatório parcial.
A rotina valida raiz, identidade e metadados antes de excluir; não segue links, junctions ou reparse points.

**Justificativa**: nem fechamento do app nem relógio do computador provam disponibilidade contínua.
Um PC desligado não executa exclusão física. Limpeza no próximo início é recuperação e não demonstra
cumprimento do máximo de sete dias. Relógio duvidoso, registro sem encerramento verificável ou erro de
exclusão também impedem afirmar conformidade. Preservar alvo ambíguo e sinalizar falha, sem exclusão precoce.

**Alternativas consideradas**: agendador/serviço local exige aprovação e ainda não executa com PC desligado;
apagar chave criptográfica não equivale automaticamente à exclusão exigida; apagar por mtime é inseguro.
Não se altera a constituição para acomodar essas limitações.

**Consequência**: concluir e testar a simulação com dados sintéticos. Produção permanece bloqueada até
Carlos definir uma política operacional viável ou aprovar formalmente eventual emenda normativa.
A geração das tarefas de simulação não depende dessa decisão; a habilitação de chamadas reais depende.

## R7 — Integrações e liberação

**Decisão**: Phone Link, entrada/saída de áudio, transcrição e voz são interfaces com implementações
simuladas. A composição de produção retorna ProductionUnavailable mesmo quando todos os campos de
preflight estão preenchidos. Nenhum fornecedor, custo ou processamento externo foi autorizado.

**Justificativa**: portar de simulação para telefone real exige evidência de roteamento para o headset,
interrupção, latência, ausência de gravação, observação após cada ação e encerramento seguro.
Uma propriedade chamada audioReturned no simulador não comprova roteamento físico.

**Alternativas consideradas**: API pública presumida, cliques por coordenadas fixas e voz externa padrão
não atendem às instruções do projeto. Autenticação, pareamento e permissões continuam ações de Carlos.

## Conclusão da pesquisa

As decisões necessárias para construir a simulação estão resolvidas. Produção é uma capacidade desabilitada
com bloqueios explícitos, não uma integração parcialmente autorizada. O planejamento não comprova
ainda qualquer critério de aceitação; as evidências deverão ser produzidas pela implementação.
