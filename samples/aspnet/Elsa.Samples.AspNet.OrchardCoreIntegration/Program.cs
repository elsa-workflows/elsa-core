using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using WebhooksCore.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore())
        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore())
        .UseWorkflowsApi()
        .UseHttp()
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseOrchardWebhooks()
        .UseDefaultAuthentication(auth => auth.UseAdminApiKey())
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>()
        .UseIdentity(identity =>
        {
            identity.UseAdminUserProvider();
            identity.TokenOptions = options => options.SigningKey = "super-secret-tamper-free-token-signing-key";
        });
});

builder.Services.Configure<WebhookSourcesOptions>(options => builder.Configuration.GetSection("Webhooks").Bind(options));
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("*")));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();