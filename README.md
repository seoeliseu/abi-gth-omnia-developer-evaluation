# abi-gth-omnia-developer-evaluation

Backend de gestão de vendas para desafio técnico, planejado com .NET 8, DDD, `Result<T>`, PostgreSQL, MongoDB, RabbitMQ e prontidão para Docker/Kubernetes.

## Hosts HTTP

- `Ambev.DeveloperEvaluation.Sales.WebApi`
- `Ambev.DeveloperEvaluation.Products.WebApi`
- `Ambev.DeveloperEvaluation.Carts.WebApi`
- `Ambev.DeveloperEvaluation.Users.WebApi`
- `Ambev.DeveloperEvaluation.Auth.WebApi`
- `Ambev.DeveloperEvaluation.ServiceDefaults` como base compartilhada de borda HTTP

## Estrutura do repositório

- `docs/`: documentação funcional, arquitetural e backlog.
- `src/`: código-fonte da aplicação.
- `tests/`: testes automatizados.
- `.claude/`: runbook de execução do repositório.

## Documentação inicial

- `docs/00-visao-geral.md`
- `docs/01-requisitos.md`
- `docs/02-arquitetura.md`
- `docs/03-roadmap.md`
- `docs/04-tasks.md`
- `docs/05-microservices-e-dados.md`
- `docs/06-containerizacao.md`

## Direções já definidas

- Backend somente.
- Stack principal: C#/.NET 8.
- Naming dos projetos seguindo o template: `Ambev.DeveloperEvaluation.*`.
- Comunicação assíncrona via RabbitMQ.
- Rebus como biblioteca de mensageria.
- EF Core como ORM relacional.
- MediatR/Mediator e AutoMapper na camada de aplicação.
- xUnit, NSubstitute e Faker/Bogus para testes.
- Cada microserviço com banco próprio.
- MongoDB faz parte da versão entregável, não apenas de uma evolução futura.
- `Result<T>` como padrão de retorno da aplicação.
- Preparação para execução local com Docker Compose.
- Preparação para deploy em Kubernetes.

## Estratégia de branches

- O desenvolvimento seguirá Git Flow simplificado por fase.
- Cada fase relevante terá branch própria.
- Quando a fase estiver validada, a branch será integrada em `master`.
- A segregação mínima seguirá: banco + ORM, camada de serviços, regras de negócio e testes.

## Próximos passos

1. Criar a solução base em `src/` com separação por camadas.
2. Implementar `Result<T>` e contratos compartilhados.
3. Modelar o domínio de vendas e suas regras.
4. Configurar persistência, mensageria e testes.

## Execução com contêineres

- `docker compose up --build` para subir os cinco serviços, PostgreSQL, MongoDB, RabbitMQ e Seq.
- Portas locais das APIs: `8081` a `8085`.
- Seq disponível em `http://localhost:5341` e RabbitMQ Management em `http://localhost:15672`.
