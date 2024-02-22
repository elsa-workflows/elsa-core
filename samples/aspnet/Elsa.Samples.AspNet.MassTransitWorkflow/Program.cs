using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Samples.AspNet.MassTransitWorkflow.Messages;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore());

    // Configure runtime feature to use EF Core.
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore();
        runtime.UseMassTransitDispatcher();
    });
    
    // Expose API endpoints.
    elsa.UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>());
    
    // Register programmatically defined workflows.
    elsa.AddWorkflowsFrom<Program>();
    
    // Configure MassTransit.
    elsa.UseMassTransit(massTransit =>
    {
        // massTransit.UseRabbitMq(rabbitMqConnectionString);
        massTransit.AddMessageType<Message>();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseWorkflowsApi();

app.MapGet("/", ctx => ctx.Response.WriteAsync("Call 'POST /elsa/api/messages' to send a message over MassTransit."));

app.Run();