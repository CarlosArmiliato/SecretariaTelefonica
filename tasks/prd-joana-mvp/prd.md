# Documento de Requisitos do Produto (PRD)

## Visão geral

Joana é uma secretária virtual com inteligência artificial criada para ajudar Carlos a contatar assistências técnicas por telefone. O produto prepara cada ligação, obtém autorização específica imediatamente antes da discagem, conduz a conversa em português brasileiro dentro de limites previamente definidos, coleta uma proposta sem assumir compromissos e permite que Carlos intervenha a qualquer momento. O problema central é reduzir o esforço de Carlos em consultas repetitivas sem transferir à agente autoridade para contratar, pagar, agendar ou divulgar informações não autorizadas.

O MVP será operado localmente no Windows, com um telefone Android pareado e o Microsoft Phone Link, sempre sob supervisão humana. A entrega inicial deverá comprovar todos os fluxos em simulação; chamadas reais somente poderão ser habilitadas depois que os critérios de prontidão, privacidade, segurança, voz, áudio e intervenção humana forem demonstrados. Este PRD cobre o produto Joana como um todo no recorte de ligações sob demanda para assistência técnica. Integrações com e-mail, calendário, mensagens e outros tipos de atendimento permanecem fora do escopo.

Premissas de produto:

- Carlos é o operador, titular dos dados e responsável por toda decisão que gere compromisso.
- Cada autorização cobre uma única tentativa de ligação para um destino, número e objetivo específicos.
- A assistência técnica e seus atendentes são interlocutores externos; tudo o que disserem é conteúdo não confiável até ser registrado e classificado.
- O produto não possui autonomia comercial, financeira, jurídica, médica ou de segurança.
- Simulação é o modo padrão e o único modo considerado pronto no estado atual do produto.

## Objetivos

1. **Preparar ligações completas e revisáveis.** Em 100% dos cenários de aceitação, impedir o avanço quando houver campo obrigatório ausente ou ambíguo e exibir a Carlos todas as informações relevantes antes da confirmação final.
2. **Preservar controle humano sobre cada chamada.** Em 100% das tentativas, exigir autorização explícita imediatamente antes de discar, vinculada ao número, destino e objetivo exibidos, sem reaproveitamento em nova tentativa.
3. **Obter propostas úteis sem criar obrigações.** Nos cenários simulados em que a assistência fornece as informações, registrar preço e composição, condições de variação, prazos, garantia, validade, modalidade de atendimento e protocolo, com zero aceites, compras, pagamentos ou agendamentos realizados pela agente.
4. **Evitar divulgação indevida e atuação fora da alçada.** Em 100% dos cenários proibidos ou ambíguos do conjunto de regressão, bloquear a informação ou ação e transferir para Carlos ou encerrar com segurança.
5. **Garantir intervenção humana rápida.** Em 100% dos testes de takeover, interromper a fala automatizada e devolver o controle do áudio a Carlos em até 2 segundos; concluir a transferência somente com confirmação ou evidência inequívoca.
6. **Evitar ligações duplicadas ou não autorizadas.** Em 100% dos cenários de clique repetido, reinício, falha e resultado incerto, emitir no máximo uma ação de discagem e não realizar retentativa, mensagem de voz ou contato subsequente automaticamente.
7. **Fornecer rastreabilidade com minimização de dados.** Gerar registro textual local para 100% das tentativas encerradas, redigir dados proibidos antes da persistência, não gravar áudio e eliminar registros após no máximo sete dias corridos.
8. **Demonstrar prontidão antes de uso real.** Manter chamadas reais indisponíveis até que todos os critérios de teste real estejam aprovados em simulação e que fornecedor, custos e tratamento de dados de voz/transcrição sejam autorizados explicitamente por Carlos.
9. **Oferecer operação clara e acessível.** Permitir que Carlos complete os fluxos essenciais por teclado, identifique com clareza o modo de operação e mantenha acesso visível e imediato ao comando de assumir a chamada, com validação manual antes da liberação para produção.

