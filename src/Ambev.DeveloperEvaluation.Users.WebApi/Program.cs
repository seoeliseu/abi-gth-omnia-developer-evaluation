using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Users.WebApi",
	(services, _) => services.AdicionarServicosAplicacaoUsers());
await app.RunAsync();