using Elsa;
using Elsa.Extensions;
using Elsa.Http.Handlers;
using Elsa.JavaScript.Options;
using Elsa.WorkflowServer.Web;

EndpointSecurityOptions.DisableSecurity();

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
        .UseFluentStorageProvider()
        .AddTypeAlias<ApiResponse<User>>("ApiResponse[User]")
        .UseIdentity(identity =>
        {
            identity.UseMongoDb();
            identity.IdentityOptions = options => identitySection.Bind(options);
            identity.TokenOptions = options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication()
        .UseWorkflowManagement(management =>
        {
            management.UseMongoDb();
            management.AddVariableType<ApiResponse<User>>("Api");
            management.AddVariableType<User>("Api");
            management.AddVariableType<Support>("Api");
        })
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseMongoDb()
            runtime.UseDefaultRuntime(dr => dr.UseMongoDb());
            runtime.UseExecutionLogRecords(e => e.UseMongoDb());
            runtime.UseAsyncWorkflowStateExporter();
            runtime.UseMassTransitDispatcher();
        })
        .UseEnvironments(environments => environments.EnvironmentsOptions = options => configuration.GetSection("Environments").Bind(options))
        .UseScheduling()
        .UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>())
        .UseJavaScript()
        .UseLiquid()
        .UseLabels(options => options.UseMongoDb())
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

// Run.
app.Run();