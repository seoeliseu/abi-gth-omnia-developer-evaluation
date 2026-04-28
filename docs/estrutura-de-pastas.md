# Estrutura de pastas

## Visão geral

O repositório está organizado para separar claramente código compartilhado, módulos de negócio, testes e artefatos operacionais locais.

```text
.
|-- src
|   |-- BuildingBlocks
|   |   |-- Common
|   |   |-- Domain
|   |   |-- IoC
|   |   |-- ORM
|   |   `-- ServiceDefaults
|   `-- Modules
|       |-- Auth
|       |-- Carts
|       |-- Products
|       |-- Sales
|       `-- Users
|-- tests
|   |-- Ambev.DeveloperEvaluation.Unit
|   |-- Ambev.DeveloperEvaluation.Integration
|   `-- Ambev.DeveloperEvaluation.Functional
|-- docs
|   |-- arquitetura.md
|   `-- estrutura-de-pastas.md
|-- ops
|   `-- docker
|-- Dockerfile
|-- docker-compose.yml
|-- Directory.Build.props
|-- Directory.Packages.props
`-- Ambev.DeveloperEvaluation.slnx
```

## `src/BuildingBlocks`

Concentra o que é compartilhado entre os contextos:

- `Common`: tipos compartilhados, abstrações e contratos transversais.
- `Domain`: base de domínio compartilhada.
- `IoC`: composição de dependências, mensageria, resiliência e integração entre módulos.
- `ORM`: `DbContext`, entidades de persistência, migrações e serviços relacionados a banco.
- `ServiceDefaults`: hosting, middlewares, resultado HTTP, health checks e comportamento padrão dos hosts.

## `src/Modules`

Cada módulo segue o mesmo desenho de pastas:

```text
Modules/<Context>
|-- Application
|-- Domain
|-- Infrastructure
`-- WebApi
```

Responsabilidade de cada camada:

- `Application`: casos de uso, serviços de aplicação, filtros de consulta e contratos.
- `Domain`: entidades, regras de negócio, eventos e validações centrais.
- `Infrastructure`: implementações concretas de acesso a dados e integrações do contexto.
- `WebApi`: controllers, inicialização do host, appsettings e borda HTTP.

## `tests`

Os testes estão separados pelo tipo de validação:

- `Ambev.DeveloperEvaluation.Unit`: valida regras isoladas e comportamento de classes individuais.
- `Ambev.DeveloperEvaluation.Integration`: valida persistência, mensageria e integrações reais com Testcontainers.
- `Ambev.DeveloperEvaluation.Functional`: sobe os hosts e testa a API fim a fim.

## `ops`

- `ops/docker`: artefatos auxiliares do ambiente local, como scripts de inicialização do PostgreSQL.

## Arquivos raiz importantes

- `Ambev.DeveloperEvaluation.slnx`: solução principal usada para build e testes.
- `Dockerfile`: imagem base usada para os hosts `WebApi`.
- `docker-compose.yml`: ambiente local completo com APIs, PostgreSQL, MongoDB, RabbitMQ e Seq.
- `.env.example`: modelo opcional para customizar variáveis do Compose.
- `Directory.Build.props` e `Directory.Packages.props`: centralização de configuração de build e versões de pacotes.

## Como navegar no código

- Para regras de negócio de vendas, começar em `src/Modules/Sales/Domain` e `src/Modules/Sales/Application`.
- Para endpoints HTTP, começar em `src/Modules/<Context>/WebApi/Controllers`.
- Para mensageria e resiliência, começar em `src/BuildingBlocks/IoC`.
- Para persistência relacional e outbox, começar em `src/BuildingBlocks/ORM`.
- Para configuração transversal do host, começar em `src/BuildingBlocks/ServiceDefaults`.