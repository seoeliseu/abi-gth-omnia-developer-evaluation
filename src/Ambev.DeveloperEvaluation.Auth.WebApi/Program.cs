using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;

var app = ServiceApiHostExtensions.BuildServiceApi(args, "Ambev.DeveloperEvaluation.Auth.WebApi");
await app.RunAsync();