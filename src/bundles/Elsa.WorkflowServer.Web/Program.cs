using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Http.Handlers;
using Elsa.JavaScript.Options;
using Elsa.MongoDb.Extensions;
using Elsa.MongoDb.Modules.Identity;
using Elsa.MongoDb.Modules.Management;
using Elsa.MongoDb.Modules.Runtime;
using Elsa.WorkflowServer.Web;
using Microsoft.Data.Sqlite;
using Proto.Persistence.Sqlite;

const bool useMongoDb = false;
const bool useProtoActor = false;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
var sqliteConnectionString = configuration.GetConnectionString("Sqlite");
var mongoDbConnectionString = configuration.GetConnectionString("MongoDb")!;

// Add Elsa services.
services
    .AddElsa(elsa =>
    {
        if(useMongoDb)
            elsa.UseMongoDb(mongoDbConnectionString);
        
        elsa
            .AddActivitiesFrom<Program>()
            .AddWorkflowsFrom<Program>()
            .UseFluentStorageProvider()
            .AddTypeAlias<ApiResponse<User>>("ApiResponse[User]")
            .UseIdentity(identity =>
            {
                if(useMongoDb)
                    identity.UseMongoDb();
                else
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
                if(useMongoDb)
                    management.UseMongoDb();
                else
                    management.UseEntityFrameworkCore();
                
                management.AddVariableType<ApiResponse<User>>("Api");
                management.AddVariableType<User>("Api");
                management.AddVariableType<Support>("Api");
            })
            .UseWorkflowRuntime(runtime =>
            {
                if(useMongoDb)
                    runtime.UseMongoDb();
                else
                    runtime.UseEntityFrameworkCore();
                
                if(useProtoActor)
                    runtime.UseProtoActor(proto => proto.PersistenceProvider = _ => new SqliteProvider(new SqliteConnectionStringBuilder(sqliteConnectionString)));
                else
                    runtime.UseDefaultRuntime(dr =>
                    {
                        if(useMongoDb)
                            dr.UseMongoDb();
                        else
                            dr.UseEntityFrameworkCore();
                    });
                
                runtime.UseExecutionLogRecords(e =>
                {
                    if(useMongoDb)
                        e.UseMongoDb();
                    else
                        e.UseEntityFrameworkCore();
                });
                runtime.UseMassTransitDispatcher();
            })
            .UseEnvironments(environments => environments.EnvironmentsOptions = options => configuration.GetSection("Environments").Bind(options))
            .UseScheduling()
            .UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>())
            .UseRealTimeWorkflows()
            .UseJavaScript()
            .UseLiquid()
            .UseHttp(http => http.HttpEndpointAuthorizationHandler = sp => sp.GetRequiredService<AllowAnonymousHttpEndpointAuthorizationHandler>())
            .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options));
    });

services.Configure<JintOptions>(options => options.AllowClrAccess = true);
services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));

// Configure middleware pipeline.
var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();

// CORS.
app.UseCors();

// Health checks.
app.MapHealthChecks("/");

// Routing used for SignalR.
app.UseRouting();

// Security.
app.UseAuthentication();
app.UseAuthorization();

// Elsa API endpoints for designer.
app.UseWorkflowsApi();

// Captures unhandled exceptions and returns a JSON response.
app.UseJsonSerializationErrorHandler();

// Elsa HTTP Endpoint activities
app.UseWorkflows();

// SignalR.
app.UseWorkflowsSignalRHubs();

// Run.
app.Run();