using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.JavaScript.Activities;
using Elsa.Jobs.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Scheduling.Extensions;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Management.Extensions;
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
        .UseRuntime(runtime => runtime.UseProtoActor(f=>
        {
            f.ClusterName = "my-cluster";
        }))
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
        .UseJobs()
        .UseScheduling()
        .UseWorkflowApiEndpoints()
        .UseHttp()
    );

// Testing only: allow client app to connect from anywhere.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Configure middleware pipelines.
var app = builder.Build();
var serviceProvider = app.Services;

// Configure workflow engine execution pipeline.
serviceProvider.ConfigureDefaultWorkflowExecutionPipeline(pipeline =>
    pipeline
        .UseWorkflowExecutionLogPersistence()
        .UsePersistentVariables()
        .UseStackBasedActivityScheduler()
);

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Register Elsa HTTP activity middleware.
app.UseHttpActivities();


// Run.
app.Run();