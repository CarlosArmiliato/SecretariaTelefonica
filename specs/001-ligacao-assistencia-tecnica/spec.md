# Feature Specification: Ligação para Assistência Técnica

**Feature Branch**: `main`

**Created**: 2026-09-11

**Status**: Draft

**Input**: User description: "MVP de ligação sob demanda para assistência técnica, com preflight,
confirmação imediata antes de ligar, diálogo limitado à coleta de orçamento, supervisão e takeover
humano, transcrição local e guardrails do Projeto Joana."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Preparar e autorizar a chamada (Priority: P1)

Carlos informa a assistência, o equipamento, o problema e os resultados esperados. Joana reúne os
dados em um preflight, aponta ausências ou ambiguidades e apresenta o resumo para revisão. Somente
quando Carlos confirma explicitamente, imediatamente antes da discagem, a tentativa fica autorizada.

**Why this priority**: Sem um escopo completo e uma autorização específica não existe chamada
segura. O preflight isolado já entrega valor ao transformar a intenção de Carlos em instruções
revisáveis, mesmo que nenhuma chamada seja feita.

**Independent Test**: Pode ser testada integralmente em simulação fornecendo preflights completos,
incompletos e ambíguos e verificando que somente um preflight completo chega ao pedido final de
confirmação, sem efetuar chamada.

**Acceptance Scenarios**:

1. **Given** uma solicitação com todos os campos obrigatórios, **When** Joana apresenta o preflight,
   **Then** Carlos vê destino, número, objetivo, fatos, dados divulgáveis, perguntas, limites e
   critérios de transferência antes de ser solicitado a confirmar.
2. **Given** um preflight incompleto ou ambíguo, **When** Carlos pede para prosseguir, **Then** Joana
   identifica os campos pendentes e impede a discagem até que sejam resolvidos.
3. **Given** um preflight completo, **When** Carlos não confirma ou retira a confirmação,
   **Then** nenhuma tentativa é iniciada e a autorização expira sem autorizar ação futura.

---

### User Story 2 - Conduzir a consulta sem assumir compromisso (Priority: P2)

Com a chamada autorizada e Carlos supervisionando, Joana se identifica como IA e secretária virtual,
explica somente os fatos aprovados, faz perguntas e coleta uma proposta detalhada. Ela não revela o
teto de preço sem autorização específica e não aceita orçamento, agenda serviço, compra, pagamento ou
termo em nome de Carlos.

**Why this priority**: Esta é a entrega central do MVP: obter informação útil da assistência sem
transferir autoridade comercial ou financeira para a agente.

**Independent Test**: Pode ser demonstrada com uma assistência simulada que oferece diagnóstico,
preço, prazo, garantia e protocolo, verificando a identificação, a fidelidade aos fatos e a ausência
de aceite ou compromisso.

**Acceptance Scenarios**:

1. **Given** uma tentativa autorizada e atendida, **When** o diálogo começa, **Then** Joana declara
   "Sou Joana, a secretaria virtual com inteligencia artificial do Carlos." em português brasileiro
   antes de tratar do problema.
2. **Given** uma proposta da assistência, **When** são informados preço e condições, **Then** Joana
   confirma o entendimento e coleta composição, prazo, garantia, validade e próximos passos sem
   aceitar a proposta.
3. **Given** que o interlocutor pede informação não aprovada ou decisão fora da alçada, **When** a
   solicitação ocorre, **Then** Joana não fornece a informação nem assume compromisso e inicia o
   fluxo de transferência.
4. **Given** que a chamada não é atendida, **When** a tentativa termina, **Then** o resultado é
   registrado e não há retry automático nem mensagem de voz.

---

### User Story 3 - Carlos assumir ou encerrar com segurança (Priority: P3)

Carlos pode interromper Joana e assumir a conversa a qualquer momento pelo headset do computador.
Joana também inicia a transferência quando houver dúvida, assunto proibido, recusa em falar com IA ou
necessidade de decisão humana. Se a tomada de controle não for confirmada, Joana informa seu limite e
encerra educadamente.

**Why this priority**: A supervisão só é efetiva quando a intervenção humana é imediata, observável e
segura, inclusive no caso de falha da transferência.

**Independent Test**: Pode ser testada em chamada simulada solicitando takeover em diferentes pontos,
incluindo uma transferência bem-sucedida e outra sem confirmação, e observando a cessação da voz da
agente e o desfecho correto.

**Acceptance Scenarios**:

