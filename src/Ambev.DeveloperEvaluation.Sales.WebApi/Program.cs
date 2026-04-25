using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;

var app = ServiceApiHostExtensions.BuildServiceApi(args, "Ambev.DeveloperEvaluation.Sales.WebApi");
await app.RunAsync();