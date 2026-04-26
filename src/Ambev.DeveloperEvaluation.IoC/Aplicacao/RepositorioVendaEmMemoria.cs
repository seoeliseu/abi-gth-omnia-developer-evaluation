using System.Collections.Concurrent;
using Ambev.DeveloperEvaluation.Sales.Application.Repositories;
using Ambev.DeveloperEvaluation.Sales.Domain.Entities;

namespace Ambev.DeveloperEvaluation.IoC.Aplicacao;

public sealed class RepositorioVendaEmMemoria : ISaleRepository
{
    private readonly ConcurrentDictionary<Guid, Sale> _sales = new();

    public Task AdicionarAsync(Sale sale, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sales[sale.Id] = sale;
        return Task.CompletedTask;
    }

    public Task AtualizarAsync(Sale sale, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sales[sale.Id] = sale;
        return Task.CompletedTask;
    }

    public Task<Sale?> ObterPorIdAsync(Guid saleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _sales.TryGetValue(saleId, out var sale);
        return Task.FromResult(sale);
    }

    public Task<IReadOnlyCollection<Sale>> ListarAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult((IReadOnlyCollection<Sale>)_sales.Values.ToArray());
    }
}