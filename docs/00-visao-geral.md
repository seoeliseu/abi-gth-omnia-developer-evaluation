# Visão Geral do Desafio

## Objetivo

Construir o backend do desafio técnico a partir do template fornecido em `template/backend`, priorizando organização, clareza arquitetural, regras de negócio, testes automatizados e prontidão para execução em ambiente conteinerizado.

## Escopo Inicial

- Implementação somente do backend.
- Estrutura preparada para evolução em microserviços.
- Comunicação assíncrona padronizada com RabbitMQ.
- Retorno de casos de uso padronizado com `Result<T>`.
- Preparação para Docker e Kubernetes desde o início.

## Stack e Frameworks Considerados

O planejamento está considerando explicitamente o material de `.doc` do template, com as seguintes decisões para o backend deste desafio:

- Linguagem e runtime: C# com .NET 8.
- Persistência: PostgreSQL e MongoDB, conforme responsabilidade de cada contexto.
- ORM: EF Core para acesso relacional e mapeamento objeto-relacional.
- Padrões de aplicação: Mediator/MediatR para casos de uso e AutoMapper para mapeamento entre contratos.
- Mensageria: Rebus sobre RabbitMQ.
- Testes: xUnit, NSubstitute e Faker/Bogus.
- API: REST com suporte a paginação, filtros, ordenação e tratamento consistente de erros.
- Observabilidade: Serilog com saída estruturada em Console JSON e retenção/pesquisa via Seq em container dedicado.

## Itens do Template Fora de Escopo

- Angular aparece no `tech-stack.md` do template, mas não será utilizado porque a entrega definida para este repositório é somente backend.

## Entregáveis Obrigatórios

- Backend versionado neste repositório.
- Instruções claras de execução no README.
- Organização por camadas e/ou serviços.
- Regras de negócio documentadas e implementadas.
- Testes automatizados, pelo menos na camada de domínio e aplicação.

## Diferenciais Planejados

- Publicação de eventos de domínio e integração.
- Observabilidade mínima com logs estruturados e health checks.
- Ambientes locais via Docker Compose.
- Manifestos base para Kubernetes.

## Premissas Arquiteturais

- O domínio principal do desafio é vendas.
- Serviços auxiliares serão definidos como bounded contexts para permitir evolução incremental.
- Consistência transacional permanecerá local a cada serviço; integrações cruzadas usarão eventos.