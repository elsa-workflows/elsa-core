using Elsa;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Http.Handlers;
using Elsa.JavaScript.Options;
using Elsa.MongoDb.Extensions;
using Elsa.WorkflowServer.Web;
using Microsoft.Data.Sqlite;
using Proto.Persistence.Sqlite;

EndpointSecurityOptions.DisableSecurity();

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var sqliteConnectionString = configuration.GetConnectionString("Sqlite");

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .AddActivitiesFrom<Program>()
        .UseFluentStorageProvider()
        .AddTypeAlias<ApiResponse<User>>("ApiResponse[User]")
        .UseIdentity(identity =>
        {
            identity.UseEntityFrameworkCore();
            identity.IdentityOptions = options => identitySection.Bind(options);
            identity.TokenOptions = options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication()
        .UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore();
            management.AddVariableType<ApiResponse<User>>("Api");
            management.AddVariableType<User>("Api");
            management.AddVariableType<Support>("Api");
        })
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseEntityFrameworkCore();
            //runtime.UseDefaultRuntime(dr => dr.UseEntityFrameworkCore());
            runtime.UseProtoActor(proto => proto.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString)));
            runtime.UseExecutionLogRecords(e => e.UseEntityFrameworkCore());
            runtime.UseMassTransitDispatcher();
        })
        .UseEnvironments(environments => environments.EnvironmentsOptions = options => configuration.GetSection("Environments").Bind(options))
        .UseScheduling()
        .UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>())
        .UseRealTimeWorkflows()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp(http => http.HttpEndpointAuthorizationHandler = sp => sp.GetRequiredService<AllowAnonymousHttpEndpointAuthorizationHandler>())
        .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
        .UseMongoDb(configuration.GetConnectionString("MongoDb")!, options => configuration.GetSection("MongoDb").Bind(options))
    );

services.Configure<JintOptions>(options => options.AllowClrAccess = true);
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

// SignalR.
app.UseRouting();
app.UseWorkflowsSignalRHubs();

// Run.
app.Run();