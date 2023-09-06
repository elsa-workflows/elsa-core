using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Samples.AspNet.WorkflowContexts.Contracts;
using Elsa.Samples.AspNet.WorkflowContexts.Providers;
using Elsa.Samples.AspNet.WorkflowContexts.Services;
using Elsa.Samples.AspNet.WorkflowContexts.Workflows;
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
        .UseWorkflows(workflows =>
        {
            // Configure workflow execution pipeline to handle workflow contexts.
            workflows.WithWorkflowExecutionPipeline(pipeline => pipeline
                .Reset()
                .UsePersistentVariables()
                .UseBookmarkPersistence()
                .UseWorkflowExecutionLogPersistence()
                .UseWorkflowContexts()
                .UseDefaultActivityScheduler()
            );
            
            // Configure activity execution pipeline to handle workflow contexts.
            workflows.WithActivityExecutionPipeline(pipeline => pipeline
                .Reset()
                .UseWorkflowContexts()
                .UseBackgroundActivityInvoker()
            );
        })
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
        })
        .UseWorkflowContexts()
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
services.AddWorkflowContextProvider<OrderWorkflowContextProvider>();

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