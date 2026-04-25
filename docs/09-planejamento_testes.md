# Planejamento de Testes com Testcontainers

## Objetivo

Definir a estratégia de testes automatizados da solução usando Testcontainers para validar integração real com banco de dados, API e RabbitMQ sem depender de infraestrutura previamente instalada na máquina.

## Decisão

Testcontainers será usado nos testes de integração e funcionais da aplicação.

Essa escolha é adequada para este projeto porque:

- a solução depende de PostgreSQL, MongoDB e RabbitMQ;
- o comportamento relevante envolve integração entre aplicação, persistência e mensageria;
- os testes precisam ser reproduzíveis em qualquer ambiente de desenvolvimento e CI;
- o template já separa `Unit`, `Integration` e `Functional`, o que encaixa bem com essa estratégia.

## Onde usar e onde não usar

### Testes unitários

Não devem usar Testcontainers.

Devem validar:

- regras de desconto;
- limites de quantidade;
- comportamento de entidades e value objects;
- handlers isolados com doubles quando a regra não exigir infraestrutura real.

### Testes de integração

Devem usar Testcontainers quando a verificação depender de infraestrutura real.

Escopos prioritários:

- PostgreSQL com EF Core e migrações;
- MongoDB para `event log` e auditoria da venda;
- RabbitMQ para publicação e consumo de eventos;
- fluxos de idempotência técnica e persistência;
- bootstrap da aplicação com dependências reais.

### Testes funcionais

Devem usar Testcontainers para subir as dependências da API e executar cenários ponta a ponta via HTTP.

Escopos prioritários:

- criação de venda;
- consulta com filtros, paginação e ordenação;
- cancelamento de venda e item;
- retorno de `ProblemDetails`;
- propagação de `correlationId` e `traceId`;
- health endpoints.

## Estratégia por projeto de testes

### `Ambev.DeveloperEvaluation.Unit`

- Sem container.
- Execução rápida.
- Deve rodar em qualquer alteração de domínio e aplicação.

### `Ambev.DeveloperEvaluation.Integration`

- Uso de Testcontainers para PostgreSQL, MongoDB e RabbitMQ conforme cenário.
- Pode compartilhar fixtures por coleção de testes para reduzir tempo total.
- Deve validar repositórios, persistência, consumidores, publishers e contratos de infraestrutura.

### `Ambev.DeveloperEvaluation.Functional`

- Uso de `WebApplicationFactory` com dependências reais providas por Testcontainers.
- Deve validar a API inteira, incluindo middlewares, autenticação quando aplicável e resposta HTTP final.

## Containers previstos para os testes

- PostgreSQL
- MongoDB
- RabbitMQ

O Seq não é prioridade para testes automatizados, porque é parte de observabilidade operacional e não do comportamento funcional central dos cenários de teste.

PostgreSQL também deverá cobrir nos testes o armazenamento de chaves de idempotência HTTP, enquanto RabbitMQ deverá validar deduplicação persistida no consumo.

## Padrão de uso recomendado

### PostgreSQL

- subir container por fixture compartilhada;
- aplicar migrações antes dos testes;
- limpar dados entre cenários com estratégia previsível.

### MongoDB

- subir container por fixture compartilhada;
- criar base isolada por coleção ou por classe quando necessário;
- limpar coleções entre testes para evitar acoplamento.

### RabbitMQ

- subir container por fixture compartilhada;
- declarar filas e exchanges do cenário;
- validar publicação, consumo e correlação de mensagens.

## Ordem de implementação recomendada

1. Criar base de testes unitários sem container.
2. Criar fixture Testcontainers para PostgreSQL.
3. Adicionar testes de integração do ORM e migrações.
4. Criar fixture Testcontainers para RabbitMQ.
5. Adicionar testes de publicação e consumo.
6. Criar fixture Testcontainers para MongoDB.
7. Adicionar testes de auditoria/event log/projeções.
8. Montar testes funcionais da API com `WebApplicationFactory`.

## Boas práticas

- Containers devem ser compartilhados por coleção de teste quando possível.
- Cada teste deve ser independente do resultado de outro.
- Não usar sleeps fixos; preferir readiness/checks do container.
- Evitar cenário de integração cobrindo regra que já está totalmente garantida em teste unitário.
- Testes com RabbitMQ devem validar idempotência e correlação, não apenas envio bruto de mensagem.

## Conclusão

Sim, faz sentido usar Testcontainers do jeito que você descreveu, mas com recorte correto:

- usar para integração real com PostgreSQL, MongoDB, RabbitMQ e API;
- não usar para testes unitários;
- não transformar todo teste em teste pesado de infraestrutura.

Essa abordagem preserva velocidade no núcleo unitário e realismo onde a integração realmente importa.