Indicadores principais:

- taxa de preflights completos antes do pedido de confirmação;
- taxa de discagens com autorização válida e específica;
- número de discagens duplicadas ou retentativas automáticas;
- taxa de apresentação obrigatória como IA antes da conversa;
- taxa de bloqueio correto de pedidos proibidos ou ambíguos;
- tempo de interrupção da voz e devolução do áudio no takeover;
- completude das propostas quando a informação estiver disponível;
- taxa de registros completos, redigidos e excluídos dentro da política;
- número de arquivos de áudio criados ou dados enviados externamente;
- percentual dos cenários de prontidão aprovados em simulação.

## Histórias de usuário

- **US1:** Como Carlos, quero informar o destino, o problema e os limites de uma ligação para que Joana saiba exatamente o que pode tratar.
- **US2:** Como Carlos, quero que Joana identifique lacunas e ambiguidades no preflight para impedir que uma chamada comece com instruções incompletas.
- **US3:** Como Carlos, quero revisar um resumo imutável e confirmar uma única tentativa imediatamente antes da discagem para manter controle sobre quem será chamado e com qual objetivo.
- **US4:** Como Carlos, quero que qualquer alteração relevante invalide a autorização anterior para que minha confirmação nunca seja aplicada a uma chamada diferente.
- **US5:** Como Carlos, quero simular o fluxo completo sem risco de ligação real para validar o comportamento antes de usar o produto em produção.
- **US6:** Como atendente de uma assistência técnica, quero saber claramente que falo com uma secretária virtual com IA e qual é o objetivo do contato para decidir se aceito prosseguir.
- **US7:** Como Carlos, quero que Joana relate somente fatos que aprovei para que ela não invente sintomas, diagnósticos ou informações pessoais.
- **US8:** Como Carlos, quero que Joana faça perguntas estruturadas sobre orçamento, prazo e garantia para que eu receba uma proposta comparável e acionável.
- **US9:** Como Carlos, quero que Joana jamais aceite proposta ou confirme serviço em meu nome para que todas as obrigações dependam da minha decisão direta.
- **US10:** Como Carlos, quero controlar separadamente quais dados podem ser revelados em cada chamada para reduzir exposição de informações pessoais e do equipamento.
- **US11:** Como Carlos, quero assumir a conversa a qualquer momento por um comando visível e imediato para lidar diretamente com decisões ou situações sensíveis.
- **US12:** Como atendente que não aceita falar com IA ou deseja falar com Carlos, quero ser avisado da transferência para que a conversa possa continuar com uma pessoa.
- **US13:** Como Carlos, quero que a ligação termine com segurança caso a transferência falhe para impedir que a agente improvise fora da alçada.
- **US14:** Como Carlos, quero que chamadas não atendidas, interrompidas ou incertas terminem sem nova tentativa automática para que nenhum contato adicional ocorra sem consentimento.
- **US15:** Como Carlos, quero consultar um relatório textual redigido de cada tentativa para conferir o que ocorreu e decidir o próximo passo sem armazenar áudio.
- **US16:** Como Carlos, quero que os registros expirem automaticamente em até sete dias para limitar a retenção de dados pessoais.
- **US17:** Como Carlos, quero distinguir no relatório fatos confirmados, estimativas do atendente e inferências da agente para avaliar a confiabilidade de cada informação.
- **US18:** Como Carlos, quero receber indicação clara de falhas, pendências e resultado observável para que o produto nunca apresente uma chamada incompleta como sucesso.

## Principais funcionalidades

### 1. Preparação e validação do preflight

Reúne o escopo autorizado de uma única chamada e impede progressão enquanto houver dados necessários ausentes ou ambíguos.

