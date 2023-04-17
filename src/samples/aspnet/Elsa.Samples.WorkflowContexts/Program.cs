using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Samples.WorkflowContexts.Contracts;
using Elsa.Samples.WorkflowContexts.Providers;
using Elsa.Samples.WorkflowContexts.Services;
using Elsa.Samples.WorkflowContexts.Workflows;
using Elsa.Workflows.Core.Middleware.Workflows;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .AddActivitiesFrom<Program>()
        .AddWorkflow<CustomerCommunicationsWorkflow>()
        .UseIdentity(identity =>
        {
            identity.IdentityOptions = options => identitySection.Bind(options);
            identity.TokenOptions = options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication()
        .UseWorkflows(workflows => workflows.WithWorkflowExecutionPipeline(pipeline => pipeline
            .Reset()
            .UsePersistentVariables()
            .UseBookmarkPersistence()
            .UseWorkflowExecutionLogPersistence()
            .UseWorkflowStatePersistence()
            .UseWorkflowContexts()
            .UseDefaultActivityScheduler()
        ))
        .UseWorkflowManagement(management =>
        {
            // Use EF core for workflow definitions and instances.
            management.UseWorkflowInstances(m => m.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));
            management.UseEntityFrameworkCore(m => m.UseSqlite(sqliteConnectionString));
        })
        .UseWorkflowRuntime(runtime =>
        {
            // Use EF core for triggers and bookmarks.
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));

            // Use EF core for execution log records.
            runtime.UseExecutionLogRecords(log => log.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));

            // Use the default workflow runtime with EF core.
            runtime.UseDefaultRuntime(defaultRuntime => defaultRuntime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));

            // Install a workflow state exporter to capture workflow states and store them in IWorkflowInstanceStore.
            runtime.UseAsyncWorkflowStateExporter();
        })
        .UseScheduling()
        .UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>())
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
        .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
    );

// Add health checks.
services.AddHealthChecks();

// Add CORS.
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Add domain services.
services.AddSingleton<ICustomerStore, MemoryCustomerStore>();

// Add workflow context providers.
services.AddWorkflowContextProvider<CustomerWorkflowContextProvider>();

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