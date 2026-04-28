# API

## Convenções gerais

- Base local das APIs:
  - `Sales`: `http://localhost:8081`
  - `Products`: `http://localhost:8082`
  - `Carts`: `http://localhost:8083`
  - `Users`: `http://localhost:8084`
  - `Auth`: `http://localhost:8085`
- Os exemplos abaixo usam JSON em `camelCase`, que é o formato exposto pela API.
- As rotas de listagem aceitam paginação por `_page`, tamanho por `_size` e ordenação por `_order`.
- As rotas mutáveis de `Sales` aceitam o header opcional `Idempotency-Key`.

## Resposta de erro padrão

Quando a requisição falha, a API responde com `ProblemDetails` e extensões padronizadas.

Exemplo:

```json
{
  "type": "https://httpstatuses.com/400",
  "title": "Validation",
  "status": 400,
  "detail": "O payload informado é inválido.",
  "errorType": "Validation",
  "errors": [
    {
      "codigo": "sales.invalid-request",
      "mensagem": "O payload informado é inválido."
    }
  ],
  "correlationId": "4b1f915b2f214a34a4a7b73d7a9a5e0a",
  "traceId": "5ce71bca8c67d4eab7a85fa814f4fe49"
}
```

Status de erro possíveis, conforme o mapeamento central da aplicação:

- `400 Bad Request`: erro de validação.
- `401 Unauthorized`: falha de autenticação.
- `403 Forbidden`: acesso proibido.
- `404 Not Found`: recurso inexistente.
- `409 Conflict`: conflito de estado.
- `422 Unprocessable Entity`: regra de negócio violada.
- `500 Internal Server Error`: erro não tratado.

## Auth API

Base path: `/api/auth`

### `POST /api/auth/login`

Autentica um usuário e devolve um token.

Exemplo de request:

```json
{
  "username": "johnd",
  "password": "123456"
}
```

Exemplo de response `200 OK`:

