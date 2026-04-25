# Roadmap de Execução

## Sequência de Trabalho

### Dia 1

- Revisão integral do enunciado e do template.
- Definição da arquitetura, requisitos e backlog.
- Criação da solução base no repositório principal.
- Configuração inicial de Docker Compose com API, PostgreSQL, MongoDB e RabbitMQ.
- Abertura da primeira branch de fase para banco de dados e ORM.

### Dia 2

- Implementação do domínio de vendas.
- Implementação do `Result<T>` compartilhado.
- Casos de uso de criação, consulta e cancelamento.
- Persistência com PostgreSQL e migrações.
- Estruturação do uso entregável de MongoDB.
- Fechamento da branch de banco + ORM e integração em `master` após validação.

### Dia 3

- Completar CRUD, filtros, paginação e ordenação.
- Adicionar publicação de eventos.
- Implementar testes unitários do domínio e aplicação.
- Refinar logs, validações e tratamento de erros.
- Trabalhar em branch de camada de serviços e branch de regras de negócio, com integração em `master` quando finalizadas.

### Dia 4

- Ajustes finais de documentação.
- Coleção de requests para testes manuais.
- Revisão da preparação para Kubernetes.
- Revisão de qualidade e entrega.
- Fechar branch de testes e branch final de estabilização antes da entrega.

## Distribuição de Tempo Recomendada

- 15% planejamento e documentação inicial.
- 45% implementação do domínio e aplicação.
- 20% integração, persistência e mensageria.
- 15% testes.
- 5% revisão final e empacotamento.

## Critérios de Priorização

1. Regra de negócio correta.
2. Contratos e estrutura limpos.
3. Executável localmente com dependências externas.
4. Testes cobrindo regras críticas.
5. Eventos e preparação operacional.