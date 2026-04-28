# Arquitetura

## Visão geral

O projeto está organizado como um backend em .NET 8 com separação física por contexto de negócio dentro de um único repositório. Cada contexto possui suas próprias camadas `Application`, `Domain`, `Infrastructure` e `WebApi`, enquanto os elementos compartilhados ficam em `src/BuildingBlocks`.

Os cinco hosts HTTP são:

- `Sales`
- `Products`
- `Carts`
- `Users`
- `Auth`

Os principais componentes de infraestrutura são:

- PostgreSQL para persistência transacional.
- MongoDB para armazenamento complementar de auditoria e event log no fluxo de vendas.
- Redis para cache-aside do catálogo de produtos.
- RabbitMQ para integração assíncrona entre `Sales` e os consumidores `Products`, `Carts` e `Users`.
- Seq para consulta de logs estruturados.

## Diagrama de componentes

```mermaid
flowchart LR
    client[Cliente ou consumidor HTTP]

    subgraph apis[Hosts Web API]
        sales[Sales API]
        products[Products API]
        carts[Carts API]
        users[Users API]
        auth[Auth API]
    end

    subgraph building[BuildingBlocks]
        common[Common e Domain]
        ioc[IoC e Mensageria]
        orm[ORM]
        defaults[ServiceDefaults]
    end

    subgraph data[Infraestrutura]
        pg[(PostgreSQL)]
        mongo[(MongoDB)]
        redis[(Redis)]
        rabbit[(RabbitMQ)]
        seq[(Seq)]
    end

    client --> sales
    client --> products
    client --> carts
    client --> users
    client --> auth

    sales --> common
    products --> common
    carts --> common
    users --> common
    auth --> common

    common --> ioc
    ioc --> orm
    ioc --> defaults

    sales --> pg
    products --> pg
    carts --> pg
    users --> pg
    auth --> pg
    sales --> mongo
    sales --> redis
    products --> redis
    sales --> rabbit
    products --> rabbit
    carts --> rabbit
    users --> rabbit

    sales --> seq
    products --> seq
    carts --> seq
    users --> seq
    auth --> seq
```

## Organização em camadas

- `WebApi`: controllers, composição HTTP, rate limiting, timeout, health checks e mapeamento de `Result<T>` para respostas HTTP.
- `Application`: casos de uso, contratos, orquestração e regras de aplicação.
- `Domain`: entidades, regras centrais, eventos de domínio e invariantes.
- `Infrastructure`: integração externa por contexto, persistência e implementações concretas.
- `BuildingBlocks`: componentes compartilhados de mensageria, resiliência, persistência, middlewares e hosting.

## Superfície de API

As rotas estão separadas por contexto e seguem convenção REST, com exceções explícitas para autenticação e comandos de cancelamento em `Sales`.

```mermaid
flowchart TB
    subgraph sales[Sales API - /api/sales]
        s1["POST /"]
        s2["GET /"]
        s3["GET /{saleId}"]
        s4["PUT /{saleId}"]
        s5["DELETE /{saleId}"]
        s6["POST /{saleId}/cancel"]
        s7["POST /{saleId}/items/{saleItemId}/cancel"]
    end

    subgraph products[Products API - /api/products]
        p1["GET /"]
        p2["POST /"]
        p3["GET /{id}"]
        p4["PUT /{id}"]
        p5["DELETE /{id}"]
        p6["GET /categories"]
        p7["GET /category/{category}"]
    end

    subgraph carts[Carts API - /api/carts]
        c1["GET /"]
        c2["POST /"]
        c3["GET /{id}"]
        c4["PUT /{id}"]
        c5["DELETE /{id}"]
    end

    subgraph users[Users API - /api/users]
        u1["GET /"]
        u2["POST /"]
        u3["GET /{id}"]
        u4["PUT /{id}"]
        u5["DELETE /{id}"]
    end

    subgraph auth[Auth API - /api/auth]
        a1["POST /login"]
    end
```

## RabbitMQ e integração assíncrona

O fluxo assíncrono atual é centrado em `Sales`:

- `Sales` persiste eventos na outbox.
- `SalesOutboxPublisherWorker` publica os eventos pendentes no RabbitMQ.
- `Products`, `Carts` e `Users` se inscrevem nos eventos de vendas.
- Os consumidores usam deduplicação persistida por `messageId`.
- Cada fila possui DLQ explícita com sufixo `.error`.

Eventos publicados hoje:

- `SaleCreatedEvent`
- `SaleModifiedEvent`
- `SaleCancelledEvent`
- `ItemCancelledEvent`

```mermaid
flowchart LR
    subgraph publisher[Origem dos eventos]
        salesApi[Sales API]
        outbox[SalesOutboxPublisherWorker]
        exchange[(Sales integration events)]
    end

    subgraph consumers[Consumidores]
        qProducts[developer-evaluation.products.webapi]
        qProductsErr[developer-evaluation.products.webapi.error]
        qCarts[developer-evaluation.carts.webapi]
        qCartsErr[developer-evaluation.carts.webapi.error]
        qUsers[developer-evaluation.users.webapi]
        qUsersErr[developer-evaluation.users.webapi.error]
    end

    salesApi --> outbox
    outbox -->|SaleCreatedEvent\nSaleModifiedEvent\nSaleCancelledEvent\nItemCancelledEvent| exchange

    exchange --> qProducts
    exchange --> qCarts
    exchange --> qUsers

    qProducts -->|falha permanente| qProductsErr
    qCarts -->|falha permanente| qCartsErr
    qUsers -->|falha permanente| qUsersErr
```

## Decisões arquiteturais relevantes

- O repositório está pronto para evolução incremental por contexto, mas ainda opera como uma solução única para desenvolvimento local.
- `Sales` concentra as regras mais ricas do domínio e também a publicação dos eventos de integração.
- Os consumidores de RabbitMQ em `Products`, `Carts` e `Users` tratam eventos de vendas de forma idempotente.
- `Products` usa cache-aside com Redis para consultas frequentes de catálogo, categorias e detalhes.
- A observabilidade combina logs estruturados, métricas de resiliência e métricas de desvio para DLQ.
- O ambiente de execução local é baseado em Docker Compose.