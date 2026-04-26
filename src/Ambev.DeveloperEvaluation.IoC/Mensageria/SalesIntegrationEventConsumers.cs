using Ambev.DeveloperEvaluation.Common.Mensageria;
using Ambev.DeveloperEvaluation.Sales.Domain.Events;
using Microsoft.Extensions.Logging;
using Rebus.Handlers;
using Rebus.Messages;
using Rebus.Pipeline;

namespace Ambev.DeveloperEvaluation.IoC.Mensageria;

public abstract class SalesIntegrationEventConsumerBase :
    IHandleMessages<SaleCreatedEvent>,
    IHandleMessages<SaleModifiedEvent>,
    IHandleMessages<SaleCancelledEvent>,
    IHandleMessages<ItemCancelledEvent>
{
    private readonly IProcessedMessageStore _processedMessageStore;
    private readonly ILogger _logger;

    protected SalesIntegrationEventConsumerBase(IProcessedMessageStore processedMessageStore, ILogger logger)
    {
        _processedMessageStore = processedMessageStore;
        _logger = logger;
    }

    protected abstract string ConsumerName { get; }
    protected abstract string ServiceName { get; }

    public Task Handle(SaleCreatedEvent message)
    {
        return ProcessarAsync(nameof(SaleCreatedEvent), message.SaleId, $"numero={message.NumeroVenda}");
    }

    public Task Handle(SaleModifiedEvent message)
    {
        return ProcessarAsync(nameof(SaleModifiedEvent), message.SaleId, $"numero={message.NumeroVenda}");
    }

    public Task Handle(SaleCancelledEvent message)
    {
        return ProcessarAsync(nameof(SaleCancelledEvent), message.SaleId, $"numero={message.NumeroVenda}");
    }

    public Task Handle(ItemCancelledEvent message)
    {
        return ProcessarAsync(nameof(ItemCancelledEvent), message.SaleId, $"item={message.SaleItemId};produto={message.ProductId}");
    }

    private async Task ProcessarAsync(string eventType, Guid saleId, string details)
    {
        var messageId = ObterMessageId();
        if (string.IsNullOrWhiteSpace(messageId))
        {
            _logger.LogWarning(
                "{ServiceName} recebeu {EventType} da venda {SaleId} sem identificador de mensagem. Detalhes: {Details}",
                ServiceName,
                eventType,
                saleId,
                details);

            return;
        }

        if (await _processedMessageStore.JaProcessadaAsync(ConsumerName, messageId, CancellationToken.None))
        {
            _logger.LogInformation(
                "{ServiceName} ignorou mensagem duplicada {MessageId} de {EventType} para a venda {SaleId}.",
                ServiceName,
                messageId,
                eventType,
                saleId);

            return;
        }

        _logger.LogInformation(
            "{ServiceName} processou {EventType} para a venda {SaleId}. Detalhes: {Details}",
            ServiceName,
            eventType,
            saleId,
            details);

        await _processedMessageStore.RegistrarAsync(ConsumerName, messageId, CancellationToken.None);
    }

    private static string? ObterMessageId()
    {
        var headers = MessageContext.Current?.Headers;
        if (headers is null)
        {
            return null;
        }

        if (headers.TryGetValue("outbox-id", out var outboxMessageId) && !string.IsNullOrWhiteSpace(outboxMessageId))
        {
            return outboxMessageId;
        }

        return headers.TryGetValue(Headers.MessageId, out var rebusMessageId) ? rebusMessageId : null;
    }
}

public sealed class ProductsSalesIntegrationEventsConsumer : SalesIntegrationEventConsumerBase
{
    public ProductsSalesIntegrationEventsConsumer(
        IProcessedMessageStore processedMessageStore,
        ILogger<ProductsSalesIntegrationEventsConsumer> logger)
        : base(processedMessageStore, logger)
    {
    }

    protected override string ConsumerName => "products.sales-events";
    protected override string ServiceName => "Products";
}

public sealed class CartsSalesIntegrationEventsConsumer : SalesIntegrationEventConsumerBase
{
    public CartsSalesIntegrationEventsConsumer(
        IProcessedMessageStore processedMessageStore,
        ILogger<CartsSalesIntegrationEventsConsumer> logger)
        : base(processedMessageStore, logger)
    {
    }

    protected override string ConsumerName => "carts.sales-events";
    protected override string ServiceName => "Carts";
}

public sealed class UsersSalesIntegrationEventsConsumer : SalesIntegrationEventConsumerBase
{
    public UsersSalesIntegrationEventsConsumer(
        IProcessedMessageStore processedMessageStore,
        ILogger<UsersSalesIntegrationEventsConsumer> logger)
        : base(processedMessageStore, logger)
    {
    }

    protected override string ConsumerName => "users.sales-events";
    protected override string ServiceName => "Users";
}