namespace Ambev.DeveloperEvaluation.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OcorreuEm { get; }
}