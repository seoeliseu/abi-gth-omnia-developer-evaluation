using System.Text.Json;
using Ambev.DeveloperEvaluation.Application.Sales.Repositories;
using Ambev.DeveloperEvaluation.Domain.Entities;
using Ambev.DeveloperEvaluation.ORM.Mongo;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.Services;

public sealed class SaleRepositoryEf : ISaleRepository
{
    private readonly DeveloperEvaluationDbContext _context;
    private readonly ISaleAuditStore _saleAuditStore;

    public SaleRepositoryEf(DeveloperEvaluationDbContext context, ISaleAuditStore saleAuditStore)
    {
        _context = context;
        _saleAuditStore = saleAuditStore;
    }

    public async Task AdicionarAsync(Sale sale, CancellationToken cancellationToken)
    {
        var eventos = sale.DomainEvents.ToArray();
        _context.Sales.Add(sale);
        EnfileirarEventos(sale, eventos);
        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaAsync(sale, eventos, cancellationToken);
        sale.LimparEventos();
    }

    public async Task AtualizarAsync(Sale sale, CancellationToken cancellationToken)
    {
        var eventos = sale.DomainEvents.ToArray();
        _context.Sales.Update(sale);
        EnfileirarEventos(sale, eventos);
        await _context.SaveChangesAsync(cancellationToken);
        await RegistrarAuditoriaAsync(sale, eventos, cancellationToken);
        sale.LimparEventos();
    }

    public Task<Sale?> ObterPorIdAsync(Guid saleId, CancellationToken cancellationToken)
    {
        return _context.Sales
            .Include(sale => sale.Items)
            .SingleOrDefaultAsync(sale => sale.Id == saleId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Sale>> ListarAsync(CancellationToken cancellationToken)
    {
        return await _context.Sales
            .Include(sale => sale.Items)
            .OrderByDescending(sale => sale.DataVenda)
            .ToArrayAsync(cancellationToken);
    }

    private void EnfileirarEventos(Sale sale, IReadOnlyCollection<object> eventos)
    {
        foreach (var evento in eventos)
        {
            _context.OutboxMessages.Add(new OutboxMessageEntity
            {
                Id = Guid.NewGuid(),
                AggregateType = nameof(Sale),
                AggregateId = sale.Id.ToString("N"),
                EventType = evento.GetType().Name,
                Payload = JsonSerializer.Serialize(evento),
                OccurredAt = DateTimeOffset.UtcNow
            });
        }
    }

    private async Task RegistrarAuditoriaAsync(Sale sale, IReadOnlyCollection<Domain.Common.IDomainEvent> eventos, CancellationToken cancellationToken)
    {
        try
        {
            await _saleAuditStore.RegistrarAsync(sale, eventos, cancellationToken);
        }
        catch
        {
            // A auditoria em Mongo é complementar nesta fase e não deve bloquear a persistência transacional.
        }
    }
}