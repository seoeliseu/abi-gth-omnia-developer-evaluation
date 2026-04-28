using System.Diagnostics.Metrics;
using Rebus.Messages;
using Rebus.Retry;
using Rebus.Transport;

namespace Ambev.DeveloperEvaluation.IoC.Mensageria;

public sealed class RebusDlqErrorHandlerDecorator : IErrorHandler
{
    private static readonly Meter Meter = new("Ambev.DeveloperEvaluation.Messaging", "1.0.0");
    private static readonly Counter<long> DlqCounter = Meter.CreateCounter<long>("developer_evaluation_rabbitmq_dlq_total");

    private readonly IErrorHandler _inner;
    private readonly string _queueName;
    private readonly string _errorQueueName;

    public RebusDlqErrorHandlerDecorator(
        IErrorHandler inner,
        string queueName,
        string errorQueueName)
    {
        _inner = inner;
        _queueName = queueName;
        _errorQueueName = errorQueueName;
    }

    public async Task HandlePoisonMessage(TransportMessage transportMessage, ITransactionContext transactionContext, ExceptionInfo exception)
    {
        var messageType = ObterHeader(transportMessage, Headers.Type, "desconhecido");
        var messageId = ObterHeader(transportMessage, Headers.MessageId, "sem-message-id");
        var sourceQueue = ObterHeader(transportMessage, Headers.SourceQueue, _queueName);
        var deliveryCount = ObterHeader(transportMessage, Headers.DeliveryCount, "desconhecido");
        var correlationId = ObterHeader(transportMessage, Headers.CorrelationId, "sem-correlation-id");

        try
        {
            await _inner.HandlePoisonMessage(transportMessage, transactionContext, exception);
        }
        catch (Exception ex)
        {
            RebusDlqDiagnostics.Record(
                $"Falha ao encaminhar mensagem para DLQ {_errorQueueName} a partir da fila {sourceQueue}. MessageId={messageId}; MessageType={messageType}; Exception={ex}");

            Console.Error.WriteLine(
                "Falha ao encaminhar mensagem para DLQ {0} a partir da fila {1}. MessageId={2}; MessageType={3}; Exception={4}",
                _errorQueueName,
                sourceQueue,
                messageId,
                messageType,
                ex);

            throw;
        }

        DlqCounter.Add(
            1,
            KeyValuePair.Create<string, object?>("queue", sourceQueue),
            KeyValuePair.Create<string, object?>("error_queue", _errorQueueName),
            KeyValuePair.Create<string, object?>("message_type", messageType),
            KeyValuePair.Create<string, object?>("failure_type", exception.Type));

        RebusDlqDiagnostics.Record(
            $"Mensagem desviada para DLQ {_errorQueueName} a partir da fila {sourceQueue}. MessageId={messageId}; MessageType={messageType}; DeliveryCount={deliveryCount}; CorrelationId={correlationId}; FailureType={exception.Type}; FailureMessage={exception.Message}");

        Console.Error.WriteLine(
            "Mensagem desviada para DLQ {0} a partir da fila {1}. MessageId={2}; MessageType={3}; DeliveryCount={4}; CorrelationId={5}; FailureType={6}; FailureMessage={7}",
            _errorQueueName,
            sourceQueue,
            messageId,
            messageType,
            deliveryCount,
            correlationId,
            exception.Type,
            exception.Message);
    }

    private static string ObterHeader(TransportMessage transportMessage, string key, string fallback)
    {
        return transportMessage.Headers.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : fallback;
    }
}

public static class RebusDlqDiagnostics
{
    private static readonly Queue<string> Entries = new();
    private static readonly object Sync = new();

    public static void Clear()
    {
        lock (Sync)
        {
            Entries.Clear();
        }
    }

    public static void Record(string message)
    {
        lock (Sync)
        {
            Entries.Enqueue(message);

            while (Entries.Count > 50)
            {
                Entries.Dequeue();
            }
        }
    }

    public static bool Contains(string messageFragment)
    {
        lock (Sync)
        {
            return Entries.Any(entry => entry.Contains(messageFragment, StringComparison.Ordinal));
        }
    }
}