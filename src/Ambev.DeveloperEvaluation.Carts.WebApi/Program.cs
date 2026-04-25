using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Carts.WebApi",
	(services, _) => services.AdicionarServicosAplicacaoCarts());
await app.RunAsync();