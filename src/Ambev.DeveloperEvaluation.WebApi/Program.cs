using System.Diagnostics;
using Ambev.DeveloperEvaluation.Common.Observabilidade;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.WebApi.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((contexto, _, configuracaoLogger) =>
{
	var urlSeq = contexto.Configuration["Observabilidade:SeqUrl"];

	configuracaoLogger
		.ReadFrom.Configuration(contexto.Configuration)
		.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
		.MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
		.Enrich.FromLogContext()
		.Enrich.WithEnvironmentName()
		.Enrich.WithExceptionDetails()
		.Enrich.WithProperty("serviceName", "Ambev.DeveloperEvaluation.WebApi")
		.WriteTo.Console(new CompactJsonFormatter());

	if (!string.IsNullOrWhiteSpace(urlSeq))
	{
		configuracaoLogger.WriteTo.Seq(urlSeq);
	}
});

builder.Services.AddControllers();
builder.Services.AdicionarServicosTransversais(builder.Configuration);

var app = builder.Build();

app.UseMiddleware<MiddlewareCorrelacao>();

app.UseSerilogRequestLogging(opcoes =>
{
	opcoes.EnrichDiagnosticContext = (contextoDiagnostico, contextoHttp) =>
	{
		var correlationId = contextoHttp.Items[CabecalhosCorrelacao.CorrelationId]?.ToString() ?? string.Empty;
		var traceId = Activity.Current?.TraceId.ToString() ?? contextoHttp.TraceIdentifier;
		var nomeUsuario = contextoHttp.User?.Identity?.IsAuthenticated == true
			? contextoHttp.User.Identity?.Name ?? "autenticado"
			: "anonimo";

		contextoDiagnostico.Set("requestPath", contextoHttp.Request.Path.Value);
		contextoDiagnostico.Set("httpMethod", contextoHttp.Request.Method);
		contextoDiagnostico.Set("statusCode", contextoHttp.Response.StatusCode);
		contextoDiagnostico.Set("correlationId", correlationId);
		contextoDiagnostico.Set("traceId", traceId);
		contextoDiagnostico.Set("userId", nomeUsuario);
		contextoDiagnostico.Set("environment", app.Environment.EnvironmentName);
	};
});

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
	Predicate = registro => registro.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
	Predicate = registro => registro.Tags.Contains("ready")
});

app.Run();
