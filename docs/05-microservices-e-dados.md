# Microserviços e Bancos de Dados

## Princípio

Cada microserviço deve possuir persistência própria para preservar baixo acoplamento, autonomia de deploy e independência evolutiva.
MongoDB faz parte da versão entregável e não ficará restrito a uma fase futura opcional.

## Mapa Inicial

| Serviço | Responsabilidade principal | Banco | Justificativa |
| --- | --- | --- | --- |
| Identity Service | Usuários, autenticação e perfis | PostgreSQL | Dados transacionais, consistência forte e relacionamento claro |
| Sales Service | Vendas, itens, descontos, cancelamentos | PostgreSQL | Núcleo transacional do desafio |
| Catalog Service | Produtos, clientes e filiais de referência | PostgreSQL | Cadastros estruturados e integrações futuras |
| Audit/Projection Service | Event log e auditoria da venda | MongoDB | Flexibilidade documental para trilha imutável de eventos e auditoria |

## Diretrizes de Integração

- Identidades externas devem ser trocadas por IDs e dados denormalizados mínimos.
- Leitura entre serviços não deve depender de joins distribuídos.
- Informações necessárias para leitura rápida devem ser replicadas via eventos.

## Estratégia de Evolução

### Fase 1

- Entrega em backend modular único.
- Bancos já separados por responsabilidade lógica em ambiente local.
- PostgreSQL e MongoDB ativos na entrega inicial.
- Naming dos projetos seguindo o padrão `Ambev.DeveloperEvaluation.*`.
- MongoDB assumindo desde o início o papel de `event log` e auditoria da venda.

### Fase 2

- Extração do `Sales Service` como primeiro microserviço isolado.
- Introdução de contratos de integração formais via RabbitMQ.

### Fase 3

- Extração de `Catalog Service` e `Identity Service` conforme necessidade.
- Expansão de projeções e observabilidade distribuída.