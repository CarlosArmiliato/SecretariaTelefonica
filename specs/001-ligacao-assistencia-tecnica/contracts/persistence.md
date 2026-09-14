# Contrato de persistência local

## Raízes

- `state/attempts/`: estado mínimo antirrepetição, sem conteúdo da chamada.
- `records/calls/`: textos, manifestos, temporários e derivados da chamada.
- `.env`: `CARLOS_NAME` e `CARLOS_PHONE`, nunca copiados a estado, relatório ou log.

`state/`, `records/` e `.env` ficam no `.gitignore`; `.env.example` só tem placeholders.

## Intenção de discagem

1. adquirir bloqueio exclusivo;
2. validar revisão, confirmação, supervisão, modo e observação;
3. criar `state/attempts/{attemptId}.intent.json` com `CreateNew`;
4. gravar esquema, IDs, sequência, estado e instante; executar flush durável;
5. consumir autorização no snapshot;
6. chamar `DialOnceAsync` uma vez;
7. persistir retorno e reobservar; incerteza após o passo 4 entra em reconciliação.

Arquivo existente, truncado ou incompatível bloqueia emissão. Não se apaga intenção para tentar de novo.
Falha antes do passo 4 invalida a confirmação. O protocolo garante no máximo uma emissão automática,
aceitando indisponibilidade; não promete atomicidade entre disco e clique ou falha de hardware.

## Snapshots e logs

Snapshots versionados usam temporário no mesmo diretório, flush e substituição. Falha conserva a versão
anterior e bloqueia efeitos. Logs têm instante, ID opaco, código, estado anterior/novo e resultado, sem
telefone, nome, texto, proposta ou hash.

## Relatório

O escritor recebe `RedactedCallReport` e cria
`records/calls/YYYY-MM-DD/HHMM-destino.md`. O slug não inclui telefone/dado pessoal; colisão ganha
sufixo opaco e nunca sobrescreve. O relatório cobre FR-027. Nome/telefone de Carlos ficam como referências
de configuração; a apresentação literal prescrita pode constar na transcrição.

## Retenção

Manifesto contém esquema, `attemptId`, `endedAtUtc`, `expiresAtUtc` e lista fechada de alvos.
A rotina comprova caminho canônico sob `records/calls/`; rejeita `..`, absoluto, link, junction e
reparse point; preserva antes do vencimento; exclui a lista no vencimento sem recursão genérica; verifica
a ausência e registra data, alvo relativo e resultado.

Ambiguidade preserva o alvo e gera falha crítica. A rotina roda antes de abrir registros, no início e
enquanto o processo estiver disponível. Se o computador ficar desligado além do vencimento, a limpeza
posterior registra violação e bloqueia produção, sem alegar conformidade retroativa.

## Testes

Cobrir disco cheio, crash antes/depois da intenção, truncamento, duas instâncias, clique duplo, replay,
retorno perdido, relógio alterado, limite de sete dias, arquivo ocupado, manifesto inválido, alvo externo
e reparse point. Testes só excluem dentro da raiz temporária criada pelo runner.
