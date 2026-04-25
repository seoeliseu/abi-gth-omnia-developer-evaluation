using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Auth.WebApi",
	(services, _) => services.AdicionarServicosAplicacaoAuth());
await app.RunAsync();