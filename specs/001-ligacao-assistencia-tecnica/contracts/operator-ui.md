# Contrato do painel do operador

## Superfícies

O painel WPF apresenta em português brasileiro: preflight e erros; revisão imutável; confirmação final
com empresa, número e objetivo; estado e última evidência; takeover sempre visível e acessível por
teclado; estado de voz/áudio/transferência; resultado e caminho relativo do registro.

## Comandos

| Comando | Pré-condições | Resultado |
|---------|---------------|-----------|
| `ReviewPreflight` | campos resolvidos | cria revisão imutável. |
| `RequestDialConfirmation` | revisão válida, simulação e supervisão | mostra vínculo final. |
| `ConfirmAndDial` | confirmação aberta e vínculo igual | consome confirmação e emite no máximo uma ação simulada. |
| `CancelDial` | confirmação não consumida | invalida sem efeito. |
| `TakeOver` | tentativa não terminal | interrompe voz e devolve áudio com prioridade. |
| `ConfirmHandoff` | transferência atual pendente | conclui apenas o `handoffId` atual. |
| `EndSafely` | chamada sem controle humano | encerra e confirma por observação. |

`ConfirmAndDial` fica indisponível fora de simulação, com estado incerto, segunda instância, mudança
de preflight, supervisão ausente ou falha de armazenamento. O controlador repete as validações.

## Prioridade e acessibilidade

`TakeOver` tem atalho, foco, nome acessível e ação estável. Seu handler não espera diálogo, transcrição
ou relatório. O painel mostra voz cessada e áudio devolvido; o cronômetro mede o efeito observado.
Erros não exibem payloads. Não há retry, voicemail, aceite, compra, pagamento ou agenda final.

## Cenários

- Preflight incompleto não chega à confirmação.
- Alteração do vínculo fecha a confirmação.
- Clique/tecla repetida produz uma emissão.
- Takeover durante fala interrompe em até dois segundos e descarta áudio pendente.
- Transferência sem confirmação encerra aos 10 segundos; falha antecipada antecipa.
- Reinício não restaura confirmação nem executa ação.
