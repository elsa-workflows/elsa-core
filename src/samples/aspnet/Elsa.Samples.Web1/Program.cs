using Elsa.AspNetCore;
using Elsa.Extensions;
using Elsa.Hangfire.Implementations;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.Quartz.Implementations;
using Elsa.Samples.Web1.Activities;
using Elsa.Samples.Web1.Serialization;
using Elsa.Samples.Web1.Workflows;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Extensions;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Sqlite;
using Elsa.Workflows.Persistence.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

// Run the SqlServer container from docker-compose.yml to start a SQL Server container.
var sqlServerConnectionString = configuration.GetConnectionString("SqlServer");

// Add Elsa services.
services
    .AddElsa(elsa => elsa.ConfigureWorkflowPersistence(p => p.UseEntityFrameworkCoreProvider(ef => ef.UseSqlite())))
    //.AddProtoActorWorkflowHost()
    .AddJobServices(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddSchedulingServices()
    .AddHttpActivityServices()
    //.AddAzureServiceBusServices(options => configuration.GetSection("AzureServiceBus").Bind(options))
    .ConfigureWorkflowRuntime(options =>
    {
        // Register workflows.
        options.Workflows.Add<HelloWorldWorkflow>();
        //options.Workflows.Add<HeartbeatWorkflow>();
        options.Workflows.Add<HttpWorkflow>();
        options.Workflows.Add<ForkedHttpWorkflow>();
        options.Workflows.Add<CompositeActivitiesWorkflow>();
        options.Workflows.Add<SendMessageWorkflow>();
        options.Workflows.Add<ReceiveMessageWorkflow>();
        options.Workflows.Add<RunJavaScriptWorkflow>();
        options.Workflows.Add<WorkflowContextsWorkflow>();
        options.Workflows.Add<SubmitJobWorkflow>();
        options.Workflows.Add<DelayWorkflow>();
        options.Workflows.Add<OrderProcessingWorkflow>();
        options.Workflows.Add<StartAtTriggerWorkflow>();
        options.Workflows.Add<StartAtBookmarkWorkflow>();
    });

// Add controller services. The below technique allows full control over what controllers get added from which assemblies.
// It is even possible to add individual controllers this way using a custom TypesPart.
services
    // Elsa API endpoints require MVC controllers. 
    .AddControllers(mvc => mvc.Conventions.Add(new ApiEndpointAttributeConvention())) // This convention is required as well. 
    .ClearApplicationParts() // Remove all controllers from referenced packages.
    .AddApplicationPartsFrom<Program>() // Add back any controllers from the current application.
    .AddElsaApiControllers() // Add Elsa API endpoint controllers.
    ;

// Testing only: allow client app to connect from anywhere.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Register activities available from the designer.
services
    .AddActivity<Sequence>()
    .AddActivity<WriteLine>()
    .AddActivity<WriteLines>()
    .AddActivity<ReadLine>()
    .AddActivity<If>()
    .AddActivity<HttpEndpoint>()
    .AddActivity<Flowchart>()
    .AddActivity<Delay>()
    .AddActivity<Timer>()
    .AddActivity<ForEach>()
    .AddActivity<Switch>()
    //.AddActivity<SendMessage>()
    //.AddActivity<MessageReceived>()
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
        .UseWorkflowContexts()
        .UseStackBasedActivityScheduler()
);

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Root.
app.MapGet("/", () => "Hello World!");

// Map Elsa API endpoint controllers.
app.MapElsaApiEndpoints("elsa/api");

// Register Elsa middleware.
app.UseJsonSerializationErrorHandler();
app.UseHttpActivities();

// Run.
app.Run();