using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
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
using FastEndpoints;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Routing;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var runtimeRoot = Path.GetFullPath(configuration["Fixture:RuntimeRoot"] ?? throw new InvalidOperationException("Missing disposable runtime root."));
var origin = configuration["Fixture:StudioOrigin"] ?? throw new InvalidOperationException("Missing Studio origin.");
var password = configuration["Fixture:Password"] ?? throw new InvalidOperationException("Missing ephemeral password.");
var passwordHash = new DefaultSecretHasher().HashSecret(password);
var permissionProfile = configuration["Fixture:PermissionProfile"];
if (permissionProfile is not ("full" or "denied" or "deny-secrets" or "deny-workflow-contexts"))
    throw new InvalidOperationException("Unknown fixture permission profile.");
var permissions = JsonSerializer.Deserialize<string[]>(configuration["Fixture:PermissionGrants"]
    ?? throw new InvalidOperationException("Missing explicit fixture permissions."));
if (permissions is not { Length: > 0 })
    throw new InvalidOperationException("Missing explicit fixture permissions.");
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
            Permissions = permissions
        }));
    });
    elsa.UseDefaultAuthentication();
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef =>
        ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "management.db")}")));
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef =>
        ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "runtime.db")}")));
    elsa.UseWorkflowsApi();
    elsa.UseJavaScript();
#if FIXTURE_BPMN
    elsa.UseBpmnInterchange();
#endif
    if (contexts)
    {
        elsa.UseWorkflowContexts();
    }
    if (secrets)
    {
        var encryptionKey = Convert.FromBase64String(configuration["Fixture:SecretsEncryptionKey"]
            ?? throw new InvalidOperationException("Missing ephemeral Secrets encryption key."));
        if (encryptionKey.Length != 32)
            throw new InvalidOperationException("Fixture Secrets encryption key must be 32 bytes.");
        elsa.UseSecrets(feature =>
        {
            feature.ConfigureOptions += options => options.EncryptionKey = encryptionKey;
            feature.UseEntityFrameworkCore(ef => ef.UseSqlite($"Data Source={Path.Combine(runtimeRoot, "secrets.db")}"));
        });
    }
});
builder.Services.Configure<SerializationTypeOptions>(options => options.RegisterTypeAlias(
    typeof(SyntheticWorkflowContextProvider), typeof(SyntheticWorkflowContextProvider).GetSimpleAssemblyQualifiedName()));
builder.Services.AddSingleton<IWorkflowContextProvider, SyntheticWorkflowContextProvider>();

var app = builder.Build();
var secretsEndpointEvidence = new SecretsEndpointEvidenceCollector();
app.UseCors();
app.UseRouting();
app.Use(async (context, next) =>
{
    var observation = SecretsEndpointObservationFactory.TryCreate(context);
    if (observation is null)
    {
        await next();
        return;
    }

    await next();
    secretsEndpointEvidence.Record(observation with { StatusCode = context.Response.StatusCode });
});
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.MapGet("/_fixture/ready", () => new
{
    schema = 1, framework = AppContext.TargetFrameworkName, runtime = RuntimeInformation.FrameworkDescription,
    auth_mode = "ElsaIdentity", permission_profile = permissionProfile, permission_grants = permissions,
    workflow_contexts_enabled = contexts, secrets_enabled = secrets,
    features = app.Services.GetRequiredService<IInstalledFeatureProvider>().List().Select(feature => feature.FullName)
});
app.MapGet("/_fixture/assemblies", () => RuntimeEvidence.LoadedAssemblies()).RequireAuthorization();
app.MapGet("/_fixture/secrets-endpoints", () => secretsEndpointEvidence.Snapshot()).RequireAuthorization();
await app.RunAsync();

sealed class SecretsEndpointEvidenceCollector
{
    private const int MaximumObservations = 64;
    private readonly object _gate = new();
    private readonly List<SecretsEndpointObservation> _observations = [];
    private bool _truncated;

