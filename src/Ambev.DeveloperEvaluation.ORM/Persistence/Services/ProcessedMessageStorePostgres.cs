using Ambev.DeveloperEvaluation.Application.Common.Mensageria;
using Ambev.DeveloperEvaluation.ORM.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ambev.DeveloperEvaluation.ORM.Persistence.Services;

public sealed class ProcessedMessageStorePostgres : IProcessedMessageStore
{
    private readonly DeveloperEvaluationDbContext _context;

    public ProcessedMessageStorePostgres(DeveloperEvaluationDbContext context)
    {
        _context = context;
    }

    public Task<bool> JaProcessadaAsync(string consumidor, string messageId, CancellationToken cancellationToken)
    {
        return _context.ProcessedMessages.AnyAsync(item => item.Consumer == consumidor && item.MessageId == messageId, cancellationToken);
    }

    public async Task RegistrarAsync(string consumidor, string messageId, CancellationToken cancellationToken)
    {
        if (await JaProcessadaAsync(consumidor, messageId, cancellationToken))
        {
            return;
        }

        _context.ProcessedMessages.Add(new ProcessedMessageEntity
        {
            Id = Guid.NewGuid(),
            Consumer = consumidor,
            MessageId = messageId,
            ProcessedAt = DateTimeOffset.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);
    }
}