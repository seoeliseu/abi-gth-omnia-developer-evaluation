using System.Diagnostics;
using Ambev.DeveloperEvaluation.Common.Observabilidade;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Ambev.DeveloperEvaluation.ServiceDefaults.Middlewares;

public sealed class MiddlewareCorrelacao
{
    private readonly RequestDelegate _proximo;

    public MiddlewareCorrelacao(RequestDelegate proximo)
    {
        _proximo = proximo;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        var correlationId = ObterOuGerarCorrelationId(contexto);
        var traceId = Activity.Current?.TraceId.ToString() ?? contexto.TraceIdentifier;

        contexto.Items[CabecalhosCorrelacao.CorrelationId] = correlationId;
        contexto.Response.Headers[CabecalhosCorrelacao.CorrelationId] = correlationId;
        contexto.Response.Headers[CabecalhosCorrelacao.TraceId] = traceId;

        using (LogContext.PushProperty("correlationId", correlationId))
        using (LogContext.PushProperty("traceId", traceId))
        {
            await _proximo(contexto);
        }
    }

    private static string ObterOuGerarCorrelationId(HttpContext contexto)
    {
        if (contexto.Request.Headers.TryGetValue(CabecalhosCorrelacao.CorrelationId, out var correlationId) &&
            !string.IsNullOrWhiteSpace(correlationId))
        {
            return correlationId.ToString();
        }

        return Guid.NewGuid().ToString("N");
    }
}