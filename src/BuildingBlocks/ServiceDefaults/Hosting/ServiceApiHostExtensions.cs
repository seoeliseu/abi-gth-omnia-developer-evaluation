using System.Diagnostics;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.RateLimiting;
using Ambev.DeveloperEvaluation.Common.Observabilidade;
using Ambev.DeveloperEvaluation.Common.Security;
using Ambev.DeveloperEvaluation.IoC;
using Ambev.DeveloperEvaluation.IoC.Security;
using Ambev.DeveloperEvaluation.ORM.Persistence;
using Ambev.DeveloperEvaluation.ServiceDefaults.Middlewares;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Formatting.Compact;

namespace Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;

public static class ServiceApiHostExtensions
{
    public static WebApplication BuildServiceApi(string[] args, string serviceName, Action<IServiceCollection, IConfiguration>? configureServices = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.Global.json", optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.Global.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables();

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
                .Enrich.WithProperty("serviceName", serviceName)
                .WriteTo.Console(new CompactJsonFormatter());

            if (!string.IsNullOrWhiteSpace(urlSeq))
            {
                configuracaoLogger.WriteTo.Seq(urlSeq);
            }
        });

        builder.Services.AddControllers(opcoes =>
        {
            opcoes.SuppressAsyncSuffixInActionNames = false;
        });
        builder.Services.AddProblemDetails();
        ConfigurarDataProtection(builder.Services, builder.Configuration);
        ConfigurarAutenticacao(builder.Services, builder.Configuration);
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
        builder.Services.AdicionarInfraestruturaCompartilhada(builder.Configuration);
        configureServices?.Invoke(builder.Services, builder.Configuration);

        var app = builder.Build();

        app.Services.InicializarPersistenciaAsync().GetAwaiter().GetResult();

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

        if (DeveRedirecionarParaHttps(app.Configuration))
        {
            app.UseHttpsRedirection();
        }

        app.UseAuthentication();
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

        return app;
    }

    private static void ConfigurarAutenticacao(IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = JwtAccessTokenIssuer.ResolveOptions(configuration);
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opcoes =>
            {
                opcoes.RequireHttpsMetadata = false;
                opcoes.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();
    }

    private static void ConfigurarDataProtection(IServiceCollection services, IConfiguration configuration)
    {
        var dataProtectionBuilder = services
            .AddDataProtection()
            .SetApplicationName("Ambev.DeveloperEvaluation");

        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            Directory.CreateDirectory(keysPath);
            dataProtectionBuilder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        var certificatePath = configuration["DataProtection:CertificatePath"];
        var certificatePassword = configuration["DataProtection:CertificatePassword"];
        if (!string.IsNullOrWhiteSpace(certificatePath))
        {
            if (!File.Exists(certificatePath))
            {
                throw new InvalidOperationException($"O certificado de Data Protection não foi encontrado em '{certificatePath}'.");
            }

            var certificate = new X509Certificate2(
                certificatePath,
                certificatePassword,
                X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable);

            dataProtectionBuilder.ProtectKeysWithCertificate(certificate);
        }
    }

    private static bool DeveRedirecionarParaHttps(IConfiguration configuration)
    {
        return configuration.GetValue("Hosting:HttpsRedirectionEnabled", true);
    }
}