using System;
using Elsa.Extensions;
using Elsa.Samples.Webhooks.WorkflowServer.Workflows;
using Elsa.Webhooks.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHandlersFrom<Program>();

builder.Services.AddElsa(elsa =>
{
    elsa.AddWorkflow<HungryWorkflow>()
    .UseHttp()
    .UseIdentity(identity =>
    {
        identity.IdentityOptions.CreateDefaultAdmin = true;
        identity.TokenOptions.SigningKey = "secret-signing-key-for-tokens";
    })
    .UseWorkflowsApi(api => api.CompleteTaskPolicy = policy => policy.RequireAssertion(_ => true)) // Allows anonymous requests. Will be replaced with an API key scheme.
    .UseDefaultAuthentication()
    .UseWebhooks(webhooks => webhooks.RegisterWebhook(new Uri("https://localhost:7100/webhooks/run-task")));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();