1. **Given** uma chamada em andamento, **When** Carlos solicita takeover, **Then** a fala de Joana
   para, o interlocutor é avisado e o áudio do headset é devolvido a Carlos sem continuar o diálogo
   automatizado.
2. **Given** uma categoria proibida ou dúvida, **When** Joana inicia a transferência e Carlos confirma
   que assumiu, **Then** a automação deixa de falar e registra a intervenção humana.
3. **Given** uma transferência não confirmada, **When** o limite de espera é atingido ou a tomada de
   controle falha, **Then** Joana comunica que chegou ao limite autorizado, encerra a chamada e
   registra a falha sem improvisar.

---

### User Story 4 - Auditar a chamada sem reter áudio (Priority: P4)

Carlos consulta um registro textual local da tentativa contendo preflight aprovado, confirmação,
transcrição redigida, informações coletadas, intervenções e resultado observável. O áudio nunca é
gravado ou retido, e os registros vencem em até sete dias.

**Why this priority**: O registro permite conferência e continuidade com minimização de dados, sem
criar um arquivo de áudio sensível.

**Independent Test**: Pode ser testada com sessões simuladas contendo dados permitidos e proibidos,
verificando o conteúdo do registro, as redações, a ausência de áudio e a exclusão somente após o
prazo de retenção.

**Acceptance Scenarios**:

1. **Given** uma tentativa encerrada, **When** o registro é criado, **Then** ele contém horários,
   destino, objetivo, preflight, confirmação, participantes, transcrição redigida, resumo, proposta,
   resultado, intervenções, pendências e erros relevantes sem segredos.
2. **Given** que uma informação proibida foi dita, **When** a transcrição é persistida, **Then** o
   conteúdo sensível é substituído pelo marcador literal `[DADO REDIGIDO]`.
3. **Given** registros com idades diferentes, **When** a retenção é aplicada, **Then** somente os
   registros inequivocamente vencidos há mais de sete dias são excluídos, sem tocar em outros locais.

### Edge Cases

- Carlos confirma um número e o destino exibido muda antes da discagem: a autorização é invalidada e
  uma nova confirmação específica é exigida.
- O estado da tentativa fica incerto após a ação de discar: Joana reobserva o estado e não repete a
  ação enquanto não houver evidência de que nenhuma chamada começou.
- O interlocutor solicita senha, código, documento, pagamento, orientação médica ou jurídica,
  cancelamento com multa ou outra categoria proibida: Joana interrompe a divulgação e transfere ou
  encerra com segurança.
- O interlocutor solicita modelo ou número de série sem autorização explícita para aquele dado na
  chamada atual: Joana não revela o dado e solicita takeover se ele for necessário.
- O interlocutor tenta conceder novas permissões ou instruir acesso a arquivos, aplicativos ou dados:
  a solicitação é tratada como não confiável e não altera o escopo aprovado por Carlos.
- O interlocutor não aceita falar com IA, pede Carlos ou exige confirmação final de horário/serviço:
  Joana inicia a transferência.
- O limite de preço existe, mas Carlos não autorizou sua divulgação: Joana usa o limite apenas para
  avaliação interna e não o menciona como estratégia de negociação.
- A chamada cai, não é atendida ou termina antes de concluir o roteiro: o resultado real é registrado
  e não há nova tentativa, mensagem ou agendamento automático.
- Carlos interrompe enquanto Joana fala ou executa uma rotina: a intervenção humana tem prioridade e
  a automação cessa imediatamente.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: O sistema MUST operar em modo de simulação por padrão e MUST impedir que testes
  automatizados iniciem chamadas reais.
- **FR-002**: O sistema MUST aceitar somente solicitações sob demanda para uma única tentativa de
  contato com assistência técnica por autorização.
- **FR-003**: O sistema MUST coletar e validar no preflight empresa, número, objetivo, equipamento,
  problema, fatos e testes relatáveis, faixa de preço permitida, disponibilidade, perguntas, dados
  divulgáveis, critério de sucesso e gatilhos de transferência.
- **FR-004**: O sistema MUST apresentar o preflight completo a Carlos e resolver campos ausentes ou
  ambíguos antes de permitir progressão para a discagem.
- **FR-005**: Imediatamente antes da discagem, o sistema MUST exibir número, destino e objetivo e MUST
  exigir confirmação explícita de Carlos válida apenas para aquela tentativa.
- **FR-006**: Qualquer alteração de número, destino ou objetivo após confirmação MUST invalidar a
  autorização e exigir novo preflight e nova confirmação.