    public void Record(SecretsEndpointObservation observation)
    {
        lock (_gate)
        {
            if (_observations.Count >= MaximumObservations)
            {
                _truncated = true;
                return;
            }

            _observations.Add(observation);
        }
    }

    public object Snapshot()
    {
        lock (_gate)
            return new { schema = 1, truncated = _truncated, observations = _observations.ToArray() };
    }
}

sealed record SecretsEndpointObservation(
    [property: JsonPropertyName("route")] string Route,
    [property: JsonPropertyName("verb")] string Verb,
    [property: JsonPropertyName("handler_type")] string? HandlerType,
    [property: JsonPropertyName("handler_assembly_name")] string? HandlerAssemblyName,
    [property: JsonPropertyName("handler_assembly_full_name")] string? HandlerAssemblyFullName,
    [property: JsonPropertyName("handler_assembly_sha256")] string? HandlerAssemblySha256,
    [property: JsonPropertyName("status_code")] int? StatusCode,
    [property: JsonPropertyName("failure_category")] string? FailureCategory);

static class SecretsEndpointObservationFactory
{
    private sealed record Target(string RequestPath, string DefinitionRoute, string Verb, string HandlerType);

    private static readonly Target[] Targets =
    [
        new("/elsa/api/secrets/descriptors", "/secrets/descriptors", "GET",
            "Elsa.Secrets.Endpoints.Secrets.Descriptors.Endpoint"),
        new("/elsa/api/secrets/picker", "/secrets/picker", "POST",
            "Elsa.Secrets.Endpoints.Secrets.Picker.Endpoint")
    ];

    public static SecretsEndpointObservation? TryCreate(HttpContext context)
    {
        var requestPath = NormalizePath(context.Request.Path.Value);
        var target = Targets.SingleOrDefault(candidate =>
            candidate.RequestPath == requestPath &&
            string.Equals(candidate.Verb, context.Request.Method, StringComparison.OrdinalIgnoreCase));
        if (target is null)
            return null;

        var endpoint = context.GetEndpoint();
        var definition = endpoint?.Metadata.GetMetadata<EndpointDefinition>();
        var routeEndpoint = endpoint as RouteEndpoint;
        var definitionMatches = definition is not null &&
            string.Equals(definition.EndpointType.FullName, target.HandlerType, StringComparison.Ordinal) &&
            definition.Routes.Length == 1 &&
            string.Equals(NormalizePath(definition.Routes[0]), target.DefinitionRoute, StringComparison.Ordinal) &&
            definition.Verbs.Length == 1 &&
            string.Equals(definition.Verbs[0], target.Verb, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(NormalizePath(routeEndpoint?.RoutePattern.RawText), target.RequestPath, StringComparison.Ordinal);

        if (!definitionMatches)
        {
            return new SecretsEndpointObservation(
                target.RequestPath, target.Verb, null, null, null, null, null,
                definition is null ? "endpoint_definition_missing" : "endpoint_definition_mismatch");
        }

        var assembly = RuntimeEvidence.GetLoadedElsaAssemblyIdentity(definition!.EndpointType.Assembly);
        if (assembly is null || assembly.Name != "Elsa.Secrets")
        {
            return new SecretsEndpointObservation(
                target.RequestPath, target.Verb, null, null, null, null, null,
                "handler_assembly_not_canonical");
        }

        return new SecretsEndpointObservation(
            target.RequestPath,
            target.Verb,
            definition.EndpointType.FullName,
            assembly.Name,
            assembly.FullName,
            assembly.Sha256,
            null,
            null);
    }

    private static string NormalizePath(string? path) =>
        string.IsNullOrWhiteSpace(path) ? "/" : $"/{path.Trim().Trim('/')}";
}

sealed class SyntheticWorkflowContextProvider : IWorkflowContextProvider
{
    public ValueTask<object?> LoadAsync(Elsa.Workflows.WorkflowExecutionContext context) => new((object?)"synthetic");
    public ValueTask SaveAsync(Elsa.Workflows.WorkflowExecutionContext context, object? value) => ValueTask.CompletedTask;
}
