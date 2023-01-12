using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

services
    .AddElsaApiEndpoints()
    .AddElsa(options => options
        .AddConsoleActivities()
        .AddHttpActivities(httpOptions => configuration.GetSection("Elsa:Http").Bind(httpOptions))
        .AddWorkflowsFrom<Program>()
    )
    .AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("Content-Disposition")));

var app = builder.Build();

app.UseCors();
app.UseRouting();
app.UseHttpActivities();
app.UseEndpoints(endpoints => endpoints.MapControllers());
app.UseWelcomePage();

app.Run();