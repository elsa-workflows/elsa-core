using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Extensions;
using Elsa.Identity;
using Elsa.Identity.Options;
using Elsa.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.EntityFrameworkCore.Modules.Labels;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Requirements;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var identityOptions = new IdentityOptions();
var identityTokenOptions = new IdentityTokenOptions();
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");
identitySection.Bind(identityOptions);
identityTokenSection.Bind(identityTokenOptions);

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseWorkflows()
        .UseWorkflowsApi()
        .UseIdentity(identity =>
        {
            identity.IdentityOptions = identityOptions;
            identity.TokenOptions = identityTokenOptions;
        })
        .UseDefaultAuthentication()
        .UseWorkflowRuntime(runtime => { runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)); })
        .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseActivityDefinitions(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseJobs()
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
    );

services.AddHealthChecks();
services.AddHttpContextAccessor();
services.AddSingleton<IAuthorizationHandler, LocalHostRequirementHandler>();
services.AddAuthorization(options => options.AddPolicy(IdentityPolicyNames.SecurityRoot, policy => policy.AddRequirements(new LocalHostRequirement())));

// Razor Pages.
services.AddRazorPages();

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
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();

app.UseEndpoints(endpoints =>
{
    endpoints.MapFallbackToPage("/Index");
});

app.MapRazorPages();
app.Run();