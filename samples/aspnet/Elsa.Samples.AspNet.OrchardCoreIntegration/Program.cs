using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.OrchardCore;
using Elsa.OrchardCore.Client;
using Elsa.Samples.AspNet.OrchardCoreIntegration;
using Elsa.Agents.Options;
using Elsa.Agents.Persistence;
using Elsa.EntityFrameworkCore.Modules.Identity;
using Elsa.Workflows.Management;
using WebhooksCore.Options;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(management =>
        {
            management.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            management.AddVariableTypeAndAlias<TranslationResult>("Agents");
            management.AddVariableTypeAndAlias<FactCheckResult>("Agents");
            management.AddVariableTypeAndAlias<GenerateTagsResult>("Agents");
            management.AddVariableTypeAndAlias<TitleResult>("Agents");
        })
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
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
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>()
        .UseIdentity(identity =>
        {
            identity.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString));
            identity.TokenOptions = options => identityTokenSection.Bind(options);
            identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
            identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
        })
        .UseDefaultAuthentication()
        .UseAgentPersistence(persistence => persistence.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseAgentActivities()
        .UseAgentsApi()
        ;
});

builder.Services.AddControllers();
builder.Services.Configure<AgentsOptions>(options => builder.Configuration.GetSection("Agents").Bind(options));
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