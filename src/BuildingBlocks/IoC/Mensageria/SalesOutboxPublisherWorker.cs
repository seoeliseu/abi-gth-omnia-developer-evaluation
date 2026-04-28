using System.Text.Json;
using Ambev.DeveloperEvaluation.Common.Resilience;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Ambev.DeveloperEvaluation.Sales.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

namespace Ambev.DeveloperEvaluation.IoC.Mensageria;

public sealed class SalesOutboxPublisherWorker : BackgroundService
{
    private static readonly TimeSpan IntervaloProcessamento = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly IIntegrationResilienceExecutor _resilienceExecutor;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SalesOutboxPublisherWorker> _logger;

    public SalesOutboxPublisherWorker(
        IIntegrationResilienceExecutor resilienceExecutor,
        IServiceScopeFactory scopeFactory,
        ILogger<SalesOutboxPublisherWorker> logger)
    {
        _resilienceExecutor = resilienceExecutor;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(IntervaloProcessamento);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublicarPendentesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao publicar eventos pendentes da outbox de vendas.");
            }

            await timer.WaitForNextTickAsync(stoppingToken);
        }
    }

    private async Task PublicarPendentesAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<DeveloperEvaluationDbContext>();
        var bus = scope.ServiceProvider.GetRequiredService<IBus>();

        var mensagensPendentes = await context.OutboxMessages
            .Where(mensagem => mensagem.PublishedAt == null)
            .OrderBy(mensagem => mensagem.OccurredAt)
            .Take(25)
            .ToListAsync(cancellationToken);

        if (mensagensPendentes.Count == 0)
        {
            return;
        }

        foreach (var mensagem in mensagensPendentes)
        {
            var evento = ResolverEvento(mensagem);
            if (evento is null)
            {
                _logger.LogWarning(
                    "Mensagem da outbox {OutboxId} ignorada por tipo de evento desconhecido: {EventType}.",
                    mensagem.Id,
                    mensagem.EventType);

                mensagem.PublishedAt = DateTimeOffset.UtcNow;
                continue;
            }

            await _resilienceExecutor.ExecuteAsync(
                IntegrationResiliencePipelineNames.RabbitMqPublish,
                _ => new ValueTask(bus.Publish(evento, new Dictionary<string, string>
                {
                    ["outbox-id"] = mensagem.Id.ToString("N"),
                    ["aggregate-type"] = mensagem.AggregateType,
                    ["aggregate-id"] = mensagem.AggregateId,
                    ["event-type"] = mensagem.EventType
                })),
                cancellationToken);

            mensagem.PublishedAt = DateTimeOffset.UtcNow;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static object? ResolverEvento(OutboxMessageEntity mensagem)
    {
        return mensagem.EventType switch
        {
            nameof(SaleCreatedEvent) => JsonSerializer.Deserialize<SaleCreatedEvent>(mensagem.Payload, SerializerOptions),
            nameof(SaleModifiedEvent) => JsonSerializer.Deserialize<SaleModifiedEvent>(mensagem.Payload, SerializerOptions),
            nameof(SaleCancelledEvent) => JsonSerializer.Deserialize<SaleCancelledEvent>(mensagem.Payload, SerializerOptions),
            nameof(ItemCancelledEvent) => JsonSerializer.Deserialize<ItemCancelledEvent>(mensagem.Payload, SerializerOptions),
            _ => null
        };
    }
}