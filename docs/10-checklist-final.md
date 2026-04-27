# Checklist Final de Entrega

## Spec original

- [x] API de vendas com CRUD completo.
- [x] Exposição de número da venda, data, cliente, valor total, filial, produtos, quantidades, preços unitários, descontos, total por item e status cancelado.
- [x] Regras de desconto por quantidade implementadas.
- [x] Limite máximo de 20 itens idênticos por produto.
- [x] Eventos `SaleCreated`, `SaleModified`, `SaleCancelled` e `ItemCancelled` implementados.
- [x] Repositório com instruções de configuração, execução e testes.
- [ ] Repositório público no GitHub.
  Ação manual: publicar o repositório e anexar o link de avaliação.

## Requisitos arquiteturais adicionados

- [x] `Result<T>` como padrão da camada de aplicação.
- [x] Organização por `BuildingBlocks` e `Modules/<Context>`.
- [x] Separação física dos bounded contexts `Auth`, `Carts`, `Products`, `Sales` e `Users`.
- [x] RabbitMQ como transporte assíncrono.
- [x] MongoDB entregue na solução.
- [x] Docker Compose pronto para subir o ambiente local.
- [x] Manifests base de Kubernetes em `ops/k8s/`.
- [x] Endpoints `/health/live` e `/health/ready`.
- [x] `ProblemDetails` e contrato de erro consistente.
- [x] `correlationId` e `traceId` propagados.
- [x] Rate limit e timeout aplicados na borda HTTP.
- [x] Polly centralizado na composição.

## Persistência e operação

- [x] PostgreSQL segregado por serviço no ambiente de entrega via Docker Compose.
- [x] Configuração Kubernetes preparada com segredo de PostgreSQL por serviço.
- [x] MongoDB configurado com database lógico por serviço no ambiente operacional.
- [x] Script de bootstrap do PostgreSQL cria as databases esperadas no Compose.
- [x] Overlay de `production` com HPA por serviço.
- [x] Overlay de `production` com imagens travadas por digest.

## Validação executada

- [x] `dotnet build .\Ambev.DeveloperEvaluation.slnx`
- [x] `dotnet test .\Ambev.DeveloperEvaluation.slnx --no-build`
- [x] `docker compose config`
- [x] `kubectl kustomize .\ops\k8s\overlays\staging`
- [x] `kubectl kustomize .\ops\k8s\overlays\production`

## Observações finais

- O único item dependente de ação externa ao workspace é a publicação do repositório em GitHub público.
- Para uma nova avaliação final, usar este checklist junto do README operacional.