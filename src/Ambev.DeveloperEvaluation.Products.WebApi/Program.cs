using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;

var app = ServiceApiHostExtensions.BuildServiceApi(args, "Ambev.DeveloperEvaluation.Products.WebApi");
await app.RunAsync();