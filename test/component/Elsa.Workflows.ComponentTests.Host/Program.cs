using Elsa.Extensions;
using Elsa.Tenants.AspNetCore;
using Elsa.Workflows.Activities.Flowchart.Extensions;
using Elsa.Workflows.Api;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.ComponentTests.Host;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Runtime.Distributed.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// TUnit captures application logs in each result. Information-level EF command logging from
// hundreds of isolated hosts otherwise dominates both runtime and report size.
builder.Logging.SetMinimumLevel(LogLevel.Warning);

// The component factory appends invocation-specific providers and applies this module once.
services.ConfigureElsa(elsa => elsa
    .UseWorkflows(workflows => workflows.UseCommitStrategies(strategies =>
    {
        strategies.AddStandardStrategies();
        strategies.Add("Every 10 seconds", new PeriodicWorkflowStrategy(TimeSpan.FromSeconds(10)));
    }))
    .UseFlowchart(flowchart => flowchart.UseTokenBasedExecution())
    .UseWorkflowManagement(management =>
    {
        management.SetDefaultLogPersistenceMode(LogPersistenceMode.Inherit);
        management.UseCache();
        management.UseReadOnlyMode(false);
    })
    .UseWorkflowRuntime(runtime =>
    {
        runtime.UseCache();
        runtime.UseDistributedRuntime();
    })
    .UseWorkflowsApi()
    .UseScheduling()
    .UseHttp(http => http.UseCache()));

services.AddActivityStateFilter<ComponentHttpRequestAuthenticationHeaderFilter>();
services.AddControllers();

var app = builder.Build();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseTenants();
app.UseJsonSerializationErrorHandler();
app.UseWorkflows();
app.MapWorkflowsApi("elsa/api");
app.MapControllers();

await app.RunAsync();