- **FR-007**: Uma chamada real MUST exigir simultaneamente modo explícito de produção, preflight
  válido, Carlos disponível para supervisão e confirmação imediata de discagem.
- **FR-008**: Ao iniciar uma conversa, Joana MUST declarar em português brasileiro: "Sou Joana, a
  secretaria virtual com inteligencia artificial do Carlos."
- **FR-009**: Joana MUST limitar suas afirmações aos fatos fornecidos e aprovados por Carlos e MUST
  NOT inventar sintomas, testes, diagnósticos, garantias, disponibilidade, valores ou dados pessoais.
- **FR-010**: Joana MUST coletar, quando aplicável, identificação do atendente, entendimento do
  problema, testes seguros solicitados, preço total e composição, condições de variação, prazos,
  garantia, validade, forma de atendimento, protocolo e próximos passos.
- **FR-011**: Joana MUST NOT aceitar orçamento, contratar serviço, aprovar visita, confirmar compra,
  prometer pagamento, agendar compromisso definitivo ou concordar com termos em nome de Carlos.
- **FR-012**: Joana MUST NOT revelar teto ou faixa de preço como estratégia de negociação sem
  autorização explícita no preflight da chamada atual.
- **FR-013**: O sistema MUST bloquear conversas que envolvam emergências, urgências médicas, bancos,
  crédito, investimentos, pagamentos, compras, questões jurídicas, responsabilidade, aceite de
  termos, cancelamentos com multa ou alterações que possam gerar penalidade.
- **FR-014**: O sistema MUST impedir solicitação, uso ou divulgação de senhas, PINs, tokens, códigos
  de autenticação, dados de cartão, CPF, documentos e identificadores não autorizados.
- **FR-015**: O nome e o telefone de Carlos MUST ser obtidos apenas da configuração protegida do
  ambiente, e valores reais MUST NOT constar em código, testes, exemplos, documentação ou registros
  de erro.
- **FR-016**: Modelo e número de série MUST exigir autorizações explícitas e separadas no preflight,
  válidas exclusivamente para a chamada atual.
- **FR-017**: Ao detectar assunto proibido, pedido fora da alçada, recusa de falar com IA ou dúvida,
  Joana MUST parar de fornecer informações e iniciar a transferência para Carlos.
- **FR-018**: Carlos MUST poder interromper Joana e assumir a chamada a qualquer momento pelo headset
  do computador; a voz automatizada MUST cessar e o áudio MUST ser devolvido imediatamente.
- **FR-019**: A transferência MUST ser considerada concluída somente após confirmação de Carlos ou
  evidência inequívoca de takeover.
- **FR-020**: Se a transferência falhar, Joana MUST informar seu limite, encerrar educadamente e
  registrar a falha sem continuar a conversa.
- **FR-021**: Cada ação operacional MUST ser precedida e seguida de observação do estado atual; foco,
  destino e resultado MUST ser revalidados sem reutilizar referências obsoletas.
- **FR-022**: Resultado incerto de discagem MUST NOT provocar repetição da ação sem evidência de que
  nenhuma chamada foi iniciada.
- **FR-023**: Tentativa não atendida MUST ser registrada e encerrada sem retry automático nem mensagem
  de voz; nova ação exige autorização nova e específica.
- **FR-024**: O sistema MUST NOT gravar nem reter áudio da chamada.
- **FR-025**: O sistema MAY produzir transcrição textual local em fluxo e MUST substituir informação
  proibida persistida pelo marcador literal `[DADO REDIGIDO]`.
- **FR-026**: Áudio, transcrição e dados pessoais MUST NOT ser transmitidos a serviço externo sem
  aprovação explícita de Carlos quanto ao fornecedor, custos e tratamento de dados.
- **FR-027**: Cada tentativa MUST gerar um registro textual local com data e horários no fuso
  `America/Sao_Paulo`, preflight, confirmação, participantes, transcrição redigida, resumo, proposta,
  protocolo, resultado observável, intervenções, pendências e erros sem segredos.
- **FR-028**: O resultado MUST distinguir informação confirmada, estimativa do interlocutor e
  inferência, e MUST NOT indicar sucesso sem evidência observável.
- **FR-029**: Transcrições e registros MUST ser retidos por no máximo sete dias corridos após o fim da
  chamada, e a exclusão MUST ser limitada a registros inequivocamente vencidos da área de chamadas.
