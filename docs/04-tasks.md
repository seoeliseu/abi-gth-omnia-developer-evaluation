# Backlog Inicial

## Épico 1. Fundação do repositório

- [x] Criar estrutura `src/`, `tests/`, `docs/`, `.claude/`.
- [x] Atualizar README com visão do projeto e passo a passo de execução.
- [x] Definir convenções de naming, camadas e retorno com `Result<T>`.
- [x] Preparar solução para Docker Compose.
- [x] Padronizar nomes dos projetos com prefixo `Ambev.DeveloperEvaluation.*`.
- [x] Definir estratégia de branches por fase e critérios de merge em `master`.

Commits sugeridos:

- `chore: cria estrutura inicial do repositorio`
- `docs: atualiza readme e backlog inicial`
- `chore: define convencoes da solution`
- `chore: define estrategia de branches por fase`

## Épico 2. Arquitetura e cross-cutting

- [x] Definir projetos base por camada.
- [x] Implementar `Result<T>` compartilhado.
- [x] Configurar Serilog com saída em Console JSON.
- [x] Configurar Seq como backend local de consulta de logs.
- [x] Definir campos obrigatórios e correlação dos logs estruturados.
- [x] Configurar health checks.
- [x] Configurar integração com RabbitMQ.
- [x] Preparar artefatos iniciais para Kubernetes.

Commits sugeridos:

- `chore: adiciona projetos base por camada`
- `feat: implementa result pattern compartilhado`
- `feat: configura serilog com console json`
- `feat: configura seq e correlacao de logs`
- `feat: adiciona health checks iniciais`
- `feat: configura integracao inicial com rabbitmq`

## Épico 3. Base dos micro-serviços

- [x] Definir fronteiras e contratos do `Sales Service`.
- [x] Definir fronteiras e contratos do `Products Service`.
- [x] Definir fronteiras e contratos do `Carts Service`.
- [x] Definir fronteiras e contratos do `Users Service`.
- [x] Definir fronteiras e contratos do `Auth Service`.
- [x] Estruturar a solução para comportar os cinco serviços do desafio sem quebrar o prefixo `Ambev.DeveloperEvaluation.*`.
- [x] Definir estratégia de comunicação entre serviços e responsabilidades síncronas vs. assíncronas.

### Recorte inicial do `Sales Service`

- [x] Modelar entidade `Sale`.
- [x] Modelar entidade `SaleItem`.
- [x] Implementar regras de desconto por faixa.
- [x] Implementar restrição máxima de 20 itens idênticos.
- [x] Implementar cancelamento de item e venda.
- [x] Criar eventos de domínio e integração.

Commits sugeridos:

- `chore: define fronteiras dos cinco microservicos`
- `chore: estrutura solution para os cinco servicos`
- `feat: adiciona entidade sale`
- `feat: adiciona entidade sale item`
- `feat: implementa regras de desconto`
- `feat: implementa cancelamento de venda e item`
- `feat: adiciona eventos de dominio e integracao`

## Épico 4. Aplicação e API

- [ ] Criar casos de uso de criar venda.
- [ ] Criar casos de uso de buscar por id e listar vendas.
- [ ] Criar casos de uso de atualizar venda.
- [ ] Criar casos de uso de cancelar venda e item.
- [ ] Implementar paginação, ordenação e filtros.
- [ ] Mapear `Result<T>` para respostas HTTP.
- [ ] Implementar contrato de erro único com `ProblemDetails` ou equivalente.
- [ ] Configurar rate limiting por política.
- [ ] Expor endpoints `/health/live` e `/health/ready`.
- [ ] Propagar `correlationId` e `traceId` em requests, logs e respostas.
- [ ] Implementar idempotência para comandos críticos de criação e cancelamento.
- [ ] Definir política de timeout e cancelamento cooperativo.

Commits sugeridos:

- `feat: adiciona caso de uso de criar venda`
- `feat: adiciona consulta e listagem de vendas`
- `feat: adiciona atualizacao e cancelamento de vendas`
- `feat: adiciona filtros paginacao e ordenacao`
- `feat: mapeia result pattern para http`
- `feat: implementa problem details e correlacao`
- `feat: configura rate limiting e endpoints de health`
- `feat: implementa idempotencia em comandos criticos`

