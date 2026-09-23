using Elsa.Extensions;
using Elsa.Studio.Localization;
using Elsa.Studio.WorkflowContexts.Contracts;
using Elsa.Studio.WorkflowContexts.Services;
using FastEndpoints;
using MudBlazor.Services;
using UiProbe;

var builder = WebApplication.CreateBuilder(args);
if (builder.Configuration.GetSection("Kestrel:Endpoints").Exists())
{
    throw new InvalidOperationException("This disposable loopback probe does not accept configured Kestrel endpoints.");
}
builder.WebHost.UseUrls("http://127.0.0.1:6187");
builder.WebHost.UseStaticWebAssets();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.Configure<Elsa.Common.Serialization.SerializationTypeOptions>(o => o.RegisterTypeAlias(typeof(SyntheticWorkflowContextProvider), typeof(SyntheticWorkflowContextProvider).GetSimpleAssemblyQualifiedName()));
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddMudServices();
builder.Services.AddSingleton<ILocalizationProvider, ProbeLocalizations>();
builder.Services.AddScoped<ILocalizer, DefaultLocalizer>();
builder.Services.AddScoped<IWorkflowContextsProvider>(_ => new RemoteWorkflowContextsProvider(new ProbeBackendClient(new Uri("http://127.0.0.1:6187/elsa/api"))));
builder.Services.AddSingleton<Elsa.WorkflowContexts.Contracts.IWorkflowContextProvider, SyntheticWorkflowContextProvider>();
builder.Services.AddElsa(elsa => elsa.UseWorkflowContexts());
builder.Services.AddFastEndpoints(options => { options.DisableAutoDiscovery = true; options.Assemblies = [typeof(Elsa.WorkflowContexts.Features.WorkflowContextsFeature).Assembly]; });
var app = builder.Build();
app.Use(async (context, next) => {
    context.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity([new System.Security.Claims.Claim("permissions", "read:workflow-context-provider-descriptors")], "local-probe"));
    await next();
});
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.UseWorkflowsApi();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
await app.RunAsync();
sealed class ProbeLocalizations : ILocalizationProvider { public string? GetTranslation(string key) => key; }
