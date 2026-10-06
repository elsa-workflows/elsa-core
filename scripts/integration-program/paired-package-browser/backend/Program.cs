using System.Runtime.InteropServices;
using Elsa.Common.Serialization;
using Elsa.Extensions;
using Elsa.Features.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Services;
using Elsa.Persistence.EFCore.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Elsa.Secrets.Extensions;
using Elsa.Secrets.Persistence.EFCore.Extensions;
using Elsa.Secrets.Persistence.EFCore.Sqlite.Extensions;
using Elsa.WorkflowContexts.Contracts;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var runtimeRoot = Path.GetFullPath(configuration["Fixture:RuntimeRoot"] ?? throw new InvalidOperationException("Missing disposable runtime root."));
var origin = configuration["Fixture:StudioOrigin"] ?? throw new InvalidOperationException("Missing Studio origin.");
var password = configuration["Fixture:Password"] ?? throw new InvalidOperationException("Missing ephemeral password.");
var passwordHash = new DefaultSecretHasher().HashSecret(password);
var denied = configuration["Fixture:PermissionProfile"] == "denied";
var contexts = configuration.GetValue<bool>("Fixture:WorkflowContexts");
var secrets = configuration.GetValue<bool>("Fixture:Secrets");

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(origin).AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("*")));
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(runtimeRoot, "keys")));
builder.Services.AddElsa(elsa =>
{
    elsa.UseIdentity(identity =>
    {
        identity.TokenOptions += options => configuration.GetSection("Identity:Tokens").Bind(options);
        identity.UseConfigurationBasedUserProvider(options => options.Users.Add(new User
        {
            Id = "paired-browser-user", Name = "paired-browser", TenantId = "",
            HashedPassword = passwordHash.EncodeSecret(), HashedPasswordSalt = passwordHash.EncodeSalt(), Roles = ["paired-browser"]
        }));
        identity.UseConfigurationBasedRoleProvider(options => options.Roles.Add(new Role
        {
            Id = "paired-browser", Name = "Paired browser fixture", TenantId = "",
            Permissions = denied
                ? ["read:workflow-definitions", "read:workflow-instances", "read:activity-descriptors", "read:workflow-context-provider-descriptors"]
                : ["*"]
        }));
    });
    elsa.UseDefaultAuthentication();
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef =>
        ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "management.db")}")));
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef =>
        ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "runtime.db")}")));
    elsa.UseWorkflowsApi();
    elsa.UseJavaScript();
    elsa.UseBpmnInterchange();
    if (contexts)
    {
        elsa.UseWorkflowContexts();
    }
    if (secrets)
    {
        elsa.UseSecrets(feature => feature.UseEntityFrameworkCore(ef =>
            ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "secrets.db")}")));
    }
});
builder.Services.Configure<SerializationTypeOptions>(options => options.RegisterTypeAlias(
    typeof(SyntheticWorkflowContextProvider), typeof(SyntheticWorkflowContextProvider).GetSimpleAssemblyQualifiedName()));
builder.Services.AddSingleton<IWorkflowContextProvider, SyntheticWorkflowContextProvider>();

var app = builder.Build();
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.MapGet("/_fixture/ready", () => new
{
    schema = 1, framework = AppContext.TargetFrameworkName, runtime = RuntimeInformation.FrameworkDescription,
    auth_mode = "ElsaIdentity", permission_profile = denied ? "denied" : "full",
    features = app.Services.GetRequiredService<IInstalledFeatureProvider>().List().Select(feature => feature.FullName)
});
app.MapGet("/_fixture/assemblies", () => RuntimeEvidence.LoadedAssemblies()).RequireAuthorization();
await app.RunAsync();

sealed class SyntheticWorkflowContextProvider : IWorkflowContextProvider
{
    public ValueTask<object?> LoadAsync(Elsa.Workflows.WorkflowExecutionContext context) => new((object?)"synthetic");
    public ValueTask SaveAsync(Elsa.Workflows.WorkflowExecutionContext context, object? value) => ValueTask.CompletedTask;
}
