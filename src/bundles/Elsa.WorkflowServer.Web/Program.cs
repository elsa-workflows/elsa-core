using Elsa.Extensions;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.Identity;
using Elsa.Identity.Extensions;
using Elsa.Identity.Options;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Jobs.Activities.Extensions;
using Elsa.Jobs.Activities.Implementations;
using Elsa.Jobs.Activities.Middleware.Activities;
using Elsa.Jobs.Activities.Services;
using Elsa.Jobs.Extensions;
using Elsa.Labels.Extensions;
using Elsa.Liquid.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.Modules.Labels;
using Elsa.Persistence.EntityFrameworkCore.Modules.Management;
using Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;
using Elsa.Persistence.EntityFrameworkCore.Modules.WorkflowSink;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Labels;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Management;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Runtime;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.WorkflowSink;
using Elsa.Requirements;
using Elsa.Scheduling.Extensions;
using Elsa.WorkflowContexts.Extensions;
using Elsa.Workflows.Api.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Middleware.Activities;
using Elsa.Workflows.Core.Middleware.Workflows;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Management.Services;
using Elsa.Workflows.Runtime.Extensions;
using Elsa.WorkflowServer.Web.Jobs;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

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
            .AddActivitiesFrom<WriteLine>()
            .AddActivitiesFrom<HttpEndpoint>()
            .AddActivitiesFrom<Elsa.Scheduling.Activities.Delay>()
            .AddActivitiesFrom<RunJavaScript>()
            .AddActivitiesFrom<Program>()
        )
        .UseIdentity(identity =>
        {
            identity.CreateDefaultUser = true;
            identity.IdentityOptions = options => identitySection.Bind(options);
        })
        .UseWorkflowRuntime(runtime =>
        {
            //runtime.UseProtoActor(proto => proto.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString)));
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            //runtime.WorkflowStateExporter = sp => sp.GetRequiredService<AsyncWorkflowStateExporter>();
        })
        .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseActivityDefinitions(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseJobs(jobs => jobs.ConfigureOptions = options => options.WorkerCount = 10)
        .UseJobActivities()
        .UseScheduling()
        .UseWorkflowApiEndpoints()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
        .UseMassTransit(feature => feature.UseRabbitMq())
        .UseWorkflowSink(feature =>
        {
            feature.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            feature.UseInMemoryDatabase();
            feature.UseMassTransitServiceBus();
        })
    );

services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Authentication & Authorization.
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, identityOptions.ConfigureJwtBearerOptions);

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
app.UseElsaFastEndpoints();
app.UseJsonSerializationErrorHandler();
app.UseHttpActivities();

// Run.
app.Run();