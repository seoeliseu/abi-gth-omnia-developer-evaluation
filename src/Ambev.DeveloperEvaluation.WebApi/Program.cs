using System.Diagnostics;
using System.Threading.RateLimiting;
using Ambev.DeveloperEvaluation.Common.Observabilidade;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.WebApi.Middlewares;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
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
builder.Services.AddProblemDetails();
builder.Services.AddRateLimiter(opcoes =>
{
	opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	opcoes.AddPolicy("sales-consultas", contexto =>
		RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "consultas",
			factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 60,
				Window = TimeSpan.FromMinutes(1),
				QueueLimit = 0,
				AutoReplenishment = true
			}));
	opcoes.AddPolicy("sales-comandos", contexto =>
		RateLimitPartition.GetFixedWindowLimiter(
			partitionKey: contexto.Connection.RemoteIpAddress?.ToString() ?? "comandos",
			factory: _ => new FixedWindowRateLimiterOptions
			{
				PermitLimit = 20,
				Window = TimeSpan.FromMinutes(1),
				QueueLimit = 0,
				AutoReplenishment = true
			}));
});
builder.Services.AddRequestTimeouts(opcoes =>
{
	opcoes.DefaultPolicy = new RequestTimeoutPolicy
	{
		Timeout = TimeSpan.FromSeconds(30)
	};

	opcoes.AddPolicy("sales-consultas", TimeSpan.FromSeconds(15));
	opcoes.AddPolicy("sales-comandos", TimeSpan.FromSeconds(30));
});
builder.Services.AdicionarServicosTransversais(builder.Configuration);

var app = builder.Build();

await app.Services.InicializarPersistenciaAsync();

app.UseExceptionHandler();
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

app.UseRateLimiter();
app.UseRequestTimeouts();
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
