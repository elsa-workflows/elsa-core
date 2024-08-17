using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.OrchardCore;
using Elsa.OrchardCore.Client;
using Elsa.Samples.AspNet.OrchardCoreIntegration;
using Elsa.Agents.Options;
using Elsa.Workflows.Management;
using WebhooksCore.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef => ef.UseSqlite());
            management.AddVariableTypeAndAlias<TranslationResult>("Agents");
            management.AddVariableTypeAndAlias<FactCheckResult>("Agents");
            management.AddVariableTypeAndAlias<GenerateTagsResult>("Agents");
            management.AddVariableTypeAndAlias<TitleResult>("Agents");
        })
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite());
            runtime.UseProtoActor();
        })
        .UseWorkflowsApi()
        .UseProtoActor()
        .UseHttp()
        .UseScheduling()
        .UseWorkflowContexts()
        .UseJavaScript()
        .UseCSharp()
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
        .UseAgentActivities()
        ;
});

builder.Services.AddControllers();
builder.Services.Configure<AgentsOptions>(options => builder.Configuration.GetSection("SemanticKernel").Bind(options));
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