using System.Text.Json;
using Ambev.DeveloperEvaluation.Domain.Common;
using Ambev.DeveloperEvaluation.Domain.Entities;
using MongoDB.Driver;

namespace Ambev.DeveloperEvaluation.ORM.Mongo;

public sealed class SaleAuditStoreMongo : ISaleAuditStore
{
    private readonly IMongoCollection<SaleAuditDocument> _collection;

    public SaleAuditStoreMongo(IMongoDatabase database)
    {
        _collection = database.GetCollection<SaleAuditDocument>("sale_audit");
    }

    public async Task RegistrarAsync(Sale sale, IReadOnlyCollection<IDomainEvent> eventos, CancellationToken cancellationToken)
    {
        if (eventos.Count == 0)
        {
            return;
        }

        var documentos = eventos.Select(evento => new SaleAuditDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            SaleId = sale.Id,
            NumeroVenda = sale.Numero,
            EventType = evento.GetType().Name,
            Payload = JsonSerializer.Serialize(new
            {
                sale.Id,
                sale.Numero,
                sale.DataVenda,
                sale.ClienteId,
                sale.ClienteNome,
                sale.FilialId,
                sale.FilialNome,
                sale.Cancelada,
                ValorTotal = sale.ValorTotal,
                Itens = sale.Items.Select(item => new
                {
                    item.Id,
                    item.ProductId,
                    item.ProductTitle,
                    item.Quantidade,
                    item.ValorUnitario,
                    item.PercentualDesconto,
                    item.Cancelado
                })
            }),
            OcorreuEm = evento.OcorreuEm
        }).ToArray();

        await _collection.InsertManyAsync(documentos, cancellationToken: cancellationToken);
    }
}