using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Products.WebApi",
	(services, configuration) =>
	{
		services.AdicionarServicosAplicacaoProducts(configuration);
		services.AdicionarMensageriaProducts(configuration);
	});
await app.RunAsync();