using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Auth.WebApi",
	(services, configuration) =>
	{
		services.AdicionarServicosAplicacaoAuth();
		services.AdicionarMensageriaAuth(configuration);
	});
await app.RunAsync();