using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Sales.WebApi",
	(services, configuration) =>
	{
		services.AdicionarServicosAplicacaoSales();
		services.AdicionarMensageriaSales(configuration);
	});
await app.RunAsync();

public partial class Program;