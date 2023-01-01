using Elsa.Extensions;
using Elsa.Identity;
using Elsa.Identity.Options;
using Elsa.Requirements;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var identityOptions = new IdentityTokenOptions();
var identitySection = configuration.GetSection("Elsa:Identity");
identitySection.Bind(identityOptions);

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseWorkflows()
        .UseWorkflowsApi()
        .UseIdentity(identity =>
        {
            identity.IdentityOptions.CreateDefaultAdmin = true;
            identity.TokenOptions = identityOptions;
        })
        .UseDefaultAuthentication()
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