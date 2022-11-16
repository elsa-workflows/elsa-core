using Elsa.Extensions;
using Elsa.Features.Extensions;
using Elsa.Identity;
using Elsa.Identity.Features;
using Elsa.Identity.Options;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Activities.Extensions;
using Elsa.Jobs.Activities.Middleware.Activities;
using Elsa.Jobs.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.Modules.Management;
using Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Management;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Runtime;
using Elsa.ProtoActor.Extensions;
using Elsa.Requirements;
using Elsa.Scheduling.Extensions;
using Elsa.Telnyx.Extensions;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Core.Middleware.Workflows;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.Workflows.Runtime.Implementations;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Event = Elsa.Workflows.Core.Activities.Event;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite");
var identityOptions = new IdentityOptions();
var identitySection = configuration.GetSection("Identity");
identitySection.Bind(identityOptions);

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseWorkflowManagement(management => management
            .UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString))
            .AddActivity<WriteLine>()
            .AddActivity<ReadLine>()
            .AddActivity<If>()
            .AddActivity<Flowchart>()
            .AddActivity<FlowDecision>()
            .AddActivity<FlowSwitch>()
            .AddActivity<FlowJoin>()
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
        .UseRuntime(runtime =>
        {
            runtime.UseProtoActor(proto =>
            {
                //proto.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString));
            });
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            runtime.WorkflowStateExporter = sp => sp.GetRequiredService<AsyncWorkflowStateExporter>();
        })
        .UseActivityDefinitions(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseJobs(jobs => jobs.ConfigureOptions = options => options.WorkerCount = 10)
        .UseJobActivities()
        .UseScheduling()
        .UseWorkflowApiEndpoints()
        .UseJavaScript()
        .UseLiquid()
        .UseTelnyx(telnyx => telnyx.ConfigureTelnyxOptions = options => configuration.GetSection("Telnyx").Bind(options))
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

// Grant localhost requests security root privileges.
services.AddAuthorization(options => options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostRequirement())));

// Configure middleware pipeline.
var app = builder.Build();
var serviceProvider = app.Services;

// Configure workflow engine execution pipeline.
serviceProvider.ConfigureDefaultWorkflowExecutionPipeline(pipeline =>
    pipeline
        .UsePersistentVariables()
        .UseWorkflowContexts()
        .UseDefaultActivityScheduler()
);

// Configure activity execution pipeline to use the job-based activity invoker.
serviceProvider.ConfigureDefaultActivityExecutionPipeline(pipeline => pipeline.UseJobBasedActivityInvoker());

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

// Telnyx webhook endpoint.
app.UseTelnyxWebhooks();

app.UseAuthentication();
app.UseAuthorization();

// Register Elsa middleware.
app.UseElsaFastEndpoints();
app.UseJsonSerializationErrorHandler();

// Run.
app.Run();