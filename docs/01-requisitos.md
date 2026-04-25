# Requisitos Funcionais e Não Funcionais

## Requisitos Funcionais

### RF-01. Gestão de vendas

O sistema deve permitir CRUD completo de vendas, contendo:

- Número da venda.
- Data e hora da venda.
- Identificação do cliente.
- Filial.
- Itens vendidos.
- Quantidade por item.
- Preço unitário.
- Percentual e valor de desconto.
- Valor total por item.
- Valor total da venda.
- Status de cancelamento da venda.
- Status de cancelamento por item.

### RF-02. Regras de desconto

- Compras com menos de 4 itens idênticos não recebem desconto.
- Compras com 4 a 9 itens idênticos recebem 10% de desconto.
- Compras com 10 a 20 itens idênticos recebem 20% de desconto.
- Não deve ser permitido vender mais de 20 itens idênticos.

### RF-03. Eventos de negócio

O backend deve ser preparado para publicar eventos de integração relacionados a:

- `SaleCreated`
- `SaleModified`
- `SaleCancelled`
- `SaleItemCancelled`

### RF-04. API e documentação

- Os endpoints devem ser documentados no README e, posteriormente, em coleção Postman/Insomnia.
- A API deve suportar filtros, paginação e ordenação nas consultas relevantes.
- A API deve expor endpoint de health check para uso local, conteinerização e orquestração.

## Requisitos Não Funcionais

### RNF-01. Arquitetura

- O backend deve seguir princípios de Clean Architecture com separação entre domínio, aplicação, infraestrutura e API.
- A solução deve permitir evolução para microserviços sem ruptura do domínio principal.
- A camada de aplicação deve ser orientada a handlers/casos de uso com Mediator.
- O mapeamento entre entidades, DTOs e contratos deve usar AutoMapper quando fizer sentido.
- Os projetos da solução devem preservar a convenção de nomes do template, usando o prefixo `Ambev.DeveloperEvaluation.*`.

### RNF-02. Padrão de retorno

- Casos de uso e serviços de aplicação devem retornar `Result<T>`.
- O padrão deve contemplar sucesso, falhas de validação, falhas de negócio, recurso não encontrado e erros inesperados mapeáveis para HTTP.

### RNF-03. Persistência

- Cada microserviço deve possuir banco de dados próprio.
- Bancos transacionais deverão ser PostgreSQL.
- MongoDB deve compor a versão entregável como store de `event log` e auditoria da venda.
- O acesso relacional deverá usar EF Core.

### RNF-04. Mensageria

- Toda comunicação assíncrona entre serviços deve ocorrer via RabbitMQ.
- Eventos devem ser idempotentes e versionáveis.
- A biblioteca de mensageria preferencial para o projeto será Rebus.
- Publicação e consumo de mensagens devem considerar rastreamento, retries controlados e prevenção de duplicidade.
- O consumo de eventos no RabbitMQ deve possuir deduplicação persistida para evitar reprocessamento indevido.

### RNF-05. Entrega e operação

- O projeto deve ser executável localmente via Docker Compose.
- A estrutura deve nascer preparada para deploy em Kubernetes.
- O repositório deve conter documentação suficiente para onboarding técnico rápido.
- A API deve possuir endpoints de `liveness` e `readiness`.
- A API deve possuir política de rate limiting configurável por ambiente.
- Os endpoints de health check devem separar dependências críticas de verificação simples de processo.

### RNF-06. Governança de API

- Respostas de erro devem seguir contrato consistente e rastreável, preferencialmente com `ProblemDetails` ou estrutura equivalente única para toda a API.
- Endpoints mutáveis críticos, especialmente criação e cancelamento, devem suportar idempotência quando aplicável.
- A API deve emitir e propagar `correlationId` e `traceId` para troubleshooting, logs e respostas HTTP.
- Deve existir documentação mínima de paginação, ordenação, filtros e códigos de resposta.
- A API deve considerar cabeçalhos de segurança e política de CORS configurável.
- A API deve ter timeout e tratamento de cancelamento cooperativo nas operações assíncronas.
- A idempotência HTTP deve ser persistida em PostgreSQL para garantir consistência nas operações críticas.

### RNF-07. Resiliência

- O projeto deve aplicar políticas de resiliência com Polly nas integrações externas e pontos de I/O suscetíveis a falhas transitórias.
- O uso de Polly deve priorizar timeout, retry com backoff, circuit breaker e, quando fizer sentido, fallback controlado.
- Polly não deve ser usado para mascarar erro de domínio, validação ou falha funcional determinística.

### RNF-08. Qualidade

- O código deve usar nomes descritivos, funções curtas e comentários somente quando agregarem contexto.
- Devem existir testes unitários cobrindo regras de negócio críticas.
- Tratamento de erros e mensagens devem ser consistentes e claros.
- Os testes automatizados devem usar xUnit, com NSubstitute para doubles e Faker/Bogus para geração de dados quando necessário.

### RNF-09. Processo e versionamento

- O fluxo de trabalho deve seguir segregação lógica de commits por etapa, alinhado à recomendação de Git Flow.
- As mensagens de commit devem seguir padrão semântico.
- O desenvolvimento deve ocorrer em branches por fase, com integração em `master` apenas após conclusão e validação da fase.
- As fases mínimas de branch devem refletir a sequência sugerida do desafio: banco + ORM, camada de serviços, regras de negócio e testes.