# Arquitetura Alvo

## Direção Arquitetural

A solução será organizada como um backend orientado a domínio, com estrutura preparada para representar os cinco micro-serviços pedidos no desafio: `Sales`, `Products`, `Carts`, `Users` e `Auth`. A prioridade continua sendo entregar o fluxo de vendas com qualidade, mas sem perder aderência à segmentação funcional esperada pela spec.

## Bounded Contexts Planejados

### 1. Sales Service

- Contexto principal do desafio.
- Responsável por venda, itens da venda, cálculo de descontos, cancelamentos e publicação de eventos.
- Banco: PostgreSQL para transações e MongoDB para `event log` e auditoria da venda na entrega inicial.

### 2. Products Service

- Responsável por cadastro, consulta, categorias e paginação de produtos conforme a spec.
- Banco: PostgreSQL.

### 3. Carts Service

- Responsável por carrinhos, itens do carrinho e vínculo com usuário/produto por identidades externas.
- Banco: PostgreSQL.

### 4. Users Service

- Responsável por cadastro, consulta, atualização, remoção, perfis e dados cadastrais de usuários.
- Banco: PostgreSQL.

### 5. Auth Service

- Responsável por autenticação e emissão de token.
- Pode compartilhar a base de identidade na entrega inicial, mas mantendo contrato e composição isoláveis.
- Banco: PostgreSQL.

## Comunicação

- Síncrona: HTTP entre cliente e APIs.
- Assíncrona: RabbitMQ para eventos de integração.
- Contrato interno de aplicação: `Result<T>` para todos os handlers/casos de uso.

### Estratégia inicial entre os cinco serviços

- `Sales Service` consulta `Products Service` e `Users Service` por contratos síncronos de aplicação para validar referências externas na Fase 1.
- `Carts Service` depende de contratos síncronos de `Users Service` e `Products Service` para composição e consistência do carrinho.
- `Auth Service` e `Users Service` compartilham a fronteira de identidade, mas preservam contratos separados para autenticação e cadastro.
- Eventos assíncronos ficam reservados para mudanças de estado relevantes, começando por `SaleCreated`, `SaleModified`, `SaleCancelled` e `ItemCancelled`.
- Enquanto a solução estiver em backend modular único, essas fronteiras podem ser representadas por contratos de aplicação entre módulos, sem exigir chamadas HTTP reais dentro do mesmo processo.

## Governança de API

- Rate limiting configurável por política, com regras mais restritivas para autenticação e operações sensíveis.
- Endpoints operacionais separados do tráfego funcional, incluindo `/health/live` e `/health/ready`.
- Padronização de erros com `Result<T>` internamente e `ProblemDetails` ou contrato HTTP consistente externamente.
- Correlação distribuída por `traceId` e `correlationId` em logs, mensageria e respostas.
- Suporte obrigatório a idempotência em operações críticas orientadas a comando, principalmente criação e cancelamento.

Como a API será interna ao ecossistema, a estratégia inicial é evitar versionamento explícito na rota e priorizar compatibilidade evolutiva, rollout coordenado e contratos assíncronos versionados.

## Mapeamento do Template Base

O template de referência já traz uma separação importante que deve ser preservada e evoluída no repositório principal:

- `Ambev.DeveloperEvaluation.Domain`: núcleo de domínio com entidades, enums, eventos e regras de negócio.
- `Ambev.DeveloperEvaluation.Application`: casos de uso, contratos, DTOs, validações e orquestração da aplicação.
- `Ambev.DeveloperEvaluation.Common`: componentes compartilhados, utilitários transversais, segurança, logging, validação e health checks.
- `Ambev.DeveloperEvaluation.ORM`: persistência, mapeamentos, contexto de dados, repositórios e integrações com bancos.
- `Ambev.DeveloperEvaluation.IoC`: composição da aplicação, registro de dependências e configuração de módulos.
- `Ambev.DeveloperEvaluation.WebApi`: exposição HTTP, controllers/endpoints, middlewares e configuração da API.

Na arquitetura alvo, esses projetos deixam de ser apenas detalhes de infraestrutura do template e passam a compor a fundação padrão de cada serviço ou módulo principal da solução.

Importante: `Ambev.DeveloperEvaluation.WebApi` é o host genérico herdado do template e, na Fase 1, pode funcionar apenas como casca do backend modular único. Ele não deve ser tratado automaticamente como `GatewayApi`.

Quando houver separação física por serviço, a evolução esperada é explicitar hosts por contexto, por exemplo:

