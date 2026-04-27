namespace Ambev.DeveloperEvaluation.Common.Mensageria;

public interface IProcessedMessageStore
{
    Task<bool> JaProcessadaAsync(string consumidor, string messageId, CancellationToken cancellationToken);
    Task RegistrarAsync(string consumidor, string messageId, CancellationToken cancellationToken);
}