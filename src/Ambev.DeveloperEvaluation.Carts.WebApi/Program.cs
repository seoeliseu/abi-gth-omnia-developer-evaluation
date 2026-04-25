using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;

var app = ServiceApiHostExtensions.BuildServiceApi(args, "Ambev.DeveloperEvaluation.Carts.WebApi");
await app.RunAsync();