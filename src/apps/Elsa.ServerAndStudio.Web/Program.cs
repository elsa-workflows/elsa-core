using Elsa.Extensions;
using Elsa.ServerAndStudio.Web.Enums;
using Elsa.ServerAndStudio.Web.Extensions;
using Medallion.Threading.FileSystem;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var services = builder.Services;
var configuration = builder.Configuration;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

// Add Elsa services.
services
    .AddElsa(elsa =>
    {
        elsa
            .UseSasTokens()
            .UseIdentity(identity =>
            {
                identity.TokenOptions = options => identityTokenSection.Bind(options);
                identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
                identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
            })
            .UseDefaultAuthentication()
            .UseWorkflowManagement(management => management.UseCache())
            .UseWorkflowRuntime(runtime =>
            {
                runtime.UseCache();
                runtime.DistributedLockProvider = _ => new FileDistributedSynchronizationProvider(new DirectoryInfo(Path.Combine(Directory.GetCurrentDirectory(), "App_Data", "locks")));
                runtime.WorkflowInboxCleanupOptions = options => configuration.GetSection("Runtime:WorkflowInboxCleanup").Bind(options);
                runtime.WorkflowDispatcherOptions = options => configuration.GetSection("Runtime:WorkflowDispatcher").Bind(options);
            })
            .UseJavaScript(javaScriptFeature => javaScriptFeature.ConfigureJintOptions(jintOptions => jintOptions.AllowClrAccess = true))
            .UseLiquid()
            .UseCSharp()
            .UsePython(python =>
            {
                python.PythonOptions += options =>
                {
                    // Make sure to configure the path to the python DLL. E.g. /opt/homebrew/Cellar/python@3.11/3.11.6_1/Frameworks/Python.framework/Versions/3.11/bin/python3.11
                    // alternatively, you can set the PYTHONNET_PYDLL environment variable.
                    configuration.GetSection("Scripting:Python").Bind(options);
                };
            })
            .UseHttp(http =>
            {
                http.UseCache();
                http.ConfigureHttpOptions = options =>
                {
                    options.BaseUrl = new Uri(configuration["Hosting:BaseUrl"] ?? configuration["Http:BaseUrl"]!); // HttpBaseUrl is for backward compatibility.
                    options.BasePath = configuration["Http:BasePath"];
                };
            })
            .UseWorkflowsApi()
            .AddActivitiesFrom<Program>()
            .AddWorkflowsFrom<Program>();
    });

services.AddHealthChecks();
services.AddCors(cors => cors.Configure(configuration.GetSection("CorsPolicy")));

// Razor Pages.
services.AddRazorPages(options => options.Conventions.ConfigureFilter(new IgnoreAntiforgeryTokenAttribute()));

// Configure middleware pipeline.
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseBlazorFrameworkFiles();
app.MapHealthChecks("/health");
app.UseRouting();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapFallbackToPage("/_Host");
await app.RunAsync();