# Microserviços e Bancos de Dados

## Princípio

Cada microserviço deve possuir persistência própria para preservar baixo acoplamento, autonomia de deploy e independência evolutiva.
MongoDB faz parte da versão entregável e não ficará restrito a uma fase futura opcional.
O alvo funcional do desafio são cinco micro-serviços: `Sales`, `Products`, `Carts`, `Users` e `Auth`.

## Mapa Inicial

| Serviço | Responsabilidade principal | Banco | Justificativa |
| --- | --- | --- | --- |
| Sales Service | Vendas, itens, descontos, cancelamentos e eventos | PostgreSQL + MongoDB | PostgreSQL para transação; MongoDB para `event log` e auditoria da venda |
| Products Service | Produtos, categorias e consultas paginadas | PostgreSQL | Catálogo estruturado e filtros relacionais |
| Carts Service | Carrinhos e itens temporários de compra | PostgreSQL | Consistência relacional entre usuário, carrinho e itens |
| Users Service | Usuários, perfis e dados cadastrais | PostgreSQL | Dados transacionais e governança de identidade |
| Auth Service | Login e emissão de token | PostgreSQL | Controle de credenciais e autenticação |

## Diretrizes de Integração

- Identidades externas devem ser trocadas por IDs e dados denormalizados mínimos.
- Leitura entre serviços não deve depender de joins distribuídos.
- Informações necessárias para leitura rápida devem ser replicadas via eventos.

## Estratégia de Comunicação Inicial

| Origem | Destino | Tipo | Objetivo |
| --- | --- | --- | --- |
| Sales Service | Products Service | Síncrona | Validar produto, preço de referência e disponibilidade lógica de catálogo |
| Sales Service | Users Service | Síncrona | Validar cliente e dados mínimos da identidade externa |
| Carts Service | Products Service | Síncrona | Validar produtos do carrinho |
| Carts Service | Users Service | Síncrona | Validar dono do carrinho |
| Sales Service | Audit store em MongoDB | Assíncrona interna | Persistir `event log` e auditoria da venda |
| Sales Service | Demais serviços | Assíncrona | Publicar eventos de integração relevantes via RabbitMQ |

Na Fase 1, como a entrega ainda é um backend modular único, a comunicação síncrona pode ser representada por contratos internos de aplicação entre módulos. A extração para HTTP entre processos fica para as fases seguintes.

## Estratégia de Evolução

### Fase 1

- Entrega em backend modular único.
- Bancos já separados por responsabilidade lógica em ambiente local.
- PostgreSQL e MongoDB ativos na entrega inicial.
- Naming dos projetos seguindo o padrão `Ambev.DeveloperEvaluation.*`.
- MongoDB assumindo desde o início o papel de `event log` e auditoria da venda no `Sales Service`.
- Os cinco serviços da spec já devem estar refletidos em módulos, contratos e backlog, mesmo antes da extração física por deploy.
- A exposição HTTP da solução acontece diretamente nos hosts `Sales`, `Products`, `Carts`, `Users` e `Auth`; não há host genérico central na estrutura final.

### Fase 2

- Extração do `Sales Service` como primeiro microserviço isolado.
- Introdução de contratos de integração formais via RabbitMQ.

### Fase 3

- Extração de `Products Service`, `Carts Service`, `Users Service` e `Auth Service` conforme prioridade de negócio.
- Expansão de projeções e observabilidade distribuída.