using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Extensions;
using Elsa.Identity;
using Elsa.Identity.Options;
using Elsa.Jobs.Activities.Services;
using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Requirements;
using Elsa.WorkflowServer.Web.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Proto.Persistence.Sqlite;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;
var identityOptions = new IdentityOptions();
var identityTokenOptions = new IdentityTokenOptions();
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
identitySection.Bind(identityOptions);
identityTokenSection.Bind(identityTokenOptions);

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .AddActivitiesFrom<Program>()
        .UseIdentity(identity =>
        {
            identity.IdentityOptions = identityOptions;
            identity.TokenOptions = identityTokenOptions;
        })
        .UseDefaultAuthentication()
        .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseProtoActor(proto => proto.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString)));
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            runtime.UseAsyncWorkflowStateExporter();
            runtime.UseMassTransitDispatcher();
        })
        .UseMassTransit(massTransit => massTransit.UseRabbitMq(rabbitMqConnectionString))
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

services.AddHandlersFrom<Program>();
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