```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `401 Unauthorized`
- `500 Internal Server Error`

## Users API

Base path: `/api/users`

### `GET /api/users`

Lista usuários com paginação e filtros.

Query params disponíveis:

- `_page`
- `_size`
- `_order`
- `username`
- `email`
- `role`
- `status`

Exemplo:

`GET /api/users?_page=1&_size=10&_order=username desc&status=Active`

Exemplo de response `200 OK`:

```json
{
  "data": [
    {
      "id": 1,
      "email": "john@example.com",
      "username": "johnd",
      "password": "123456",
      "name": {
        "firstname": "John",
        "lastname": "Doe"
      },
      "address": {
        "city": "São Paulo",
        "street": "Rua A",
        "number": 10,
        "zipcode": "01000-000",
        "geolocation": {
          "lat": "-23.5505",
          "long": "-46.6333"
        }
      },
      "phone": "+55 11 99999-9999",
      "status": "Active",
      "role": "Admin"
    }
  ],
  "totalItems": 1,
  "currentPage": 1,
  "totalPages": 1
}
```

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `500 Internal Server Error`

### `POST /api/users`

Cria um usuário.

Exemplo de request:

```json
{
  "email": "john@example.com",
  "username": "johnd",
  "password": "123456",
  "name": {
    "firstname": "John",
    "lastname": "Doe"
  },
  "address": {
    "city": "São Paulo",
    "street": "Rua A",
    "number": 10,
    "zipcode": "01000-000",
    "geolocation": {
      "lat": "-23.5505",
      "long": "-46.6333"
    }
  },
  "phone": "+55 11 99999-9999",
  "status": "Active",
  "role": "Admin"
}
```

Exemplo de response `201 Created`:

```json
{
  "id": 1,
  "email": "john@example.com",
  "username": "johnd",
  "password": "123456",
  "name": {
    "firstname": "John",
    "lastname": "Doe"
  },
  "address": {
    "city": "São Paulo",
    "street": "Rua A",
    "number": 10,
    "zipcode": "01000-000",
    "geolocation": {
      "lat": "-23.5505",
      "long": "-46.6333"
    }
  },
  "phone": "+55 11 99999-9999",
  "status": "Active",
  "role": "Admin"
}
```

Status possíveis:

- `201 Created`
- `400 Bad Request`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `GET /api/users/{id}`

Obtém o detalhe de um usuário.

Exemplo de response `200 OK`:

```json
{
  "id": 1,
  "email": "john@example.com",
  "username": "johnd",
  "password": "123456",
  "name": {
    "firstname": "John",
    "lastname": "Doe"
  },
  "address": {
    "city": "São Paulo",
    "street": "Rua A",
    "number": 10,
    "zipcode": "01000-000",
    "geolocation": {
      "lat": "-23.5505",
      "long": "-46.6333"
    }
  },
  "phone": "+55 11 99999-9999",
  "status": "Active",
  "role": "Admin"
}
```

Status possíveis:

- `200 OK`
- `404 Not Found`
- `500 Internal Server Error`

### `PUT /api/users/{id}`

Atualiza um usuário. O payload é o mesmo do `POST /api/users`.

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `DELETE /api/users/{id}`

Remove um usuário.

Exemplo de response `200 OK`:

```json
{
  "id": 1,
  "email": "john@example.com",
  "username": "johnd",
  "password": "123456",
  "name": {
    "firstname": "John",
    "lastname": "Doe"
  },
  "address": {
    "city": "São Paulo",
    "street": "Rua A",
    "number": 10,
    "zipcode": "01000-000",
    "geolocation": {
      "lat": "-23.5505",
      "long": "-46.6333"
    }
  },
  "phone": "+55 11 99999-9999",
  "status": "Inactive",
  "role": "Admin"
}
```

Status possíveis:

- `200 OK`
- `404 Not Found`
- `409 Conflict`
- `500 Internal Server Error`

## Products API

Base path: `/api/products`

### `GET /api/products`

Lista produtos com paginação e filtros.

Query params disponíveis:

- `_page`
- `_size`
- `_order`
- `category`
- `title`
- `_minPrice`
- `_maxPrice`

Exemplo:

`GET /api/products?_page=1&_size=10&category=electronics&_minPrice=100&_maxPrice=500`

Exemplo de response `200 OK`:

```json
{
  "data": [
    {
      "id": 1,
      "title": "Monitor 27",
      "price": 1299.9,
      "description": "Monitor IPS 27 polegadas",
      "category": "electronics",
      "image": "https://example.com/monitor.png",
      "rating": {
        "rate": 4.8,
        "count": 120
      },
      "active": true
    }
  ],
  "totalItems": 1,
  "currentPage": 1,
  "totalPages": 1
}
```

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `500 Internal Server Error`

### `POST /api/products`

Cria um produto.

Exemplo de request:

```json
{
  "title": "Monitor 27",
  "price": 1299.9,
  "description": "Monitor IPS 27 polegadas",
  "category": "electronics",
  "image": "https://example.com/monitor.png",
  "rating": {
    "rate": 4.8,
    "count": 120
  }
}
```

Exemplo de response `201 Created`:

```json
{
  "id": 1,
  "title": "Monitor 27",
  "price": 1299.9,
  "description": "Monitor IPS 27 polegadas",
  "category": "electronics",
  "image": "https://example.com/monitor.png",
  "rating": {
    "rate": 4.8,
    "count": 120
  },
  "active": true
}
```

Status possíveis:

- `201 Created`
- `400 Bad Request`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `GET /api/products/{id}`

Obtém o detalhe de um produto.

Status possíveis:

- `200 OK`
- `404 Not Found`
- `500 Internal Server Error`

### `PUT /api/products/{id}`

Atualiza um produto. O payload é o mesmo do `POST /api/products`.

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `DELETE /api/products/{id}`

Remove um produto.

Status possíveis:

- `200 OK`
- `404 Not Found`
- `409 Conflict`
- `500 Internal Server Error`

### `GET /api/products/categories`

Lista as categorias existentes.

Exemplo de response `200 OK`:

```json
[
  "electronics",
  "furniture",
  "books"
]
```

Status possíveis:

- `200 OK`
- `500 Internal Server Error`

### `GET /api/products/category/{category}`

Lista produtos de uma categoria específica com o mesmo envelope paginado de `GET /api/products`.

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `500 Internal Server Error`

## Carts API

Base path: `/api/carts`

### `GET /api/carts`

Lista carrinhos com paginação e filtros.

Query params disponíveis:

- `_page`
- `_size`
- `_order`
- `userId`
- `_minDate`
- `_maxDate`

Exemplo de response `200 OK`:

```json
{
  "data": [
    {
      "id": 1,
      "usuarioId": 10,
      "data": "2026-04-27T10:00:00Z",
      "produtos": [
        {
          "productId": 1,
          "quantidade": 2
        }
      ]
    }
  ],
  "totalItems": 1,
  "currentPage": 1,
  "totalPages": 1
}
```

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `500 Internal Server Error`

### `POST /api/carts`

Cria um carrinho.

Exemplo de request:

```json
{
  "userId": 10,
  "date": "2026-04-27T10:00:00Z",
  "products": [
    {
      "productId": 1,
      "quantidade": 2
    },
    {
      "productId": 2,
      "quantidade": 1
    }
  ]
}
```

Exemplo de response `201 Created`:

```json
{
  "id": 1,
  "usuarioId": 10,
  "data": "2026-04-27T10:00:00Z",
  "produtos": [
    {
      "productId": 1,
      "quantidade": 2
    },
    {
      "productId": 2,
      "quantidade": 1
    }
  ]
}
```

Status possíveis:

- `201 Created`
- `400 Bad Request`
- `404 Not Found`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `GET /api/carts/{id}`

Obtém o detalhe de um carrinho.

Status possíveis:

- `200 OK`
- `404 Not Found`
- `500 Internal Server Error`

### `PUT /api/carts/{id}`

Atualiza um carrinho. O payload é o mesmo do `POST /api/carts`.

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `DELETE /api/carts/{id}`

Remove um carrinho.

Exemplo de response `200 OK`:

```json
{
  "id": 1,
  "usuarioId": 10,
  "data": "2026-04-27T10:00:00Z",
  "produtos": [
    {
      "productId": 1,
      "quantidade": 2
    }
  ]
}
```

Status possíveis:

- `200 OK`
- `404 Not Found`
- `500 Internal Server Error`

## Sales API

Base path: `/api/sales`

### `GET /api/sales`

Lista vendas com paginação e filtros.

Query params disponíveis:

- `_page`
- `_size`
- `_order`
- `numero`
- `clienteNome`
- `filialNome`
- `cancelada`
- `_minDataVenda`
- `_maxDataVenda`

Exemplo:

`GET /api/sales?_page=1&_size=10&numero=VEN-1001&cancelada=false`

Exemplo de response `200 OK`:

```json
{
  "data": [
    {
      "id": "f4c5d58d-a7f5-45cc-a3ef-590ec1b4c87e",
      "numero": "VEN-1001",
      "dataVenda": "2026-04-27T13:00:00Z",
      "clienteId": 10,
      "clienteNome": "Cliente 10",
      "filialId": 1,
      "filialNome": "Filial Centro",
      "valorTotal": 270,
      "cancelada": false
    }
  ],
  "totalItems": 1,
  "currentPage": 1,
  "totalPages": 1
}
```

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `500 Internal Server Error`

### `POST /api/sales`

Cria uma venda.

Header opcional:

- `Idempotency-Key: 6fbbd2df-18b8-44b7-a113-5a8cbdc89e4a`

Exemplo de request:

```json
{
  "numero": "VEN-1001",
  "dataVenda": "2026-04-27T13:00:00Z",
  "clienteId": 10,
  "filialId": 1,
  "filialNome": "Filial Centro",
  "itens": [
    {
      "productId": 1,
      "quantidade": 4
    },
    {
      "productId": 2,
      "quantidade": 10
    }
  ]
}
```

Exemplo de response `201 Created`:

```json
{
  "id": "f4c5d58d-a7f5-45cc-a3ef-590ec1b4c87e",
  "numero": "VEN-1001",
  "dataVenda": "2026-04-27T13:00:00Z",
  "clienteId": 10,
  "clienteNome": "Cliente 10",
  "filialId": 1,
  "filialNome": "Filial Centro",
  "valorTotal": 270,
  "cancelada": false,
  "itens": [
    {
      "id": "5dc5335c-a551-4ad6-bf53-73a96c79bd37",
      "productId": 1,
      "productTitle": "Produto 1",
      "quantidade": 4,
      "valorUnitario": 30,
      "percentualDesconto": 0,
      "valorDesconto": 0,
      "valorTotal": 120,
      "cancelado": false
    },
    {
      "id": "4d8aa2ad-e631-4cd7-902f-c0a5c745b42b",
      "productId": 2,
      "productTitle": "Produto 2",
      "quantidade": 10,
      "valorUnitario": 20,
      "percentualDesconto": 0.1,
      "valorDesconto": 20,
      "valorTotal": 180,
      "cancelado": false
    }
  ]
}
```

Status possíveis:

- `201 Created`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `GET /api/sales/{saleId}`

Obtém o detalhe de uma venda.

Status possíveis:

- `200 OK`
- `404 Not Found`
- `500 Internal Server Error`

### `PUT /api/sales/{saleId}`

Atualiza uma venda.

Exemplo de request:

```json
{
  "dataVenda": "2026-04-27T13:00:00Z",
  "clienteId": 10,
  "filialId": 1,
  "filialNome": "Filial Centro",
  "itens": [
    {
      "productId": 1,
      "quantidade": 3
    },
    {
      "productId": 2,
      "quantidade": 8
    }
  ]
}
```

Status possíveis:

- `200 OK`
- `400 Bad Request`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `DELETE /api/sales/{saleId}`

Remove uma venda.

Exemplo de response `200 OK`:

```json
{
  "message": "Sale deleted successfully"
}
```

Status possíveis:

- `200 OK`
- `404 Not Found`
- `409 Conflict`
- `500 Internal Server Error`

### `POST /api/sales/{saleId}/cancel`

Cancela uma venda.

Header opcional:

- `Idempotency-Key: 6fbbd2df-18b8-44b7-a113-5a8cbdc89e4a`

Exemplo de response `200 OK`:

```json
{
  "id": "f4c5d58d-a7f5-45cc-a3ef-590ec1b4c87e",
  "numero": "VEN-1001",
  "dataVenda": "2026-04-27T13:00:00Z",
  "clienteId": 10,
  "clienteNome": "Cliente 10",
  "filialId": 1,
  "filialNome": "Filial Centro",
  "valorTotal": 270,
  "cancelada": true,
  "itens": []
}
```

Status possíveis:

- `200 OK`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

### `POST /api/sales/{saleId}/items/{saleItemId}/cancel`

Cancela um item de uma venda.

Header opcional:

- `Idempotency-Key: 6fbbd2df-18b8-44b7-a113-5a8cbdc89e4a`

Status possíveis:

- `200 OK`
- `404 Not Found`
- `409 Conflict`
- `422 Unprocessable Entity`
- `500 Internal Server Error`

## Observações finais

- As respostas de listagem usam o envelope paginado `{ data, totalItems, currentPage, totalPages }`.
- Os nomes dos campos seguem `camelCase` em JSON, mesmo quando os tipos internos em C# usam `PascalCase`.
- `Products`, `Users` e `Carts` usam CRUD tradicional; `Sales` acrescenta comandos de cancelamento e idempotência.