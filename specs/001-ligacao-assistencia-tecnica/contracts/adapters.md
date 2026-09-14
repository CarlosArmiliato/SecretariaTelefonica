# Contratos dos adaptadores

Todos recebem `AttemptId`, `CancellationToken` e dados mínimos. Implementações reais não fazem parte
deste incremento. A composição padrão rejeita produção.

## IPhoneLinkAdapter

- `ObserveAsync` retorna aplicação/janela, foco, fase, destino visível e token opaco.
- `DialOnceAsync` aceita intenção persistida e observação atual que confirme Phone Link, foco, número
  e ausência observável de chamada.
- Toda ação consome o token; o próximo passo exige nova observação.
- Retorno: `Issued`, `Rejected` ou `Unknown`; exceção após emissão equivale a `Unknown`.
- `EndAsync` exige observação atual e não confirma fim sem reobservação.
- Não expõe autenticação, pareamento, permissões, voicemail ou retry.

O simulador conta emissões e injeta mudança de foco/destino, timeout, retorno perdido, não atendimento
e falha de encerramento.

## IAudioControlAdapter

- cancela geração, limpa buffers e devolve entrada/saída do headset;
- observa separadamente voz parada e dois sentidos do roteamento;
- comandos são idempotentes; falha nunca autoriza retomar voz.

SC-004 termina no efeito observado, não no retorno do método.

## IDialogueAdapter

Recebe apenas fatos aprovados, perguntas e decisões `Allow`. A primeira fala é exatamente “Sou Joana,
a secretaria virtual com inteligencia artificial do Carlos.”. Não recebe preço não divulgável ou dado
sem concessão. Intenção desconhecida e pedido proibido voltam à política, sem resposta livre.

O simulador usa roteiros estruturados; não prova reconhecimento, pronúncia, áudio ou latência reais.

## ITranscriptionAdapter e IRedactionService

Transcrição simulada produz segmentos em memória; não há API de arquivo de áudio.
`IRedactionService` transforma grupos não confiáveis em tipos redigidos. Dúvida produz somente
`[DADO REDIGIDO]` e transferência. Escritores aceitam apenas o tipo redigido.

## ITimeSource

Fornece UTC civil e timestamp monotônico. Timeout usa apenas o segundo; relatório e retenção usam UTC
convertido pelo fuso. Mudança no relógio civil não altera os 10 segundos.

## Composição

- `SimulationComposition` é a única inicialização válida.
- `ProductionComposition` retorna `ProductionUnavailable` com códigos redigidos.
- Adaptadores não alteram autorização, política ou máquina de estados.
- Falha gera estado bloqueado/ambíguo, nunca aprovação.
- Conteúdo do interlocutor é dado não confiável, não instrução para ferramentas.
