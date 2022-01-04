using Elsa.Activities.Console;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Workflows;
using Elsa.Api.Extensions;
using Elsa.Extensions;
using Elsa.Management.Contracts;
using Elsa.Management.Extensions;
using Elsa.Mediator.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Sqlite;
using Elsa.Persistence.Middleware.WorkflowExecution;
using Elsa.Pipelines.WorkflowExecution.Components;
using Elsa.Runtime.ProtoActor.Extensions;
using Elsa.Samples.Web1.Activities;
using Elsa.Samples.Web1.Workflows;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

var services = builder.Services;

// Add services.
services
    .AddElsa()
    .AddMediator()
    //.AddInMemoryPersistence()
    .AddEntityFrameworkCorePersistence((_, ef) => ef.UseSqlite())
    .IndexWorkflowTriggers()
    .AddElsaManagement()
    .AddHttpActivityServices()
    .AddProtoActorWorkflowHost()
    .ConfigureWorkflowRuntime(options =>
    {
        options.Workflows.Add("HelloWorldWorkflow", new HelloWorldWorkflow());
        options.Workflows.Add("HttpWorkflow", new HttpWorkflow());
        options.Workflows.Add("ForkedHttpWorkflow", new ForkedHttpWorkflow());
        options.Workflows.Add(nameof(CompositeActivitiesWorkflow), new CompositeActivitiesWorkflow());
    });

// Testing only: allow client app to connect from anywhere.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Register available activities.
services
    .AddActivity<WriteLine>()
    .AddActivity<WriteLines>()
    .AddActivity<ReadLine>()
    .AddActivity<If>()
    .AddActivity<HttpTrigger>()
    .AddActivity<Flowchart>();

// Register available triggers.
services
    .AddTrigger<HttpTrigger>();

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
serviceProvider.ConfigureDefaultWorkflowExecutionPipeline(pipeline => pipeline
    .PersistWorkflows()
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