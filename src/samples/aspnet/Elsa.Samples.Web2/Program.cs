using Elsa.Extensions;
using Elsa.Hangfire.Implementations;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.Quartz.Implementations;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Core.Serialization;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Serialization;
using Elsa.Workflows.Runtime.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add services.
services
    .AddElsa(elsa => elsa
        .UseWorkflows()
        .UseRuntime(runtime => runtime.UseProtoActor())
        .UseManagement(management => management
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
            .AddActivity<RunJavaScript>())
        .UseHttp());

services
    .AddJobServices(new QuartzJobSchedulerProvider(), new HangfireJobQueueProvider())
    .AddSchedulingServices();

// Testing only: allow client app to connect from anywhere.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Register scripting languages.
services
    .AddJavaScriptExpressions()
    .AddLiquidExpressions();

// Register serialization configurator for configuring what types to allow to be serialized.
services.AddSingleton<ISerializationOptionsConfigurator, CustomSerializationOptionConfigurator>();
services.AddSingleton<ISerializationOptionsConfigurator, SerializationOptionsConfigurator>();

// Configure middleware pipelines.
var app = builder.Build();
var serviceProvider = app.Services;

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
app.MapManagementApiEndpoints();

// Register Elsa HTTP activity middleware.
app.UseHttpActivities();


// Run.
app.Run();