- `Ambev.DeveloperEvaluation.Sales.WebApi`
- `Ambev.DeveloperEvaluation.Products.WebApi`
- `Ambev.DeveloperEvaluation.Carts.WebApi`
- `Ambev.DeveloperEvaluation.Users.WebApi`
- `Ambev.DeveloperEvaluation.Auth.WebApi`

Uma `GatewayApi` só deve existir se houver responsabilidade real de edge aggregation, roteamento, autenticação central de borda ou políticas transversais de entrada. Se isso não existir, o nome correto é API do próprio serviço, não gateway.

## Convenção de nomenclatura

- A solução manterá o padrão nominal do template.
- Os projetos devem seguir o prefixo `Ambev.DeveloperEvaluation.*`.
- Novos módulos e testes devem respeitar a mesma convenção para preservar consistência estrutural e facilitar comparação com o template de referência.

## Estrutura Lógica Recomendada

Cada serviço deverá seguir a separação abaixo:

- `Domain`: entidades, value objects, eventos, regras de negócio.
- `Application`: casos de uso, contratos, DTOs, validações, `Result<T>`.
- `Common`: abstrações e componentes transversais compartilhados.
- `Infrastructure`: persistência, mensageria, observabilidade, adapters, incluindo `ORM`.
- `IoC`: composição e registro de dependências.
- `Api`: controllers/endpoints, filtros, mapeamento HTTP.

Na implementação inicial do Épico 3, a materialização dessas fronteiras começa em `Application`, com contratos específicos por serviço (`Sales`, `Products`, `Carts`, `Users` e `Auth`), e em `Domain`, com o agregado de vendas e seus eventos de negócio.

## Decisões Técnicas Iniciais

### Result Pattern

`Result<T>` será o padrão oficial de retorno da aplicação, com metadados mínimos:

- `IsSuccess`
- `Value`
- `Errors`
- `ErrorType`
- `StatusCode` opcional para mapeamento HTTP

### Persistência

- PostgreSQL para dados transacionais de `Sales`, `Products`, `Carts`, `Users` e `Auth`.
- MongoDB como parte da entrega inicial para `event log` e auditoria da venda dentro do `Sales Service`, sem criar um sexto serviço público fora da spec.
- Migrações versionadas por serviço.

### Mensageria

- RabbitMQ com exchanges por contexto.
- Contratos de eventos versionados.
- Consumer idempotente com rastreamento de mensagens processadas.
- Publicação confiável com estratégia de retry controlado e observabilidade do processamento.
- Deduplicação persistida do consumo em RabbitMQ para impedir reprocessamento da mesma mensagem.

### Observabilidade

- Logs estruturados com Serilog.
- Emissão primária em Console JSON (`stdout`).
- Seq como backend de consulta e retenção operacional da entrega local via `docker-compose`.
- Health checks para banco, broker e dependências críticas.
- Correlação por `traceId` e `correlationId`.
- Métricas básicas de latência, throughput e falhas por endpoint.
- Exposição de readiness e liveness para operação em Docker/Kubernetes.
- Health checks separados por objetivo: processo vivo, prontidão operacional e dependências críticas.

Campos mínimos obrigatórios nos logs estruturados:

- `timestamp`
- `level`
- `message`
- `exception`
- `traceId`
- `correlationId`
- `requestPath`
- `httpMethod`
- `statusCode`
- `serviceName`
- `environment`

### Resiliência e borda HTTP

- Rate limiting por política e por ambiente.
- Timeouts explícitos, cancelamento cooperativo e tratamento de exceções não tratadas.
- Cabeçalhos de segurança, CORS configurável e validação centralizada.
- Idempotência para comandos críticos, especialmente criação e cancelamento.
- Armazenamento de chaves de idempotência HTTP em PostgreSQL.

### Polly na arquitetura

- Polly deve ser aplicado em integrações HTTP, acesso a broker e outros pontos de falha transitória fora do domínio.
- Políticas recomendadas: `Timeout`, `Retry` com backoff exponencial e jitter, `CircuitBreaker` e `Fallback` apenas quando houver comportamento degradado aceitável.
- O registro das políticas deve ficar centralizado na composição (`IoC`), evitando duplicação nos handlers.
- As políticas devem ser observáveis, com logs e métricas por tentativa, abertura de circuito e timeout.
- Polly não deve envolver regras de negócio puras, validações ou acesso local em memória.

## Estratégia de Entrega

Para o desafio, a entrega pode começar com um único deployable backend organizado em módulos internos, desde que a separação arquitetural e os contratos já deixem explícitos os cinco micro-serviços exigidos pela spec. Essa abordagem reduz risco de prazo e preserva a visão-alvo.