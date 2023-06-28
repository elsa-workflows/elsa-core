using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Samples.AspNet.Onboarding.WorkflowServer.Workflows;
using Elsa.Webhooks.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Add workflow.
    elsa.AddWorkflow<OnboardingWorkflow>();

    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore());

    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore();
        runtime.UseDefaultRuntime(dr => dr.UseEntityFrameworkCore());
        runtime.UseExecutionLogRecords(e => e.UseEntityFrameworkCore());
    });

    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions = options =>
        {
            options.SigningKey = "secret-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    });

    // Expose API endpoints.
    elsa.UseWorkflowsApi();

    // Use default authentication (JWT + ApiKey).
    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());

    elsa.UseWebhooks(webhooks => webhooks.WebhookOptions = options => builder.Configuration.GetSection("Webhooks").Bind(options));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.Run();