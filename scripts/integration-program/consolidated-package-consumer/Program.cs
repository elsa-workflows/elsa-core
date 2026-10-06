using FastEndpoints;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Features.Contracts;
using Elsa.Slack.Features;
using Elsa.WorkflowContexts.Features;
using Microsoft.Extensions.DependencyInjection;

#if NET8_0
const string targetFramework = "net8.0";
#elif NET9_0
const string targetFramework = "net9.0";
#elif NET10_0
const string targetFramework = "net10.0";
#else
#error Unsupported consumer target framework.
#endif

var assemblyChecks = new Dictionary<string, string>
{
    ["Elsa"] = typeof(Elsa.Features.ElsaFeature).Assembly.GetName().Name!,
    ["Elsa.Slack"] = typeof(SlackFeature).Assembly.GetName().Name!,
    ["Elsa.WorkflowContexts"] = typeof(WorkflowContextsFeature).Assembly.GetName().Name!,
    ["Elsa.Studio.WorkflowContexts"] = typeof(Elsa.Studio.WorkflowContexts.Feature).Assembly.GetName().Name!,
    ["Elsa.Studio.Core"] = typeof(Elsa.Studio.Contracts.IBackendApiClientProvider).Assembly.GetName().Name!
};

foreach (var (packageId, assemblyName) in assemblyChecks)
{
    if (!string.Equals(packageId, assemblyName, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"{packageId} resolved assembly {assemblyName}.");
    }
}

var services = new ServiceCollection();
services.AddElsa(elsa => elsa.UseWorkflowContexts());
await using var provider = services.BuildServiceProvider();
var features = provider.GetRequiredService<IInstalledFeatureProvider>().List().Select(x => x.FullName).ToArray();
var studioFeature = typeof(Elsa.Studio.WorkflowContexts.Feature);
var expected = studioFeature.GetCustomAttribute<Elsa.Studio.Attributes.RemoteFeatureAttribute>()?.Name;
if (expected is null || !features.Contains(expected))
{
    throw new InvalidOperationException("Studio remote feature requirement does not match the backend registry.");
}

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
// The legacy context loader consumes simple assembly-qualified names. Explicitly
// register this trusted host provider; never permit arbitrary CLR type loading.
builder.Services.Configure<Elsa.Common.Serialization.SerializationTypeOptions>(options =>
    options.RegisterTypeAlias(typeof(SyntheticWorkflowContextProvider), typeof(SyntheticWorkflowContextProvider).GetSimpleAssemblyQualifiedName()));
builder.Services.AddSingleton<Elsa.WorkflowContexts.Contracts.IWorkflowContextProvider, SyntheticWorkflowContextProvider>();
builder.Services.AddElsa(elsa => elsa.UseWorkflowContexts());
builder.Services.AddFastEndpoints(options =>
{
    options.DisableAutoDiscovery = true;
    options.Assemblies = [typeof(WorkflowContextsFeature).Assembly];
});

await using var app = builder.Build();
app.Use(async (context, next) =>
{
    context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(
        [new System.Security.Claims.Claim("permissions", "read:workflow-context-provider-descriptors")], "local-probe"));
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
await app.StartAsync();
try
{
    var url = new Uri(app.Urls.Single() + "/elsa/api");
    var client = new Elsa.Studio.WorkflowContexts.Services.RemoteWorkflowContextsProvider(new ProbeBackendClient(url));
    var descriptors = (await client.ListAsync()).ToArray();
    var expectedType = typeof(SyntheticWorkflowContextProvider).GetSimpleAssemblyQualifiedName();
    if (descriptors.Length != 1 || descriptors[0].Name != "Synthetic" || descriptors[0].Type != expectedType ||
        Type.GetType(descriptors[0].Type) != typeof(SyntheticWorkflowContextProvider))
    {
        throw new InvalidOperationException("WorkflowContexts descriptor contract mismatch.");
    }

    Console.WriteLine("CONSUMER_PROOF=" + JsonSerializer.Serialize(new
    {
        framework = targetFramework,
        runtime = RuntimeInformation.FrameworkDescription,
        assemblyChecks,
        expected,
        featureMatches = true,
        httpRoundtrip = true,
        descriptorCount = descriptors.Length,
        descriptorName = descriptors[0].Name,
        descriptorType = descriptors[0].Type,
        providerTypeResolves = true,
        backendAssembly = typeof(WorkflowContextsFeature).Assembly.Location,
        studioAssembly = studioFeature.Assembly.Location
    }));
}
finally
{
    await app.StopAsync();
}
