using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Identity;
using Elsa.Identity.Options;
using Elsa.JavaScript.Activities;
using Elsa.Jobs.Activities.Implementations;
using Elsa.Jobs.Activities.Middleware.Activities;
using Elsa.Jobs.Activities.Services;
using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.MassTransit.Options;
using Elsa.Requirements;
using Elsa.Scheduling.Activities;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Middleware.Activities;
using Elsa.Workflows.Core.Middleware.Workflows;
using Elsa.Workflows.Management.Services;
using Elsa.WorkflowServer.Web.Jobs;
using Microsoft.AspNetCore.Authorization;

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
var rabbitMqOptions = new RabbitMqOptions();
configuration.GetSection(RabbitMqOptions.RabbitMq).Bind(rabbitMqOptions);

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseWorkflowManagement(management => management
            .UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString))
            .AddActivitiesFrom<WriteLine>()
            .AddActivitiesFrom<HttpEndpoint>()
            .AddActivitiesFrom<Delay>()
            .AddActivitiesFrom<RunJavaScript>()
            .AddActivitiesFrom<Program>()
        )
        .UseIdentity(identity =>
        {
            identity.IdentityOptions = identityOptions;
            identity.TokenOptions = identityTokenOptions;
        })
        .UseDefaultAuthentication()
        .UseWorkflowRuntime(runtime =>
        {
            //runtime.UseProtoActor(proto => proto.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString)));
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
        })
        .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseActivityDefinitions(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseJobs(jobs => jobs.ConfigureOptions = options => options.WorkerCount = 10)
        .UseJobActivities()
        .UseScheduling()
        .UseWorkflowsApi()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
    );

services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
services.AddHttpContextAccessor();
services.AddSingleton<IAuthorizationHandler, LocalHostRequirementHandler>();
services.AddAuthorization(options => options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostRequirement())));

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
        .UsePersistentVariables()
        .UseBookmarkPersistence()
        .UseWorkflowContexts()
        .UseDefaultActivityScheduler()
);

// Configure activity execution pipeline to use the job-based activity invoker.
serviceProvider.ConfigureDefaultActivityExecutionPipeline(pipeline => pipeline
    .UseExceptionHandling()
    .UseJobBasedActivityInvoker());

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

app.UseAuthentication();
app.UseAuthorization();

// Register Elsa middleware.
app.UseWorkflowsApi();
app.UseJsonSerializationErrorHandler();
app.UseWorkflows();

// Run.
app.Run();