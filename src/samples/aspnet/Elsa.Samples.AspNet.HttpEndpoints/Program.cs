using Elsa.Extensions;
using Elsa.Samples.AspNet.HttpEndpoints.Workflows;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Add workflow.
    elsa.AddWorkflow<SubmissionWorkflow>();
    
    // Enable the HTTP feature for HTTP activities.
    elsa.UseHttp();
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseWorkflows();
app.Run();