- **RF1:** O produto deve coletar empresa ou assistência, número a discar e objetivo exato da chamada.
- **RF2:** O produto deve coletar equipamento ou item afetado, descrição do problema, fatos, testes realizados e mensagens de erro que podem ser relatados.
- **RF3:** O produto deve coletar valor máximo ou faixa de preço que pode ser considerada e registrar separadamente se essa informação pode ser mencionada ao interlocutor.
- **RF4:** O produto deve coletar dias, horários, intervalos e restrições de disponibilidade aplicáveis.
- **RF5:** O produto deve coletar perguntas a responder, dados que podem ser revelados, critério de sucesso e situações que exigem transferência.
- **RF6:** O produto deve exigir uma indicação explícita quando não houver testes realizados, disponibilidade, faixa de preço ou outro campo condicional, em vez de interpretar omissão como resposta.
- **RF7:** O produto deve apontar lacunas e ambiguidades e impedir a solicitação de discagem até que sejam resolvidas.
- **RF8:** O produto deve apresentar um resumo revisável do preflight antes da confirmação final.

### 2. Autorização e discagem de tentativa única

Mantém Carlos no controle da ação externa mais sensível e evita repetição por erro, clique duplo ou incerteza.

- **RF9:** Imediatamente antes de discar, o produto deve exibir número, destino e objetivo e solicitar confirmação explícita de Carlos.
- **RF10:** A autorização deve valer apenas para a tentativa, o destino, o número, o objetivo e a versão do preflight confirmados.
- **RF11:** Alteração em qualquer dado vinculado à autorização deve invalidá-la e exigir nova revisão e confirmação.
- **RF12:** Recusa, cancelamento, ausência ou expiração da confirmação deve impedir a chamada e não conceder permissão futura.
- **RF13:** Uma chamada real deve exigir simultaneamente modo explícito de produção, preflight válido, supervisão humana disponível e confirmação imediata.
- **RF14:** O produto deve garantir no máximo uma emissão de discagem por tentativa, inclusive diante de clique repetido, reinício, falha ou concorrência.
- **RF15:** Quando o resultado da discagem for desconhecido, o produto deve reavaliar o estado e não repetir a ação sem prova de que nenhuma chamada foi iniciada.
- **RF16:** Chamada não atendida deve ser registrada e encerrada sem retentativa automática nem mensagem de voz.

### 3. Diálogo supervisionado para coleta de proposta

Conduz uma conversa objetiva em português brasileiro, limitada aos fatos aprovados e à obtenção de informações.

- **RF17:** No início de toda chamada atendida, antes de tratar do problema, Joana deve declarar: “Sou Joana, a secretaria virtual com inteligencia artificial do Carlos.”
- **RF18:** A fala deve ser educada, objetiva, calma e natural, sem pressão, manipulação, discussão ou tentativa de se passar por pessoa humana.
- **RF19:** Joana deve relatar somente fatos aprovados no preflight e não pode inventar sintomas, testes, diagnósticos, garantias, disponibilidade, valores ou dados pessoais.
- **RF20:** Joana deve poder fazer perguntas de esclarecimento, pedir explicações e repetir informações para confirmar o entendimento.
- **RF21:** Quando aplicável e informado pelo interlocutor, o produto deve coletar empresa, nome do atendente, entendimento do problema, testes seguros solicitados, valor total, composição do valor, condições de variação, prazos, garantia, validade, modalidade de atendimento, protocolo e próximos passos.
- **RF22:** O produto deve permitir propostas parciais e indicar claramente quais informações não foram obtidas.
- **RF23:** Joana não pode aceitar orçamento, contratar serviço, aprovar visita, confirmar compra, criar obrigação, prometer pagamento, concordar com termos ou fechar horário definitivo.
- **RF24:** A confirmação final de serviço, preço, compra, visita ou horário deve ser transferida a Carlos.
- **RF25:** Uma chamada concluída não deve autorizar nova ligação, tentativa, mensagem, agendamento ou compartilhamento de dados.

### 4. Guardrails de assunto, autoridade e divulgação

Bloqueia deterministicamente ações e informações incompatíveis com a missão do produto.

