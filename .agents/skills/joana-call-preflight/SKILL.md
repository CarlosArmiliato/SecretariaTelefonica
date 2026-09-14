---
name: joana-call-preflight
description: Collect and review one technical-support call preflight before an operator may request a simulated dial confirmation.
---

Use this skill only to prepare one simulated technical-support call in Portuguese brasileiro. Do not dial, use Phone Link, read `.env`, access the network, add dependencies, or make a real-call attempt.

1. Colete, sem inferir nem preencher lacunas, a solicitação completa: empresa e número; objetivo; equipamento e problema; fatos aprovados; testes, mensagens de erro ou a declaração explícita `Nenhum teste`; faixa/teto de preço e se pode ser divulgado; disponibilidade e restrições; perguntas; concessões de divulgação; critérios de sucesso; e gatilhos de transferência. Modelo e número de série exigem concessões separadas. O preço pode ser usado internamente sem ser revelado quando `mayDisclose` for falso.
2. Valide pelo fluxo da aplicação (`CallRequest.Validate`/`PreflightCoordinator.Review`). Campos ausentes, vazios, conflitantes ou ambíguos bloqueiam a progressão e devem gerar perguntas objetivas a Carlos. Não trate “prosseguir” como resolução. Só aceite a revisão quando o código produzir uma `PreflightRevision` nova e imutável; nunca altere uma revisão já apresentada.
3. Apresente a revisão em modo somente leitura, incluindo empresa, número, objetivo, equipamento, problema, fatos e testes relatáveis, política de preço, disponibilidade, perguntas, dados explicitamente divulgáveis, critérios de sucesso e gatilhos de transferência. Informe que a chamada permanece simulada e sob supervisão de Carlos.
4. Imediatamente antes da única tentativa, solicite confirmação explícita de Carlos mostrando novamente, juntos, empresa/destino, número e objetivo. A confirmação deve ser vinculada à tentativa e à revisão atuais, com supervisão presente, e consumida uma única vez pelo código. Recusa, ausência de confirmação, alteração de qualquer vínculo, reinício, estado incerto ou nova tentativa invalidam o fluxo e exigem novo preflight e nova confirmação.

Os bloqueios determinísticos permanecem no domínio, na aplicação e nos testes: esta skill não autoriza divulgação, compromisso, retry, voicemail, chamada real ou bypass de `DialAuthorization`/`CallAttemptCoordinator`. Após a confirmação, entregue o controle ao fluxo simulado; não execute a discagem por conta própria.
