using Elsa.Activities;
using Elsa.Api.Extensions;
using Elsa.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Management.Extensions;
using Elsa.Management.Serialization;
using Elsa.Modules.Activities.Console;
using Elsa.Modules.Activities.Workflows;
using Elsa.Modules.Hangfire.Implementations;
using Elsa.Modules.Http;
using Elsa.Modules.Http.Extensions;
using Elsa.Modules.JavaScript.Activities;
using Elsa.Modules.Quartz.Implementations;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Modules.Scheduling.Extensions;
using Elsa.Persistence.InMemory.Extensions;
using Elsa.Pipelines.WorkflowExecution.Components;
using Elsa.Runtime.Extensions;
using Elsa.Runtime.ProtoActor.Extensions;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.Liquid.Extensions;
using Elsa.Serialization;
using Elsa.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services.
services
    .AddElsa()
    .AddHttpActivityServices()
    .AddProtoActorRuntime()
    .AddJobServices(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddSchedulingServices();

// Testing only: allow client app to connect from anywhere.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Register activities available from the designer.
services
    .AddActivity<Sequence>()
    .AddActivity<WriteLine>()
    .AddActivity<ReadLine>()
    .AddActivity<If>()
    .AddActivity<HttpEndpoint>()
    .AddActivity<Flowchart>()
    .AddActivity<Delay>()
    .AddActivity<Timer>()
    .AddActivity<ForEach>()
    .AddActivity<Switch>()
    .AddActivity<RunJavaScript>()
    ;

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
        .UseStackBasedActivityScheduler()
);

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Map Elsa API endpoints.
app.MapElsaApiEndpoints();

// Register Elsa HTTP activity middleware.
app.UseHttpActivities();


// Run.
app.Run();