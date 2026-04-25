using Ambev.DeveloperEvaluation.Domain.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rebus.Bus;

namespace Ambev.DeveloperEvaluation.IoC.Mensageria;

public abstract class SalesIntegrationEventsSubscriberHostedServiceBase : IHostedService
{
    private readonly IBus _bus;
    private readonly ILogger _logger;

    protected SalesIntegrationEventsSubscriberHostedServiceBase(IBus bus, ILogger logger)
    {
        _bus = bus;
        _logger = logger;
    }

    protected abstract string ServiceName { get; }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _bus.Subscribe<SaleCreatedEvent>();
        await _bus.Subscribe<SaleModifiedEvent>();
        await _bus.Subscribe<SaleCancelledEvent>();
        await _bus.Subscribe<ItemCancelledEvent>();

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
        ILogger<ProductsSalesEventsSubscriberHostedService> logger)
        : base(bus, logger)
    {
    }

    protected override string ServiceName => "Products";
}

public sealed class CartsSalesEventsSubscriberHostedService : SalesIntegrationEventsSubscriberHostedServiceBase
{
    public CartsSalesEventsSubscriberHostedService(
        IBus bus,
        ILogger<CartsSalesEventsSubscriberHostedService> logger)
        : base(bus, logger)
    {
    }

    protected override string ServiceName => "Carts";
}

public sealed class UsersSalesEventsSubscriberHostedService : SalesIntegrationEventsSubscriberHostedServiceBase
{
    public UsersSalesEventsSubscriberHostedService(
        IBus bus,
        ILogger<UsersSalesEventsSubscriberHostedService> logger)
        : base(bus, logger)
    {
    }

    protected override string ServiceName => "Users";
}