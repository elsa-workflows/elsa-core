using System.Text.Encodings.Web;
using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows.Api;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

// ReSharper disable RedundantAssignment
var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

// Add Elsa services.
services
    .AddElsa(elsa =>
    {
        elsa
            .AddActivitiesFrom<Elsa.TestServer.Web.Program>()
            .AddWorkflowsFrom<Elsa.TestServer.Web.Program>()
            .UseIdentity(identity =>
            {
                identity.TokenOptions = options => identityTokenSection.Bind(options);
                identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
            })
            .UseDefaultAuthentication()
            .UseWorkflowManagement()
            .UseWorkflowRuntime()
            .UseWorkflowsApi(api =>
            {
                api.AddFastEndpointsAssembly<Elsa.TestServer.Web.Program>();
            })
            .UseJavaScript(options =>
            {
                options.AllowClrAccess = true;
            })
            .UseLiquid(liquid => liquid.FluidOptions = options => options.Encoder = HtmlEncoder.Default)
            .UseHttp(http =>
            {
                http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options);
                http.UseCache();
            });

        Elsa.TestServer.Web.Program.ConfigureForTest?.Invoke(elsa);
    });

services.AddHealthChecks();
services.AddControllers();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin().WithExposedHeaders("*")));

// Build the web application.
var app = builder.Build();

// Configure the pipeline.
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
var routePrefix = app.Services.GetRequiredService<IOptions<ApiEndpointOptions>>().Value.RoutePrefix;
app.UseWorkflowsApi(routePrefix);

// Captures unhandled exceptions and returns a JSON response.
app.UseJsonSerializationErrorHandler();

// Elsa HTTP Endpoint activities.
app.UseWorkflows();

app.MapControllers();

// Swagger API documentation.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerUI();
}

// Run.
await app.RunAsync();

namespace Elsa.TestServer.Web
{
    /// <summary>
    /// The main entry point for the application made public for end to end testing.
    /// </summary>
    [UsedImplicitly]
    public partial class Program
    {
        /// <summary>
        /// Set by the test runner to configure the module for testing.
        /// </summary>
        public static Action<IModule>? ConfigureForTest { get; set; }
    }
}