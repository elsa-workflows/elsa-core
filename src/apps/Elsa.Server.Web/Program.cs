using System.Text.Encodings.Web;
using Elsa.Caching.Options;
using Elsa.Common.RecurringTasks;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Identity.Multitenancy;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Server.Web.Filters;
using Elsa.Tenants.AspNetCore;
using Elsa.Tenants.Extensions;
using Elsa.Workflows;
using Elsa.Workflows.Api;
using Elsa.Workflows.CommitStates.Strategies;
using Elsa.Workflows.IncidentStrategies;
using Elsa.Workflows.LogPersistence;
using Elsa.Workflows.Options;
using Elsa.Workflows.Runtime.Distributed.Extensions;
using Elsa.Workflows.Runtime.Options;
using Elsa.Workflows.Runtime.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

// ReSharper disable RedundantAssignment
const bool useReadOnlyMode = false;
const bool useSignalR = false; // Disabled until Elsa Studio sends authenticated requests.
const bool useMultitenancy = false;
const bool disableVariableWrappers = false;

ObjectConverter.StrictMode = true;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

// Add Elsa services.
services
    .AddElsa(elsa =>
    {
        elsa
            .AddActivitiesFrom<Program>()
            .AddWorkflowsFrom<Program>()
            .UseIdentity(identity =>
            {
                identity.TokenOptions = options => identityTokenSection.Bind(options);
                identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
            })
            .UseDefaultAuthentication()
            .UseWorkflows(workflows =>
            {
                workflows.UseCommitStrategies(strategies =>
                {
                    strategies.AddStandardStrategies();
                    strategies.Add("Every 10 seconds", new PeriodicWorkflowStrategy(TimeSpan.FromSeconds(10)));
                });
            })
            .UseWorkflowManagement(management =>
            {
                management.UseEntityFrameworkCore(ef => ef.UseSqlite());
                management.SetDefaultLogPersistenceMode(LogPersistenceMode.Inherit);
                management.UseCache();
                management.UseReadOnlyMode(useReadOnlyMode);
            })
            .UseWorkflowRuntime(runtime =>
            {
                runtime.UseEntityFrameworkCore(ef => ef.UseSqlite());
                runtime.UseCache();
                runtime.UseDistributedRuntime();
            })
            .UseWorkflowsApi()
            .UseScheduling()
            .UseCSharp(options =>
            {
                options.DisableWrappers = disableVariableWrappers;
                options.AppendScript("string Greet(string name) => $\"Hello {name}!\";");
                options.AppendScript("string SayHelloWorld() => Greet(\"World\");");
            })
            .UseJavaScript(options =>
            {
                options.AllowClrAccess = true;
                options.ConfigureEngine(engine =>
                {
                    engine.Execute("function greet(name) { return `Hello ${name}!`; }");
                    engine.Execute("function sayHelloWorld() { return greet('World'); }");
                });
            })
            .UsePython(python =>
            {
                python.PythonOptions += options =>
                {
                    // Make sure to configure the path to the python DLL. E.g. /opt/homebrew/Cellar/python@3.11/3.11.6_1/Frameworks/Python.framework/Versions/3.11/bin/python3.11
                    // alternatively, you can set the PYTHONNET_PYDLL environment variable.
                    configuration.GetSection("Scripting:Python").Bind(options);

                    options.AddScript(sb =>
                    {
                        sb.AppendLine("def greet():");
                        sb.AppendLine("    return \"Hello, welcome to Python!\"");
                    });
                };
            })
            .UseLiquid(liquid => liquid.FluidOptions = options => options.Encoder = HtmlEncoder.Default)
            .UseHttp(http =>
            {
                http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options);
                http.UseCache();
            });
        ConfigureForTest?.Invoke(elsa);
    });

// Obfuscate HTTP request headers.
services.AddActivityStateFilter<HttpRequestAuthenticationHeaderFilter>();

// Optionally configure recurring tasks using alternative schedules.
services.Configure<RecurringTaskOptions>(options =>
{
    options.Schedule.ConfigureTask<TriggerBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(300));
    options.Schedule.ConfigureTask<PurgeBookmarkQueueRecurringTask>(TimeSpan.FromSeconds(300));
    options.Schedule.ConfigureTask<RestartInterruptedWorkflowsTask>(TimeSpan.FromSeconds(15));
});

services.Configure<RuntimeOptions>(options => { options.InactivityThreshold = TimeSpan.FromSeconds(15); });
services.Configure<BookmarkQueuePurgeOptions>(options => options.Ttl = TimeSpan.FromSeconds(3600));
services.Configure<CachingOptions>(options => options.CacheDuration = TimeSpan.FromDays(1));
services.Configure<IncidentOptions>(options => options.DefaultIncidentStrategy = typeof(ContinueWithIncidentsStrategy));
services.AddHealthChecks();
services.AddControllers();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));

// Build the web application.
var app = builder.Build();

// Configure the pipeline.
if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

// Routing used for SignalR.
app.UseRouting();

// Security.
app.UseAuthentication();
app.UseAuthorization();

// Multitenancy.
if (useMultitenancy)
    app.UseTenants();

// Elsa API endpoints for designer.
var routePrefix = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value.RoutePrefix;
app.UseWorkflowsApi(routePrefix);

// Captures unhandled exceptions and returns a JSON response.
app.UseJsonSerializationErrorHandler();

// Elsa HTTP Endpoint activities.
app.UseWorkflows();

app.MapControllers();

// Swagger API documentation.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

// SignalR.
if (useSignalR)
{
    app.UseWorkflowsSignalRHubs();
}

// Run.
await app.RunAsync();

/// <summary>
/// The main entry point for the application made public for end to end testing.
/// </summary>
[UsedImplicitly]
public partial class Program
{
    /// <summary>
    /// Set by the test runner to configure the module for testing.
    /// </summary>
    public static Action<IModule>? ConfigureForTest { get; set; }
}