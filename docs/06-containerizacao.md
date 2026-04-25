# Containerização e Kubernetes

## Objetivo

Garantir que o backend seja executável localmente com dependências externas e tenha caminho claro para deploy em Kubernetes sem retrabalho estrutural.

## Ambiente Local Alvo

O `docker-compose.yml` do repositório principal deverá orquestrar:

- API/backend.
- PostgreSQL.
- MongoDB.
- RabbitMQ.
- Seq.

## Requisitos de Imagem

- `Dockerfile` multi-stage para build e runtime.
- Variáveis de ambiente externas para connection strings, credenciais e broker.
- Health check configurado para containers críticos.

## Preparação para Kubernetes

Manter uma pasta `ops/k8s/` com artefatos base:

- `namespace.yaml`
- `configmap.yaml`
- `secrets.example.yaml`
- `deployment-api.yaml`
- `service-api.yaml`
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