using Ambev.DeveloperEvaluation.ServiceDefaults.Hosting;

var app = ServiceApiHostExtensions.BuildServiceApi(args, "Ambev.DeveloperEvaluation.Users.WebApi");
await app.RunAsync();