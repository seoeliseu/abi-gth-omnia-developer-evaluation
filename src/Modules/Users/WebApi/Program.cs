using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;
using Ambev.DeveloperEvaluation.IoC;

var app = ServiceApiHostExtensions.BuildServiceApi(
	args,
	"Ambev.DeveloperEvaluation.Users.WebApi",
	(services, configuration) =>
	{
		services.AdicionarServicosAplicacaoUsers();
		services.AdicionarMensageriaUsers(configuration);
	});
await app.RunAsync();