using System.Diagnostics;
using Ambev.DeveloperEvaluation.Common.Observabilidade;
using Ambev.DeveloperEvaluation.Common.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Ambev.DeveloperEvaluation.ServiceDefaults.Resultados;

public static class ResultHttpExtensions
{
    public static IActionResult ParaActionResult<T>(this ControllerBase controller, Result<T> resultado, Func<T, IActionResult> sucesso)
    {
        if (resultado.IsSuccess && resultado.Value is not null)
        {
            return sucesso(resultado.Value);
        }

        return CriarProblema(controller, resultado);
    }

    private static IActionResult CriarProblema(ControllerBase controller, Result resultado)
    {
        var statusCode = resultado.StatusCode ?? MapearStatusCode(resultado.ErrorType);
        var primeiroErro = resultado.Errors.FirstOrDefault();
        var correlationId = controller.HttpContext.Items[CabecalhosCorrelacao.CorrelationId]?.ToString();
        var traceId = Activity.Current?.TraceId.ToString() ?? controller.HttpContext.TraceIdentifier;

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = primeiroErro?.Codigo ?? resultado.ErrorType.ToString(),
            Detail = primeiroErro?.Mensagem ?? "A requisição não pôde ser processada.",
            Type = $"https://httpstatuses.com/{statusCode}"
        };

        problemDetails.Extensions["errorType"] = resultado.ErrorType.ToString();
        problemDetails.Extensions["errors"] = resultado.Errors.Select(erro => new { erro.Codigo, erro.Mensagem }).ToArray();
        problemDetails.Extensions["correlationId"] = correlationId;
        problemDetails.Extensions["traceId"] = traceId;

        return controller.StatusCode(statusCode, problemDetails);
    }

    private static int MapearStatusCode(ResultErrorType errorType)
    {
        return errorType switch
        {
            ResultErrorType.Validation => StatusCodes.Status400BadRequest,
            ResultErrorType.NotFound => StatusCodes.Status404NotFound,
            ResultErrorType.Conflict => StatusCodes.Status409Conflict,
            ResultErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ResultErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ResultErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };
    }
}