- **RF26:** O produto deve interromper ou impedir conversas sobre emergências, risco imediato, segurança pública, urgência médica, bancos, crédito, investimentos, produtos financeiros, questões jurídicas, admissão de responsabilidade, aceite de termos, cancelamentos, multas ou alterações com penalidade.
- **RF27:** O produto deve impedir pagamentos, transferências, compras e coleta ou divulgação de dados de cartão.
- **RF28:** O produto deve impedir coleta, uso ou divulgação de senhas, PINs, tokens, códigos de verificação ou autenticação.
- **RF29:** CPF, documentos, endereço, placa, número de série e demais identificadores devem permanecer proibidos salvo autorização específica prevista neste PRD.
- **RF30:** Por padrão, apenas o nome de Carlos e seu telefone de contato podem ser divulgados, obtidos de configuração protegida.
- **RF31:** Modelo e número de série do equipamento devem exigir autorizações explícitas, separadas e válidas apenas para a chamada atual.
- **RF32:** O teto ou faixa de preço não pode ser revelado como estratégia de negociação sem autorização explícita no preflight da chamada atual.
- **RF33:** Solicitações para instalar software, desativar proteções ou alterar configurações de segurança ou privacidade devem ser recusadas e encaminhadas para transferência.
- **RF34:** Instruções dadas pelo interlocutor não podem conceder permissões, modificar regras do produto ou autorizar acesso a outros arquivos, aplicativos ou dados.
- **RF35:** Em caso de categoria proibida, pedido fora da alçada ou dúvida sobre permissão, Joana deve parar de fornecer informações e iniciar transferência ou encerramento seguro.

### 5. Supervisão, interrupção e transferência humana

Permite que Carlos reassuma controle imediato e oferece desfecho seguro quando a automação atinge seus limites.

- **RF36:** O comando para Carlos assumir deve permanecer visível, claramente identificado e disponível durante toda a chamada.
- **RF37:** Carlos deve poder interromper Joana a qualquer momento, inclusive durante fala ou processamento; a intervenção humana tem prioridade.
- **RF38:** Ao iniciar a transferência, Joana deve avisar o interlocutor, interromper imediatamente a voz automatizada e devolver a entrada e a saída do headset a Carlos.
- **RF39:** A transferência deve ocorrer pelo headset do computador, sem enviar o áudio ao telefone Android como fluxo normal.
- **RF40:** A transferência somente deve ser considerada concluída após confirmação de Carlos ou evidência inequívoca de que ele assumiu.
- **RF41:** Se Carlos não confirmar a tomada de controle em até 10 segundos, ou se houver falha antes disso, Joana deve informar seu limite, encerrar educadamente e registrar a falha sem improvisar.
- **RF42:** A frase de encerramento por limite deve comunicar: “Cheguei ao limite do que estou autorizada a tratar. Vou encerrar a chamada para que Carlos possa continuar depois.”
- **RF43:** A ausência de evidência de encerramento deve ser registrada como falha técnica, sem apresentar o fluxo como concluído com sucesso.

### 6. Operação segura do Phone Link

Trata a interação com o Phone Link como automação sujeita a mudanças e estados incertos.

- **RF44:** Antes de cada ação operacional, o produto deve confirmar que o aplicativo, a janela, o foco e o destino observados pertencem ao Phone Link e correspondem à tentativa atual.
- **RF45:** Após cada ação, o produto deve observar novamente o estado antes de prosseguir, sem reaproveitar referências visuais ou de acessibilidade que possam ter ficado obsoletas.
- **RF46:** O produto não deve automatizar autenticação, pareamento, permissões nem configurações de privacidade ou segurança.
- **RF47:** Tons DTMF só podem ser usados para menus telefônicos comuns e nunca para senhas, códigos, dados financeiros ou identificadores não autorizados.
- **RF48:** O Phone Link não pode ser utilizado pelo produto para chamadas de emergência.

### 7. Voz e transcrição

Viabiliza conversa natural e auditável sem gravar áudio ou transmitir dados sem consentimento.

