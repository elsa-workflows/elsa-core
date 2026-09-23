using FastEndpoints;
using System.Reflection;
using System.Text.Json;
using Elsa.Extensions;
using Elsa.Features.Contracts;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddElsa(elsa => elsa.UseWorkflowContexts());
await using var provider = services.BuildServiceProvider();
var features = provider.GetRequiredService<IInstalledFeatureProvider>().List().Select(x => x.FullName).ToArray();
var studioFeature = typeof(Elsa.Studio.WorkflowContexts.Feature);
var expected = studioFeature.GetCustomAttribute<Elsa.Studio.Attributes.RemoteFeatureAttribute>()?.Name;

if (!features.Contains(expected)) throw new InvalidOperationException("Studio remote feature requirement does not match the backend registry.");

var builder = WebApplication.CreateBuilder();
builder.WebHost.UseUrls("http://127.0.0.1:0");
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddSingleton<Elsa.WorkflowContexts.Contracts.IWorkflowContextProvider, SyntheticWorkflowContextProvider>();
builder.Services.AddElsa(elsa => elsa.UseWorkflowContexts());
builder.Services.AddFastEndpoints(options => { options.DisableAutoDiscovery = true; options.Assemblies = [typeof(Elsa.WorkflowContexts.Features.WorkflowContextsFeature).Assembly]; });
await using var app = builder.Build();
app.Use(async (context, next) => {
    context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("permissions", "read:workflow-context-provider-descriptors")], "local-probe"));
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
await app.StartAsync();
try {
    var url = new Uri(app.Urls.Single() + "/elsa/api");
    var client = new Elsa.Studio.WorkflowContexts.Services.RemoteWorkflowContextsProvider(new ProbeBackendClient(url));
    var descriptors = (await client.ListAsync()).ToArray();
    if (descriptors.Length != 1 || descriptors[0].Name != "Synthetic") throw new InvalidOperationException("Descriptor contract mismatch");
    Console.WriteLine("PAIR_PROOF=" + JsonSerializer.Serialize(new { expected, features, featureMatches = features.Contains(expected), httpRoundtrip = true, descriptorCount = descriptors.Length, descriptorName = descriptors[0].Name, backendAssembly = typeof(Elsa.WorkflowContexts.Features.WorkflowContextsFeature).Assembly.Location, studioAssembly = studioFeature.Assembly.Location, interactiveDebuggingVerified = false }));
} finally { await app.StopAsync(); }
