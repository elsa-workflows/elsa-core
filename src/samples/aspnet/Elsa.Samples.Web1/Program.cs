using Elsa.Extensions;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.Samples.Web1.Workflows;
using Elsa.Scheduling.Extensions;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
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
    .AddElsa(elsa => elsa
        .UseWorkflowPersistence(persistence => persistence.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseRuntime(runtime => runtime
            .AddWorkflow<HelloWorldWorkflow>()
            .AddWorkflow<HttpWorkflow>()
            .AddWorkflow<ForkedHttpWorkflow>()
            .AddWorkflow<CompositeActivitiesWorkflow>()
            .AddWorkflow<SendMessageWorkflow>()
            .AddWorkflow<ReceiveMessageWorkflow>()
            .AddWorkflow<RunJavaScriptWorkflow>()
            .AddWorkflow<WorkflowContextsWorkflow>()
            .AddWorkflow<SubmitJobWorkflow>()
            .AddWorkflow<DelayWorkflow>()
            .AddWorkflow<OrderProcessingWorkflow>()
            .AddWorkflow<StartAtTriggerWorkflow>()
            .AddWorkflow<StartAtBookmarkWorkflow>())
        .UseJobs()
        .UseScheduling()
        .UseWorkflowApiEndpoints()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
    );

// Testing only: allow client app to connect from anywhere.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Configure middleware pipeline.
var app = builder.Build();
var serviceProvider = app.Services;

// Configure workflow engine execution pipeline.
serviceProvider.ConfigureDefaultWorkflowExecutionPipeline(pipeline =>
    pipeline
        .UseWorkflowExecutionLogPersistence()
        .UsePersistentVariables()
        .UseWorkflowContexts()
        .UseStackBasedActivityScheduler()
);

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Root.
app.MapGet("/", () => "Hello World!");

// Register Elsa middleware.
app.UseJsonSerializationErrorHandler();
app.UseHttpActivities();

// Run.
app.Run();