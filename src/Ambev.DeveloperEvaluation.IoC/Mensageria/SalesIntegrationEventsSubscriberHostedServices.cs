using Ambev.DeveloperEvaluation.Domain.Events;
using Ambev.DeveloperEvaluation.Common.Resilience;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

namespace Ambev.DeveloperEvaluation.IoC.Mensageria;

public abstract class SalesIntegrationEventsSubscriberHostedServiceBase : IHostedService
{
    private readonly IBus _bus;
    private readonly IIntegrationResilienceExecutor _resilienceExecutor;
    private readonly ILogger _logger;

    protected SalesIntegrationEventsSubscriberHostedServiceBase(IBus bus, IIntegrationResilienceExecutor resilienceExecutor, ILogger logger)
    {
        _bus = bus;
        _resilienceExecutor = resilienceExecutor;
        _logger = logger;
    }

    protected abstract string ServiceName { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _resilienceExecutor.ExecuteAsync(
            IntegrationResiliencePipelineNames.RabbitMqSubscribe,
            async _ =>
            {
                await _bus.Subscribe<SaleCreatedEvent>();
                await _bus.Subscribe<SaleModifiedEvent>();
                await _bus.Subscribe<SaleCancelledEvent>();
                await _bus.Subscribe<ItemCancelledEvent>();
            },
            cancellationToken);

        _logger.LogInformation("{ServiceName} inscrito nos eventos de integração de vendas.", ServiceName);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class ProductsSalesEventsSubscriberHostedService : SalesIntegrationEventsSubscriberHostedServiceBase
{
    public ProductsSalesEventsSubscriberHostedService(
        IBus bus,
        IIntegrationResilienceExecutor resilienceExecutor,
        ILogger<ProductsSalesEventsSubscriberHostedService> logger)
        : base(bus, resilienceExecutor, logger)
    {
    }

    protected override string ServiceName => "Products";
}

public sealed class CartsSalesEventsSubscriberHostedService : SalesIntegrationEventsSubscriberHostedServiceBase
{
    public CartsSalesEventsSubscriberHostedService(
        IBus bus,
        IIntegrationResilienceExecutor resilienceExecutor,
        ILogger<CartsSalesEventsSubscriberHostedService> logger)
        : base(bus, resilienceExecutor, logger)
    {
    }

    protected override string ServiceName => "Carts";
}

public sealed class UsersSalesEventsSubscriberHostedService : SalesIntegrationEventsSubscriberHostedServiceBase
{
    public UsersSalesEventsSubscriberHostedService(
        IBus bus,
        IIntegrationResilienceExecutor resilienceExecutor,
        ILogger<UsersSalesEventsSubscriberHostedService> logger)
        : base(bus, resilienceExecutor, logger)
    {
    }

    protected override string ServiceName => "Users";
}