## Épico 4.1 Demais APIs do desafio

- [ ] Implementar CRUD de `Products` conforme a spec.
- [ ] Implementar endpoint de categorias e consulta por categoria em `Products`.
- [ ] Implementar CRUD de `Carts` conforme a spec.
- [ ] Implementar CRUD de `Users` conforme a spec.
- [ ] Implementar `POST /auth/login` conforme a spec.
- [ ] Garantir paginação, ordenação, filtros e contrato de erro compatíveis nos quatro serviços de apoio.

Commits sugeridos:

- `feat: implementa api de products`
- `feat: implementa api de carts`
- `feat: implementa api de users`
- `feat: implementa api de auth`
- `feat: alinha contratos das apis auxiliares com a spec`

## Épico 5. Persistência

- [ ] Configurar PostgreSQL para dados transacionais.
- [ ] Definir agregados e mapeamentos ORM.
- [ ] Criar migrações iniciais.
- [ ] Implementar MongoDB na versão entregável como store de `event log` e auditoria da venda.
- [ ] Definir estratégia de outbox ou publicação confiável.
- [ ] Definir armazenamento de chaves de idempotência HTTP em PostgreSQL.
- [ ] Definir deduplicação persistida de mensagens consumidas no RabbitMQ.

Commits sugeridos:

- `feat: configura persistencia postgres`
- `feat: adiciona mapeamentos orm e migracoes iniciais`
- `feat: configura mongodb para event log e auditoria`
- `feat: adiciona estrategia de outbox`
- `feat: adiciona idempotencia http e deduplicacao de mensagens`

## Épico 5.1 Resiliência e integrações

- [ ] Configurar Polly centralizado na composição da aplicação.
- [ ] Aplicar retry com backoff e jitter nas integrações externas.
- [ ] Aplicar timeout e circuit breaker em chamadas suscetíveis a falhas transitórias.
- [ ] Instrumentar logs e métricas das políticas de resiliência.

Commits sugeridos:

- `feat: configura polly na composicao da aplicacao`
- `feat: aplica retry timeout e circuit breaker`
- `feat: instrumenta logs e metricas de resiliencia`

## Épico 6. Testes

- [ ] Criar suíte de testes unitários para regras de desconto.
- [ ] Criar testes para limites de quantidade.
- [ ] Criar testes dos handlers principais.
- [ ] Criar ao menos um teste de integração do fluxo de criação.
- [ ] Criar infraestrutura base de Testcontainers para PostgreSQL.
- [ ] Criar infraestrutura base de Testcontainers para MongoDB.
- [ ] Criar infraestrutura base de Testcontainers para RabbitMQ.
- [ ] Criar testes de integração de persistência com banco real.
- [ ] Criar testes de integração de mensageria com broker real.
- [ ] Criar testes funcionais da API com `WebApplicationFactory` e Testcontainers.

Commits sugeridos:

- `test: adiciona testes das regras de desconto`
- `test: adiciona testes dos limites de quantidade`
- `test: adiciona testes dos handlers principais`
- `test: adiciona teste de integracao do fluxo de criacao`
- `test: adiciona fixtures base com testcontainers`
- `test: adiciona testes de integracao com banco e broker`
- `test: adiciona testes funcionais com api real`

## Épico 7. Entrega operacional

- [ ] Criar `docker-compose.yml` do repositório principal.
- [ ] Adicionar `Dockerfile` da API.
- [ ] Adicionar container do Seq ao `docker-compose`.
- [ ] Criar pasta `ops/k8s/` com manifests base.
- [ ] Exportar coleção para testes manuais.
- [ ] Revisar documentação final.

Commits sugeridos:

- `chore: cria docker compose da entrega`
- `chore: adiciona dockerfile da api`
- `chore: adiciona seq ao ambiente local`
- `chore: adiciona manifests iniciais de kubernetes`
- `docs: adiciona collection e revisa documentacao final`