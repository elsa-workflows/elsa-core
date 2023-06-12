using Elsa.Extensions;
using Elsa.Samples.AspNet.WorkflowSinks.Sinks;
using Elsa.Samples.AspNet.WorkflowSinks.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddElsa(elsa =>
{
    // Register the workflow with the runtime so that the sink can find it.
    elsa.UseWorkflowRuntime(runtime => runtime.AddWorkflow<OrderWorkflow>());
    
    // Enable the Workflow Sinks feature and install our custom Customer Details sink.
    elsa.UseWorkflowSinks(sinks => sinks.AddWorkflowSink<CustomerDetailsSink>());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.MapControllers();
app.Run();