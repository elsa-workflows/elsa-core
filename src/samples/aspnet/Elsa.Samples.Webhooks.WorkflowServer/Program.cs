using System;
using Elsa.Extensions;
using Elsa.Samples.Webhooks.WorkflowServer.Workflows;
using Elsa.Webhooks.Extensions;
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
    elsa.UseIdentity(identity => identity.IdentityOptions.CreateDefaultAdmin = true);
    elsa.UseWorkflowsApi();
    elsa.UseDefaultAuthentication();
    elsa.UseWebhooks(webhooks => webhooks.RegisterWebhook(new Uri("https://localhost:7100/webhooks/run-task")));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();