- **RF49:** Toda fala automatizada deve usar português brasileiro e pronúncia natural.
- **RF50:** A voz pode ser sintética, mas não pode copiar nem se passar pela voz de pessoa real sem permissão adequada.
- **RF51:** O produto não pode gravar ou reter o áudio da chamada.
- **RF52:** A transcrição textual pode ser produzida em fluxo e deve permanecer local no MVP.
- **RF53:** Informação proibida presente na fala deve ser substituída por `[DADO REDIGIDO]` antes de ser persistida em qualquer campo.
- **RF54:** Áudio, transcrição ou dados pessoais não podem ser enviados a serviço externo sem aprovação explícita de Carlos sobre fornecedor, custos e tratamento de dados.
- **RF55:** Latência, interrupção de fala, detecção de silêncio e capacidade de takeover devem ser avaliadas e aprovadas antes da primeira chamada real.

### 8. Registro, auditoria e retenção

Mantém evidência local suficiente para conferência, sem aumentar desnecessariamente a exposição de dados.

- **RF56:** Cada tentativa deve gerar um registro textual local, inclusive quando não atendida, transferida, encerrada por limite ou afetada por falha técnica.
- **RF57:** O registro deve conter data e horários de início e fim no fuso `America/Sao_Paulo`, destino, número chamado, objetivo, preflight aprovado e confirmação de Carlos.
- **RF58:** O registro deve conter pessoas identificadas, transcrição redigida, resumo objetivo, proposta, prazos, garantia, validade, protocolo, intervenções humanas, pendências, próxima ação sugerida e erros relevantes sem segredos.
- **RF59:** O resultado deve usar uma classificação observável entre atendida, não atendida, transferida, encerrada por limite, falha técnica ou concluída.
- **RF60:** O produto não pode indicar sucesso sem evidência observável e deve diferenciar informação confirmada, estimativa do interlocutor e inferência da agente.
- **RF61:** Registros e transcrições devem ser retidos por no máximo sete dias corridos contados do encerramento da chamada.
- **RF62:** A exclusão deve atingir somente arquivos inequivocamente vencidos na área local de registros de chamadas e deve registrar data, alvo e resultado sem copiar a transcrição.
- **RF63:** Nenhum registro, transcrição ou relatório deve ser sincronizado ou enviado automaticamente.

### 9. Modos de operação e prontidão

Impede que recursos incompletos sejam confundidos com capacidade segura de produção.

- **RF64:** O modo padrão do produto deve ser simulação.
- **RF65:** Testes automatizados nunca podem realizar chamadas reais, acessar contatos reais, gravar áudio ou transmitir dados.
- **RF66:** O modo ativo e a disponibilidade ou bloqueio de produção devem ser apresentados claramente ao operador.
- **RF67:** Chamadas reais devem permanecer indisponíveis enquanto fornecedor de voz/transcrição, roteamento de áudio e tratamento de dados não estiverem aprovados.
- **RF68:** Antes da primeira chamada real, o produto deve demonstrar em simulação: identificação como IA; confirmação imediata; bloqueio de assuntos e dados proibidos; interrupção e takeover; encerramento seguro em falha; ausência de retentativa e gravação; relatório correto e redigido; e recuperação de estado incerto sem chamada duplicada.

## Critérios de aceitação

