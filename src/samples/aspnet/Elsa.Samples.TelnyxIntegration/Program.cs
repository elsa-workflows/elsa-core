using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Extensions;
using Elsa.Identity;
using Elsa.Identity.Options;
using Elsa.JavaScript.Activities;
using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Requirements;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Activities.Flowchart.Activities;
using Elsa.Workflows.Runtime.Implementations;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Event = Elsa.Workflows.Runtime.Activities.Event;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var identityOptions = new IdentityOptions();
var identityTokenOptions = new IdentityTokenOptions();
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
identitySection.Bind(identityOptions);
identityTokenSection.Bind(identityTokenOptions);

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseWorkflowManagement(management =>
        {
            management
                .UseDefaultManagement(dm => dm.UseEntityFrameworkCore(ef => ef.UseSqlite()))
                .UseWorkflowInstances(w => w.UseEntityFrameworkCore(ef => ef.UseSqlite()))
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
                .AddActivitiesFrom<Program>();
        })
        .UseIdentity(identity =>
        {
            identity.IdentityOptions = identityOptions;
            identity.TokenOptions = identityTokenOptions;
        })
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseProtoActor(proto =>
            {
                //proto.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString));
            });
            runtime.UseDefaultRuntime(dr => dr.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));
            runtime.UseExecutionLogRecords(d => d.UseEntityFrameworkCore(ef => ef.UseSqlite()));
            runtime.UseExecutionLogRecords(e => e.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));
            runtime.WorkflowStateExporter = sp => sp.GetRequiredService<AsyncWorkflowStateExporter>();
        })
        .UseActivityDefinitions(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseJobs(jobs => jobs.ConfigureOptions = options => options.WorkerCount = 10)
        .UseJobActivities()
        .UseScheduling()
        .UseWorkflowsApi()
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
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, identityTokenOptions.ConfigureJwtBearerOptions);

services.AddHttpContextAccessor();
services.AddSingleton<IAuthorizationHandler, LocalHostRequirementHandler>();

// Grant localhost requests security root privileges.
services.AddAuthorization(options => options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostRequirement())));

// Configure middleware pipeline.
var app = builder.Build();
var serviceProvider = app.Services;

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
app.UseWorkflowsApi();
app.UseJsonSerializationErrorHandler();

// Run.
app.Run();