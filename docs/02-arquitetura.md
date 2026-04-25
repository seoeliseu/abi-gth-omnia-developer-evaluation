# Arquitetura Alvo

## Direção Arquitetural

A solução será organizada como um backend orientado a domínio, com início em um serviço principal de vendas e estrutura preparada para extração gradual de microserviços. A prioridade do desafio é entregar o fluxo de vendas com qualidade sem comprometer evolutividade operacional.

## Bounded Contexts Planejados

### 1. Identity Service

- Responsável por autenticação, autorização, usuários e perfis.
- Baseado no template já existente para `Auth` e `Users`.
- Banco: PostgreSQL.

### 2. Sales Service

- Contexto principal do desafio.
- Responsável por venda, itens da venda, cálculo de descontos, cancelamentos e publicação de eventos.
- Banco: PostgreSQL.

### 3. Catalog Service

- Responsável por cadastro e consulta de produtos e, futuramente, clientes e filiais referenciados externamente.
- Exposição inicial pode ser interna ao ecossistema.
- Banco: PostgreSQL.

### 4. Audit/Projection Service

- Responsável por armazenar `event log` e auditoria da venda na entrega inicial, podendo evoluir para projeções de leitura quando necessário.
- Banco: MongoDB.

## Comunicação

- Síncrona: HTTP entre cliente e APIs.
- Assíncrona: RabbitMQ para eventos de integração.
- Contrato interno de aplicação: `Result<T>` para todos os handlers/casos de uso.

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

## Decisões Técnicas Iniciais

### Result Pattern

`Result<T>` será o padrão oficial de retorno da aplicação, com metadados mínimos:

- `IsSuccess`
- `Value`
- `Errors`
- `ErrorType`
- `StatusCode` opcional para mapeamento HTTP

### Persistência

- PostgreSQL para dados transacionais dos serviços de negócio.
- MongoDB como parte da entrega inicial para `event log` e auditoria da venda.
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

Para o desafio, a entrega pode começar com um único deployable backend organizado em módulos internos, desde que a separação arquitetural e os contratos já deixem explícita a futura extração para microserviços. Essa abordagem reduz risco de prazo e preserva a visão-alvo.