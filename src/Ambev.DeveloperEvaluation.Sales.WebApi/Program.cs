using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Sales.WebApi",
	(services, _) => services.AdicionarServicosAplicacaoSales());
await app.RunAsync();