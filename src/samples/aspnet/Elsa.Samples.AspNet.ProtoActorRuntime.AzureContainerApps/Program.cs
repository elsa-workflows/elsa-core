using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.ProtoActor.Protos;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Data.Sqlite;
using Proto.Cluster.AzureContainerApps;
using Proto.Persistence.Sqlite;
using Proto.Remote;
using Proto.Remote.GrpcNet;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var protoActorSection = configuration.GetSection("ProtoActor");
var protoActorClusterSection = protoActorSection.GetSection("Cluster");

// Configure Proto Actor cluster provider services.
services.AddAzureContainerAppsProvider(ArmClientProviders.DefaultAzureCredential, options => protoActorClusterSection.GetSection("AzureContainerApps").Bind(options));

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .AddActivitiesFrom<Program>()
        .UseIdentity(identity =>
        {
            identity.IdentityOptions = options => identitySection.Bind(options);
            identity.TokenOptions = options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication()
        .UseWorkflowManagement(management =>
        {
            // Use EF core for workflow definitions and instances.
            management.UseEntityFrameworkCore(m => m.UseSqlite(sqliteConnectionString));
        })
        .UseWorkflowRuntime(runtime =>
        {
            // Use EF core for triggers and bookmarks.
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));

            // Use EF core for execution log records.
            runtime.UseExecutionLogRecords(log => log.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));

            // Install a workflow state exporter to capture workflow states and store them in IWorkflowInstanceStore.
            runtime.UseAsyncWorkflowStateExporter();

            // Use Proto.Actor for workflow execution.
            runtime.UseProtoActor(protoActor =>
            {
                var advertisedHost = ConfigUtils.FindSmallestIpAddress().ToString();

                protoActor.ClusterProvider = sp => sp.GetRequiredService<AzureContainerAppsProvider>();

                protoActor.RemoteConfig = _ => GrpcNetRemoteConfig
                    .BindTo(advertisedHost)
                    .WithProtoMessages(EmptyReflection.Descriptor)
                    .WithProtoMessages(MessagesReflection.Descriptor)
                    .WithLogLevelForDeserializationErrors(LogLevel.Critical)
                    .WithRemoteDiagnostics(true); // required by proto.actor dashboard

                protoActor.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString));
            });
        })
        .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseScheduling()
        .UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>())
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
    );

services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Configure middleware pipeline.
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

app.UseAuthentication();
app.UseAuthorization();

// Elsa API endpoints for designer.
app.UseWorkflowsApi();

// Captures unhandled exceptions and returns a JSON response.
app.UseJsonSerializationErrorHandler();

// Elsa HTTP Endpoint activities
app.UseWorkflows();

// Run.
app.Run();