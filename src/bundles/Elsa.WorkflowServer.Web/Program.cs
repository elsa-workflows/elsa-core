using Elsa.AspNetCore;
using Elsa.Extensions;
using Elsa.Hangfire.Implementations;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Labels.EntityFrameworkCore.Extensions;
using Elsa.Labels.EntityFrameworkCore.Sqlite;
using Elsa.Labels.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.Quartz.Implementations;
using Elsa.Scheduling.Extensions;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Sqlite;
using Elsa.Workflows.Persistence.Extensions;
using Elsa.Workflows.Runtime.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services
    .ConfigureElsa()
    .UseWorkflows()
    .UseManagement(management => management
        .AddActivity<Sequence>()
        .AddActivity<WriteLine>()
        .AddActivity<ReadLine>()
        .AddActivity<If>()
        .AddActivity<HttpEndpoint>()
        .AddActivity<Flowchart>()
        .AddActivity<Elsa.Scheduling.Activities.Delay>()
        .AddActivity<Elsa.Scheduling.Activities.Timer>()
        .AddActivity<ForEach>()
        .AddActivity<Switch>()
        .AddActivity<RunJavaScript>()
    )
    .UsePersistence(p => p.UseEntityFrameworkCore(ef => ef.UseSqlite()))
    .UseRuntime(runtime => runtime.UseProtoActor())
    .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite()))
    .UseHttp()
    .Apply();

services
    .AddJobServices(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddSchedulingServices()
    ;

// Add controller services. The below technique allows full control over what controllers get added from which assemblies.
// It is even possible to add individual controllers this way using a custom TypesPart.
services
    // Elsa API endpoints require MVC controllers. 
    .AddControllers(mvc => mvc.Conventions.Add(new ApiEndpointAttributeConvention())) // This convention is required as well. 
    .ClearApplicationParts() // Remove all controllers from referenced packages.
    .AddApplicationPartsFrom<Program>() // Add back any controllers from the current application.
    .AddWorkflowManagementApiControllers() // Add workflow management API endpoint controllers.
    .AddLabelsApiControllers() // Add label API controllers.
    ;

services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Register scripting languages.
services
    .AddJavaScriptExpressions()
    .AddLiquidExpressions();

// Register serialization configurator for configuring what types to allow to be serialized.
services.AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>();
services.AddSingleton<ISerializationOptionsConfigurator, SerializationOptionsConfigurator>();

// Configure middleware pipeline.
var app = builder.Build();
var serviceProvider = app.Services;

// Add type aliases for prettier JSON serialization.
var wellKnownTypeRegistry = serviceProvider.GetRequiredService<IWellKnownTypeRegistry>();
wellKnownTypeRegistry.RegisterType<int>("int");
wellKnownTypeRegistry.RegisterType<float>("float");
wellKnownTypeRegistry.RegisterType<bool>("boolean");
wellKnownTypeRegistry.RegisterType<string>("string");

// Configure workflow engine execution pipeline.
serviceProvider.ConfigureDefaultWorkflowExecutionPipeline(pipeline =>
    pipeline
        .UseWorkflowExecutionEvents()
        .UseWorkflowExecutionLogPersistence()
        .UsePersistence()
        .UseWorkflowContexts()
        .UseStackBasedActivityScheduler()
);

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

// Map Elsa API endpoint controllers.
app.MapWorkflowManagementApiEndpoints();
app.MapLabelApiEndpoints();

// Register Elsa middleware.
app.UseJsonSerializationErrorHandler();
app.UseHttpActivities();

// Run.
app.Run();