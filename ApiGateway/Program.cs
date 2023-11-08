using Microsoft.Extensions.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Values;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

var builder = WebApplication.CreateBuilder(args);

//builder.Configuration.AddJsonFile("Ocelot.json", optional: false, reloadOnChange: true);
//builder.Configuration.AddSwaggerForOcelot("Ocelot.json");
builder.Services.AddApplicationInsightsTelemetry();
builder.Services.AddApplicationInsightsTelemetry();
var app = builder.Build();
app.UseSwaggerForOcelotUI(opt =>
{
    opt.PathToSwaggerGenerator = "/swagger/docs";
});
app.MapGet("/", () => "Hello World!");
app.MapControllers();
await app.UseOcelot();

app.Run();
