using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.OrchardCore;
using Elsa.OrchardCore.Client;
using Elsa.Samples.AspNet.OrchardCoreIntegration;
using Elsa.SemanticKernel.Api.Extensions;
using Elsa.SemanticKernel.Options;
using WebhooksCore.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef => ef.UseSqlite());
            management.AddVariableType<ProofreaderResult>("Agents");
        })
        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite()))
        .UseWorkflowsApi()
        .UseHttp()
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseOrchardCore()
        .UseDefaultAuthentication(auth => auth.UseAdminApiKey())
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>()
        .UseIdentity(identity =>
        {
            identity.UseAdminUserProvider();
            identity.TokenOptions = options => options.SigningKey = "super-secret-tamper-free-token-signing-key";
        })
        .UseSemanticKernel()
        .UseSemanticKernelApi();
});

builder.Services.Configure<SemanticKernelOptions>(options => builder.Configuration.GetSection("SemanticKernel").Bind(options));
builder.Services.Configure<WebhookSourcesOptions>(options => builder.Configuration.GetSection("Webhooks").Bind(options));
builder.Services.Configure<OrchardCoreOptions>(options => builder.Configuration.GetSection("OrchardCore").Bind(options));
builder.Services.Configure<OrchardCoreClientOptions>(options => builder.Configuration.GetSection("OrchardCore:Client").Bind(options));
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("*")));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();