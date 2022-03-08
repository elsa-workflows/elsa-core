using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Workflows;
using Elsa.Api.Extensions;
using Elsa.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Management.Contracts;
using Elsa.Management.Extensions;
using Elsa.Mediator.Extensions;
using Elsa.Modules.AzureServiceBus.Activities;
using Elsa.Modules.AzureServiceBus.Extensions;
using Elsa.Modules.Hangfire.Services;
using Elsa.Modules.Http;
using Elsa.Modules.Http.Extensions;
using Elsa.Modules.JavaScript.Activities;
using Elsa.Modules.Quartz.Services;
using Elsa.Modules.Scheduling.Activities;
using Elsa.Modules.Scheduling.Extensions;
using Elsa.Modules.Scheduling.Triggers;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Sqlite;
using Elsa.Pipelines.WorkflowExecution.Components;
using Elsa.Runtime.Extensions;
using Elsa.Runtime.ProtoActor.Extensions;
using Elsa.Samples.Web1.Activities;
using Elsa.Samples.Web1.Workflows;
using Elsa.Scripting.JavaScript.Extensions;
using Elsa.Scripting.Liquid.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;
var configuration = builder.Configuration;

// Add services.
services
    .AddElsa()
    .AddMediator()
    .AddEntityFrameworkCorePersistence((_, ef) => ef.UseSqlite())
    .AddProtoActorWorkflowHost()
    .IndexWorkflowTriggers()
    .AddElsaManagement()
    .AddJobs(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddHttpActivityServices()
    .AddJobServices()
    .AddAzureServiceBusServices(options => configuration.GetSection("AzureServiceBus").Bind(options))
    .ConfigureWorkflowRuntime(options =>
    {
        // Register workflows.
        options.Workflows.Add(nameof(HelloWorldWorkflow), new HelloWorldWorkflow());
        options.Workflows.Add(nameof(HttpWorkflow), new HttpWorkflow());
        options.Workflows.Add(nameof(ForkedHttpWorkflow), new ForkedHttpWorkflow());
        options.Workflows.Add(nameof(CompositeActivitiesWorkflow), new CompositeActivitiesWorkflow());
        options.Workflows.Add(nameof(SendMessageWorkflow), new SendMessageWorkflow());
        options.Workflows.Add(nameof(ReceiveMessageWorkflow), new ReceiveMessageWorkflow());
        options.Workflows.Add(nameof(RunJavaScriptWorkflow), new RunJavaScriptWorkflow());
    });

// Testing only: allow client app to connect from anywhere.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Register available activities.
services
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
    .AddActivity<SendMessage>()
    .AddActivity<RunJavaScript>()
    ;

// Register scripting languages.
services
    .AddJavaScriptExpressions()
    .AddLiquidExpressions();

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
        .UseActivityScheduler()
);

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Root.
app.MapGet("/", () => "Hello World!");

// Map Elsa API endpoints.
app.MapElsaApiEndpoints();

// Register Elsa middleware.
app.UseJsonSerializationErrorHandler();
app.UseHttpActivities();

// Run.
app.Run();