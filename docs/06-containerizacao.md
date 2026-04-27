# Containerização e Kubernetes

## Objetivo

Garantir que o backend seja executável localmente com dependências externas e tenha caminho claro para deploy em Kubernetes sem retrabalho estrutural.

## Ambiente Local Alvo

O `docker-compose.yml` do repositório principal deverá orquestrar:

- `Sales API`.
- `Products API`.
- `Carts API`.
- `Users API`.
- `Auth API`.
- PostgreSQL.
- MongoDB.
- RabbitMQ.
- Seq.

## Requisitos de Imagem

- `Dockerfile` multi-stage reutilizável para build e runtime dos hosts `*.WebApi`.
- Variáveis de ambiente externas para connection strings, credenciais e broker.
- Health check configurado para containers críticos.

## Preparação para Kubernetes

Manter uma pasta `ops/k8s/` com artefatos base:

- `namespace.yaml`
- `configmap.yaml` com configuração por serviço.
- `secrets.staging.example.yaml`
- `secrets.production.example.yaml`
- `deployment-api.yaml` com deployments dos cinco serviços.
- `service-api.yaml` com services dos cinco serviços.
- `ingress.yaml` opcional
- `job-migrations.yaml` opcional

## Convenções Operacionais

- Configuração via variáveis de ambiente.
- Readiness e liveness probes.
- `liveness` validando processo vivo sem depender de infraestrutura externa.
- `readiness` validando capacidade de atender tráfego e dependências críticas configuradas.
- Logs estruturados emitidos em `stdout`.
- Serilog como provider de logging da aplicação.
- Console JSON como sink primário.
- Seq como destino de consulta e retenção operacional no ambiente local de entrega.
- Containers stateless.
- Persistência delegada a serviços externos gerenciados ou volumes dedicados.

## Organização de Ambientes

- O `docker-compose.yml` executa as APIs com `ASPNETCORE_ENVIRONMENT=Staging`.
- O arquivo `.env.example` define os nomes padronizados de variáveis para Compose.
- O diretório `ops/k8s/` permanece como base dos manifests.
- A base reutilizável de Kustomize fica em `ops/k8s/base/`.
- Os overlays `ops/k8s/overlays/staging/` e `ops/k8s/overlays/production/` explicitam o ambiente alvo via Kustomize.
- Os overlays também diferenciam `replicas`, `resources`, HPA e estratégia de imagem entre `staging` e `production`.
- Os arquivos `secrets.staging.example.yaml` e `secrets.production.example.yaml` mantêm apenas placeholders padronizados, para preenchimento pelo mecanismo de segredo do ambiente.