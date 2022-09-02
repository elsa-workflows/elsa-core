using Elsa.ActivityDefinitions.EntityFrameworkCore.Extensions;
using Elsa.ActivityDefinitions.EntityFrameworkCore.Sqlite;
using Elsa.Extensions;
using Elsa.Features.Extensions;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.Identity.Features;
using Elsa.Identity.Options;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Activities.Extensions;
using Elsa.Jobs.Activities.Implementations;
using Elsa.Jobs.Activities.Services;
using Elsa.Labels.EntityFrameworkCore.Extensions;
using Elsa.Labels.EntityFrameworkCore.Sqlite;
using Elsa.Labels.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.ProtoActor.Extensions;
using Elsa.Scheduling.Extensions;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution.Components;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Extensions;
using Elsa.Workflows.Persistence.EntityFrameworkCore.Sqlite;
using Elsa.Workflows.Persistence.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.WorkflowServer.Web.Jobs;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Proto.Persistence.Sqlite;
using Event = Elsa.Workflows.Core.Activities.Event;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var dbConnectionString = configuration.GetConnectionString("Sqlite");
var identityOptions = new IdentityOptions();
var identitySection = configuration.GetSection("Identity");
identitySection.Bind(identityOptions);

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseManagement(management => management
            .AddActivity<WriteLine>()
            .AddActivity<ReadLine>()
            .AddActivity<If>()
            .AddActivity<HttpEndpoint>()
            .AddActivity<WriteHttpResponse>()
            .AddActivity<Flowchart>()
            .AddActivity<FlowDecision>()
            .AddActivity<FlowSwitch>()
            .AddActivity<Elsa.Scheduling.Activities.Delay>()
            .AddActivity<Elsa.Scheduling.Activities.Timer>()
            .AddActivity<ForEach>()
            .AddActivity<Switch>()
            .AddActivity<RunJavaScript>()
            .AddActivity<Event>()
        )
        .Use<IdentityFeature>(identity =>
        {
            identity.CreateDefaultUser = true;
            identity.IdentityOptions = options => identitySection.Bind(options);
        })
        .UseRuntime(runtime => runtime.UseProtoActor(proto => proto.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(dbConnectionString))))
        .UseJobActivities()
        .UseScheduling()
        .UseWorkflowPersistence(p => p.UseEntityFrameworkCore(ef => ef.UseSqlite(dbConnectionString)))
        .UseWorkflowApiEndpoints()
        .UseJavaScript()
        .UseLiquid()
        .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseCustomActivities(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseHttp()
    );

services.AddFastEndpoints();
services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Authentication & Authorization.
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, identityOptions.ConfigureJwtBearerOptions);

services.AddHttpContextAccessor();
services.AddSingleton<IAuthorizationHandler, LocalHostRequirementHandler>();
services.AddAuthorization(options => options.AddPolicy("SecurityRoot", policy => policy.AddRequirements(new LocalHostRequirement())));

// Configure middleware pipeline.
var app = builder.Build();
var serviceProvider = app.Services;

// Register a dummy job for demo purposes.
var jobRegistry = serviceProvider.GetRequiredService<IJobRegistry>();
jobRegistry.Add(typeof(IndexBlockchainJob));

// Update activity providers.
var activityRegistryPopulator = serviceProvider.GetRequiredService<IActivityRegistryPopulator>();
activityRegistryPopulator.PopulateRegistryAsync(typeof(JobActivityProvider));

// Configure workflow engine execution pipeline.
serviceProvider.ConfigureDefaultWorkflowExecutionPipeline(pipeline =>
    pipeline
        .UseWorkflowExecutionEvents()
        .UseWorkflowContexts()
        .UseStackBasedActivityScheduler()
);

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

app.UseAuthentication();
app.UseAuthorization();

// Register Elsa middleware.
app.UseElsaFastEndpoints();
app.UseJsonSerializationErrorHandler();
app.UseHttpActivities();

// Run.
app.Run();