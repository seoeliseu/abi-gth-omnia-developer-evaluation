# abi-gth-omnia-developer-evaluation

Backend de gestão de vendas para o desafio técnico, implementado em .NET 8 com DDD, `Result<T>`, PostgreSQL, MongoDB, RabbitMQ, Docker Compose e manifests base para Kubernetes.

## O que está entregue

- Cinco hosts HTTP: `Sales`, `Products`, `Carts`, `Users` e `Auth`.
- CRUD de `Sales`, incluindo remoção explícita e cancelamentos de venda e item.
- Regras de desconto por quantidade, idempotência, outbox e eventos de integração.
- `ProblemDetails`, `correlationId`, `traceId`, rate limit, readiness/liveness e resiliência com Polly.
- Testes unitários, de integração e funcionais com Testcontainers.

## Estrutura do repositório

- `src/BuildingBlocks`: cross-cutting concerns, ORM, IoC e borda HTTP.
- `src/Modules`: módulos `Auth`, `Carts`, `Products`, `Sales` e `Users`.
- `tests`: projetos `Unit`, `Integration` e `Functional`.
- `docs`: visão geral, requisitos, arquitetura, dados, containerização e checklist final.
- `ops/k8s`: manifests base para Kubernetes.

## Pré-requisitos

- .NET SDK 8.
- Docker Desktop ou Docker Engine com Compose.
- Porta `5432` livre para PostgreSQL local do Compose.
- Portas `8081` a `8085`, `15672` e `5341` livres.

## Configuração local

Não há arquivo `.env` obrigatório para rodar localmente com o Compose padrão.

O `docker-compose.yml` já sobe:

- `developer-evaluation-sales-api`
- `developer-evaluation-products-api`
- `developer-evaluation-carts-api`
- `developer-evaluation-users-api`
- `developer-evaluation-auth-api`
- PostgreSQL com bases segregadas por serviço
- MongoDB
- RabbitMQ
- Seq

## Executar com Docker Compose

Use na raiz do repositório:

```powershell
docker compose up --build
```

Endpoints locais:

- Sales API: `http://localhost:8081`
- Products API: `http://localhost:8082`
- Carts API: `http://localhost:8083`
- Users API: `http://localhost:8084`
- Auth API: `http://localhost:8085`
- Seq: `http://localhost:5341`
- RabbitMQ Management: `http://localhost:15672`

## Build local

```powershell
dotnet build .\Ambev.DeveloperEvaluation.slnx
```

## Testes automatizados

Os testes de integração e funcionais usam Testcontainers. Docker precisa estar ativo antes da execução.

```powershell
dotnet test .\Ambev.DeveloperEvaluation.slnx
```

Para rodar sem rebuild prévio:

```powershell
dotnet test .\Ambev.DeveloperEvaluation.slnx --no-build
```

## Abrir no Visual Studio

Abra a solution [Ambev.DeveloperEvaluation.slnx](c:/r/abi-gth-omnia-developer-evaluation/Ambev.DeveloperEvaluation.slnx).

A árvore da solution está organizada por:

- `src/BuildingBlocks`
- `src/Modules/Auth`
- `src/Modules/Carts`
- `src/Modules/Products`
- `src/Modules/Sales`
- `src/Modules/Users`
- `tests`

## Documentação complementar

- `docs/00-visao-geral.md`
- `docs/01-requisitos.md`
- `docs/02-arquitetura.md`
- `docs/05-microservices-e-dados.md`
- `docs/06-containerizacao.md`
- `docs/10-checklist-final.md`