- **CA-01 (US1, RF1–RF8):** Dado um preflight completo, quando Carlos o revisa, então vê destino, número, objetivo, equipamento, problema, fatos, testes, política de preço, disponibilidade, perguntas, dados divulgáveis, critério de sucesso e gatilhos de transferência.
- **CA-02 (US2, RF6–RF7):** Dado um campo necessário ausente ou ambíguo, quando Carlos tenta avançar, então o produto identifica a pendência e bloqueia a confirmação de discagem.
- **CA-03 (US3, RF9–RF12):** Dado um preflight válido, quando Carlos chega ao passo final, então o produto exibe número, destino e objetivo e nenhuma discagem ocorre sem confirmação explícita naquele momento.
- **CA-04 (US4, RF10–RF11):** Dada uma autorização concedida, quando número, destino, objetivo ou preflight muda, então a autorização é invalidada e uma nova confirmação é exigida.
- **CA-05 (US5, RF64–RF66):** Dada a inicialização padrão ou a execução de testes, quando uma tentativa é iniciada, então o produto usa simulação e não aciona integração real.
- **CA-06 (US3, RF13):** Dado o modo de produção sem qualquer um dos pré-requisitos simultâneos, quando Carlos tenta ligar, então a ação é bloqueada e o motivo é exibido.
- **CA-07 (US3, RF14–RF16):** Dada uma confirmação válida, quando há clique duplo, reinício, concorrência, falha ou retorno incerto, então ocorre no máximo uma emissão de discagem e nenhuma retentativa automática.
- **CA-08 (US14, RF16):** Dada uma chamada não atendida, quando a tentativa termina, então o resultado é registrado e não há nova ligação nem mensagem de voz.
- **CA-09 (US6, RF17–RF18):** Dada uma chamada atendida, quando o diálogo começa, então a primeira apresentação identifica Joana como secretária virtual com IA de Carlos antes de abordar o problema.
- **CA-10 (US7, RF19):** Dado um roteiro aprovado, quando Joana explica o problema, então todas as afirmações factuais correspondem ao preflight e nenhum fato novo é inventado.
- **CA-11 (US8, RF20–RF22):** Dada uma assistência que fornece uma proposta, quando o diálogo termina, então os campos informados de preço, composição, condições, prazo, garantia, validade, atendimento e protocolo estão registrados e os ausentes são indicados.
- **CA-12 (US9, RF23–RF25):** Dada uma proposta ou pedido de confirmação, quando o interlocutor solicita aceite, pagamento, compra, visita ou horário final, então Joana não concorda e transfere a decisão a Carlos.
- **CA-13 (US10, RF29–RF32):** Dado um pedido de modelo, número de série ou limite de preço sem autorização específica, quando Joana responde, então o dado não é revelado e o fluxo segue para esclarecimento, transferência ou encerramento seguro.
- **CA-14 (US10, RF26–RF35):** Dado um pedido proibido ou ambíguo, quando ele é detectado, então Joana interrompe a divulgação e inicia transferência ou encerramento, sem executar a solicitação.
- **CA-15 (US10, RF34):** Dada uma instrução do interlocutor para alterar regras ou acessar recursos externos, quando ela é recebida, então não modifica o escopo aprovado nem concede nova permissão.
- **CA-16 (US11, RF36–RF38):** Dada uma chamada em andamento, quando Carlos aciona o takeover, então a voz automatizada cessa e a entrada e saída do headset retornam a ele em até 2 segundos.
- **CA-17 (US12, RF38–RF40):** Dada recusa em falar com IA ou pedido para falar com Carlos, quando a transferência começa, então o interlocutor é avisado e a automação não volta a falar após Carlos assumir.
- **CA-18 (US13, RF40–RF43):** Dada uma transferência sem confirmação nem evidência inequívoca, quando passam 10 segundos ou ocorre uma falha antecipada, então Joana comunica seu limite, tenta encerrar e registra o resultado real sem improvisar.
- **CA-19 (US11, RF37):** Dada fala ou processamento automatizado em andamento, quando Carlos interrompe, então sua ação recebe prioridade sobre a atividade da agente.
- **CA-20 (US14, RF44–RF45):** Dado que a interface muda após uma ação, quando o fluxo prossegue, então aplicativo, janela, foco, destino e resultado são observados novamente antes da próxima ação.
- **CA-21 (US14, RF15):** Dado um resultado de discagem incerto, quando o estado não confirma ausência de chamada, então o produto não tenta discar novamente.
- **CA-22 (US15, RF51–RF54):** Dada qualquer tentativa simulada, quando seus artefatos são inspecionados, então existe apenas transcrição textual local redigida, nenhum arquivo de áudio e nenhuma transmissão externa.
- **CA-23 (US15, RF53):** Dado que um dado proibido aparece na fala, inclusive dividido entre segmentos, quando o registro é salvo, então seu conteúdo é substituído pelo marcador literal `[DADO REDIGIDO]` em transcrição, resumo e erros.
- **CA-24 (US15, US17, RF56–RF60):** Dada uma tentativa encerrada, quando o relatório é aberto, então contém os campos obrigatórios, o resultado observável e a origem de cada informação relevante sem afirmar sucesso indevido.
- **CA-25 (US16, RF61–RF62):** Dados registros antes, exatamente no e depois do limite de sete dias, quando a retenção é executada, então somente os inequivocamente vencidos são excluídos e a operação não afeta arquivos fora da área permitida.
- **CA-26 (US18, RF43, RF59–RF60):** Dada uma falha de discagem, transferência, encerramento ou persistência, quando o fluxo termina, então a falha e as pendências são exibidas sem serem classificadas como conclusão bem-sucedida.
- **CA-27 (US5, RF65):** Dada a suíte automatizada completa, quando ela é executada, então não há chamada real, rede externa, gravação, retry, voicemail, compromisso ou uso de dado pessoal real.
- **CA-28 (US5, RF67–RF68):** Dado que qualquer critério de prontidão está pendente, quando o modo de produção é solicitado, então o produto permanece bloqueado e informa que produção está indisponível.
- **CA-29 (US11, RF36):** Dada qualquer etapa de chamada simulada, quando Carlos navega apenas por teclado, então consegue localizar e acionar o takeover, com foco visível e nome acessível.
- **CA-30 (US1–US18):** Dada a regressão de segurança, quando são executados cenários permitidos, proibidos e ambíguos, então 100% deles preservam confirmação específica, limites de autoridade, privacidade, transferência e resultado observável.

