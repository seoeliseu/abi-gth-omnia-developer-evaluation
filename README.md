# abi-gth-omnia-developer-evaluation

Backend de gestão de vendas implementado em .NET 8, com separação por contextos de negócio, mensageria com RabbitMQ, persistência em PostgreSQL e MongoDB, cache-aside com Redis para catálogo de produtos, observabilidade com Seq e execução local via Docker Compose.

## Visão do projeto

O repositório entrega cinco APIs:

- `Sales`
- `Products`
- `Carts`
- `Users`
- `Auth`

Além do CRUD dos contextos principais, a solução inclui:

- regras de negócio de vendas com desconto por quantidade;
- idempotência em comandos críticos de `Sales`;
- outbox para publicação confiável de eventos;
- consumidores idempotentes com deduplicação persistida;
- DLQ explícita por fila do Rebus;
- health checks, `ProblemDetails`, `correlationId`, `traceId`, rate limiting e políticas de resiliência.

## Documentação

- Arquitetura: [docs/arquitetura.md](c:/r/abi-gth-omnia-developer-evaluation/docs/arquitetura.md)
- Estrutura de pastas: [docs/estrutura-de-pastas.md](c:/r/abi-gth-omnia-developer-evaluation/docs/estrutura-de-pastas.md)
- API HTTP: [docs/api.md](c:/r/abi-gth-omnia-developer-evaluation/docs/api.md)

## Pré-requisitos

- .NET SDK 8
- Docker Desktop ou Docker Engine com Compose
- portas `5432`, `5341`, `5672`, `15672` e `8081` a `8085` disponíveis

## Como iniciar

### Caminho recomendado no Windows com Docker Desktop

No Windows com Docker Desktop, o caminho mais estável é usar o fluxo sequencial já configurado no repositório. Ele sobe a infraestrutura primeiro, builda as imagens uma a uma e só depois executa o `docker compose up -d`, evitando cancelamentos intermitentes durante `dotnet restore` e `dotnet publish` concorrentes.

Via task do VS Code:

```text
Run Task -> docker: compose up sequential
```

Via PowerShell:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\compose-up-sequential.ps1
```

### Caminho direto com Docker Compose

Se o seu ambiente Docker estiver estável para builds concorrentes, você também pode subir todo o ambiente local diretamente da raiz do repositório:

```powershell
docker compose up --build
```

Se quiser customizar variáveis locais antes da subida:

```powershell
Copy-Item .env.example .env
docker compose --env-file .env up --build
```

O ambiente sobe:

- cinco hosts `WebApi`;
- PostgreSQL com bases segregadas por serviço;
- MongoDB;
- Redis;
- RabbitMQ;
- Seq.

## Endpoints locais

- Sales API: `http://localhost:8081/api/sales`
- Products API: `http://localhost:8082/api/products`
- Carts API: `http://localhost:8083/api/carts`
- Users API: `http://localhost:8084/api/users`
- Auth API: `http://localhost:8085/api/auth/login`
- Seq: `http://localhost:5341`
- Redis: `localhost:6379`
- RabbitMQ Management: `http://localhost:15672`

## Build e testes

Build da solução:

```powershell
dotnet build .\Ambev.DeveloperEvaluation.slnx
```

Testes automatizados:

```powershell
dotnet test .\Ambev.DeveloperEvaluation.slnx
```

Para rodar os testes sem rebuild prévio:

```powershell
dotnet test .\Ambev.DeveloperEvaluation.slnx --no-build
```

Os testes de integração e funcionais usam Testcontainers, então o Docker precisa estar ativo durante a execução.

## Abrir no Visual Studio

Abra a solution [Ambev.DeveloperEvaluation.slnx](c:/r/abi-gth-omnia-developer-evaluation/Ambev.DeveloperEvaluation.slnx).

## Navegação rápida

- Para entender os componentes, APIs e filas RabbitMQ, consulte [docs/arquitetura.md](c:/r/abi-gth-omnia-developer-evaluation/docs/arquitetura.md).
- Para entender onde fica cada parte do código, consulte [docs/estrutura-de-pastas.md](c:/r/abi-gth-omnia-developer-evaluation/docs/estrutura-de-pastas.md).
- Para ver rotas, payloads, respostas e status code, consulte [docs/api.md](c:/r/abi-gth-omnia-developer-evaluation/docs/api.md).

