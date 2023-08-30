using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Http;
using Elsa.Workflows.Core.Activities;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Add Elsa services.
services.AddElsa(elsa => elsa
    // Add the Fluent Storage workflow definition provider.
    .UseFluentStorageProvider()

    // Enable the Elsa DSL.
    .UseWorkflowManagement(management =>
    {
        management.UseEntityFrameworkCore();
        management.UseDslIntegration(dsl =>
        {
            dsl.MapActivityFunction("print", nameof(WriteLine), new[] { nameof(WriteLine.Text) });
            dsl.MapActivityFunction("http_listen", nameof(HttpEndpoint), new[] { nameof(HttpEndpoint.Path), nameof(HttpEndpoint.SupportedMethods) }, activity => activity.SetCanStartWorkflow(true));
            dsl.MapActivityFunction("http_write", nameof(WriteHttpResponse), new[] { nameof(WriteHttpResponse.StatusCode), nameof(WriteHttpResponse.Content) });
        });
    })
    .UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore();
    })

    // Expose API endpoints.
    .UseWorkflowsApi()

    // Configure identity so that we can create a default admin user.
    .UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "secret-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    })

    // Use default authentication (JWT).
    .UseDefaultAuthentication(auth => auth.UseAdminApiKey())

    // Use HTTP activities.
    .UseHttp()
);


// Configure middleware pipeline.
var app = builder.Build();
// Configure the HTTP request pipeline.
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();