## Experiência do usuário

### Perfis e necessidades

- **Carlos — operador e decisor:** precisa reduzir esforço, revisar tudo antes de agir, acompanhar a chamada em tempo real, interromper imediatamente, compreender falhas e decidir qualquer compromisso.
- **Atendente da assistência — interlocutor externo:** precisa receber identificação transparente de que fala com IA, contexto objetivo, perguntas claras e aviso antes de uma transferência.
- **Responsável por validação — perfil secundário:** precisa executar cenários simulados, verificar evidências e confirmar que produção continua bloqueada enquanto os critérios não forem atendidos.

### Jornada principal

1. Carlos inicia uma nova solicitação de chamada e vê claramente que está em simulação ou produção.
2. Preenche os dados do preflight; o produto apresenta validações específicas para ausências e ambiguidades.
3. Revisa um resumo somente leitura com tudo o que Joana poderá dizer, perguntar e revelar.
4. Solicita a ligação; imediatamente antes da ação, confere destino, número e objetivo e confirma ou cancela.
5. Durante a chamada, acompanha fatos liberados, roteiro, informações coletadas e o comando permanente para assumir.
6. Se o diálogo sair do escopo, Joana bloqueia a ação e transfere; Carlos assume pelo headset ou o produto encerra com segurança.
7. Ao fim, Carlos vê resultado, proposta, classificações, pendências e referência do relatório local.

### Diretrizes de UI/UX e acessibilidade

- O modo ativo deve permanecer inequívoco, com produção bloqueada apresentada como indisponível e não como erro genérico.
- A confirmação de discagem deve ser uma etapa distinta e imediata, sem confirmação em lote, opção pré-marcada ou linguagem ambígua.
- O resumo de preflight deve ser legível, somente leitura e permitir voltar para corrigir dados antes de confirmar.
- Informações autorizadas e proibidas devem ter distinção textual, sem depender exclusivamente de cor.
- O comando de takeover deve permanecer visível, ter nome de ação explícito, foco evidente e atalho acessível por teclado.
- Alertas de segurança, transferência e falha devem usar linguagem direta e indicar o que ocorreu e o que Carlos precisa fazer.
- Estados de processamento não podem ocultar nem desabilitar a capacidade de interrupção humana.
- A interface deve oferecer ordem de foco previsível, rótulos acessíveis, contraste adequado e suporte aos fluxos essenciais apenas por teclado.
- Textos, números e resumos devem permanecer legíveis em escalonamento de tela comum do Windows, sem truncar destino, objetivo ou ação crítica.
- A interface deve evitar expor dados proibidos em notificações, erros, nomes de arquivos ou evidências de teste.
- Antes da produção, teclado, foco, nomes acessíveis, visibilidade do takeover e responsividade devem ser validados manualmente com evidência observável.

