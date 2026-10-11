using Elsa.Agents;
using Elsa.Extensions;
using Elsa.Persistence.EFCore.Modules.Management;
using Elsa.Persistence.EFCore.Modules.Runtime;
using Microsoft.AspNetCore.StaticFiles;
using static Elsa.Server.Shared.DatabaseConfiguration;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

builder.WebHost.UseStaticWebAssets();

services
    .AddElsa(elsa => elsa
        .UseIdentity(identity =>
        {
            identity.TokenOptions = options => configuration.GetSection("Identity:Tokens").Bind(options);
            identity.UseAdminUserProvider(options => configuration.GetSection("Identity:AdminUser").Bind(options));
        })
        .UseDefaultAuthentication()
        .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ConfigureEntityFrameworkCore(ef, configuration)))
        .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ConfigureEntityFrameworkCore(ef, configuration)))
        .UseWorkflowRuntimeDashboard()
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp(http => http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options))
        .UseWorkflowsApi()
        .UseDashboardApi()
        .UseAgentActivities()
        .UseAgentPersistence(persistence => persistence.UseEntityFrameworkCore(ef => ConfigureEntityFrameworkCoreForAgents(ef, configuration)))
        .UseAgentsApi()
        .AddActivitiesFrom<Program>()
        .AddWorkflowsFrom<Program>()
    );

services.AddRazorPages();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.UseRouting();
app.UseCors();

// Use Static Files instead of MapStaticAssets.
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = new FileExtensionContentTypeProvider
    {
        Mappings =
        {
            // Add custom MIME type mappings for WASM
            [".dat"] = "application/octet-stream"
        }
    }
});
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapRazorPages();
app.MapFallbackToPage("/_Host");

app.Run();