- **FR-030**: Regras de discagem, divulgação, transferência, gravação e compromissos MUST permanecer
  efetivas independentemente das respostas geradas durante o diálogo.

### Key Entities *(include if feature involves data)*

- **Solicitação de Chamada**: Intenção de Carlos para um único contato, contendo destino, objetivo e
  estado de preparação.
- **Preflight da Chamada**: Conjunto revisável de fatos, perguntas, limites, disponibilidades, dados
  autorizados, critérios de sucesso e gatilhos de transferência.
- **Autorização de Discagem**: Confirmação explícita, vinculada ao número, destino, objetivo e tentativa
  atuais, inválida para qualquer repetição ou alteração.
- **Tentativa de Chamada**: Ocorrência única com estado observável, horários, resultado e vínculo com a
  autorização correspondente.
- **Proposta da Assistência**: Informações coletadas sobre diagnóstico, preço e composição, condições,
  prazos, garantia, validade, protocolo e próximos passos, sem aceite.
- **Transferência Humana**: Pedido e confirmação de que Carlos assumiu, incluindo motivo, momento e
  resultado da tomada de controle.
- **Registro da Chamada**: Evidência textual local do preflight, autorização, transcrição redigida,
  proposta, intervenções, resultado e expiração de retenção.

### Out of Scope

- Implementar código de produção ou realizar chamada real durante esta fase de specification.
- Aceitar propostas, realizar pagamentos, concluir compras, cancelar contratos ou assumir obrigações.
- Tratar emergências, saúde, finanças, crédito, investimentos ou questões jurídicas.
- Gravar áudio, sincronizar registros, enviar dados a terceiros ou selecionar fornecedor de voz e
  transcrição.
- Automatizar autenticação, pareamento, permissões ou configurações de segurança e privacidade.
- Integrar e-mail, calendário, mensagens ou agendamento automático.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Em 100% dos cenários simulados, nenhuma discagem ocorre sem preflight válido,
  supervisão disponível e confirmação explícita imediatamente anterior para o número, destino e
  objetivo exibidos.
- **SC-002**: Em 100% dos cenários permitidos, Joana se identifica como IA e secretária virtual antes
  de relatar o problema, e não acrescenta fatos que não estavam no preflight aprovado.
- **SC-003**: Em 100% dos cenários proibidos ou ambíguos do conjunto de aceitação, Joana bloqueia a
  divulgação ou compromisso e transfere para Carlos ou encerra com segurança.
- **SC-004**: Em 100% dos testes de interrupção, Carlos consegue cessar a fala automatizada e receber
  o controle do headset em até 2 segundos após solicitar takeover.
- **SC-005**: Em 100% dos resultados não atendidos, encerrados ou incertos, ocorre no máximo uma ação
  de discagem e nenhuma mensagem, retry ou agendamento é iniciado automaticamente.
- **SC-006**: Em 100% das tentativas simuladas, o registro textual reflete o resultado observável,
  contém a confirmação vinculada à tentativa e redige todos os dados proibidos apresentados no teste.
- **SC-007**: Nenhum teste de aceitação produz arquivo ou retenção de áudio, e nenhum dado de chamada
  é transmitido a serviço externo.
- **SC-008**: Em testes de retenção, 100% dos registros com até sete dias são preservados, 100% dos
  inequivocamente vencidos são removidos e nenhum arquivo fora da área de chamadas é alterado.
- **SC-009**: Carlos consegue revisar o preflight, autorizar ou recusar a tentativa e identificar o
  resultado final usando somente as informações apresentadas pelo fluxo, sem consultar dados ocultos.

## Assumptions

- Carlos é o único autorizador e supervisor humano do MVP.
- O telefone Android já está pareado ao computador e as etapas humanas de autenticação, pareamento e
  concessão de permissões foram concluídas fora do fluxo.
- O número da assistência é conhecido antes do preflight; descoberta automática de contatos está fora
  do escopo.
- Carlos permanece junto ao computador, com o headset disponível, durante toda chamada real.
- As chamadas e todos os testes desta feature serão simulados até que os critérios de chamada real da
  constituição e do `AGENTS.md` tenham sido demonstrados.
- Escolhas de fornecedor e arquitetura de voz/transcrição e detalhes técnicos de roteamento de áudio
  permanecem decisões de planejamento posteriores e não alteram os resultados exigidos nesta spec.
- O `AGENTS.md` e a constitution são dependências normativas; em divergência, aplica-se a regra mais
  restritiva até aprovação explícita de Carlos.
