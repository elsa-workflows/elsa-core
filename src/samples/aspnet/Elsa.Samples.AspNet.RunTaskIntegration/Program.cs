using Elsa.Extensions;
using Elsa.Samples.AspNet.RunTaskIntegration.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddHandlersFrom<Program>();

builder.Services.AddElsa(elsa =>
{
    elsa.AddWorkflow<HungryWorkflow>();
    elsa.UseHttp();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseWorkflows();
app.Run();