## Restrições técnicas de alto nível

- O MVP deve operar no Windows e interagir com um telefone Android já pareado por meio do Microsoft Phone Link.
- A interação com o Phone Link é automação de interface; o produto não pode pressupor API pública ou estabilidade de posições, identificadores e elementos visuais entre versões.
- O produto deve funcionar localmente no MVP, sem sincronização automática ou dependência de rede para registros.
- O nome e o telefone reais de Carlos devem permanecer somente em configuração protegida do ambiente; código, testes, fixtures, documentação, mensagens de erro e controle de versão devem usar placeholders ou dados sintéticos.
- Guardrails de discagem, divulgação, compromisso, transferência, gravação e retenção devem ser independentes da resposta gerada pela IA e falhar de forma segura quando houver dúvida.
- O produto deve impedir chamadas duplicadas mediante estado persistente e vínculo de uma autorização a uma única tentativa.
- Datas de apresentação e caminhos de registro devem considerar o fuso `America/Sao_Paulo`; a expiração deve ocorrer no máximo sete dias corridos após o encerramento.
- O sistema não pode gravar áudio. Dados textuais persistidos devem passar por redação antes do armazenamento.
- Serviços externos de voz, transcrição ou dados só podem ser incorporados após aprovação explícita de fornecedor, custos, privacidade e tratamento de dados.
- O fluxo deve suportar interrupção prioritária da fala e devolução do áudio a Carlos em até 2 segundos nos cenários de aceitação.
- O produto deve permanecer em simulação por padrão e apresentar falha fechada quando produção ou uma integração necessária não estiver disponível.
- Qualquer mudança que afete discagem, divulgação, transferência, gravação ou compromissos exige regressão com cenários permitidos, proibidos e ambíguos.
- Não há meta de operação concorrente ou em escala no MVP: o produto atende um operador, uma chamada e uma tentativa por vez.

## Fora do escopo

- Aceitar orçamento, contratar serviço, aprovar visita, efetuar compra, pagamento ou transferência, prometer pagamento, concordar com termos ou confirmar agendamento definitivo.
- Realizar chamadas de emergência ou tratar urgências médicas, segurança pública, finanças, crédito, investimentos, questões jurídicas, responsabilidade ou penalidades.
- Fazer chamadas em lote, campanhas, discagem autônoma, rediscagem automática, voicemail ou contato sem solicitação e autorização específica de Carlos.
- Automatizar autenticação, pareamento do Android, permissões ou configurações de privacidade e segurança do Windows ou Phone Link.
- Gravar, armazenar ou reproduzir o áudio da ligação.
- Enviar automaticamente áudio, transcrição, relatórios ou dados pessoais a terceiros.
- Selecionar neste PRD o fornecedor definitivo de voz ou transcrição, seus custos ou a arquitetura de roteamento de áudio.
- Copiar ou imitar a voz de uma pessoa real sem permissão adequada.
- Integrar Gmail, calendário, mensagens, CRM, pagamentos, compras ou agendamento automático.
- Realizar negociação baseada no teto de preço de Carlos sem autorização específica para divulgá-lo.
- Atender outros domínios além de assistência técnica no MVP.
- Operar sem Carlos disponível para supervisão e takeover.
- Liberar chamadas reais apenas porque os fluxos simulados existem; produção depende de todos os critérios de prontidão e de validação humana da interface e do áudio.
- Reter transcrições e registros por mais de sete dias ou manter arquivo de áudio como evidência.
- Definir arquitetura, classes, bibliotecas, protocolos internos ou tarefas de implementação; esses detalhes pertencem à TechSpec e ao plano de execução.
