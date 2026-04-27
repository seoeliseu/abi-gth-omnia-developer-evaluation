using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Carts.WebApi",
	(services, configuration) =>
	{
		services.AdicionarServicosAplicacaoCarts();
		services.AdicionarMensageriaCarts(configuration);
	});
await app.RunAsync();