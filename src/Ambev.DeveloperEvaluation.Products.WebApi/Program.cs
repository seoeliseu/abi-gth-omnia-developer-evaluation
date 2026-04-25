using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Products.WebApi",
	(services, _) => services.AdicionarServicosAplicacaoProducts());
await app.RunAsync();