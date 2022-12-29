using Elsa.Extensions;
using Elsa.Jobs.Extensions;
using Elsa.Http;
using Elsa.Http.Extensions;
using Elsa.Identity;
using Elsa.Identity.Extensions;
using Elsa.Identity.Features;
using Elsa.Identity.Options;
using Elsa.JavaScript.Activities;
using Elsa.JavaScript.Extensions;
using Elsa.Labels.Extensions;
using Elsa.Scheduling.Activities;
using Elsa.Liquid.Extensions;
using Elsa.Persistence.EntityFrameworkCore.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.Modules.Labels;
using Elsa.Persistence.EntityFrameworkCore.Modules.Runtime;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.ActivityDefinitions;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Labels;
using Elsa.Persistence.EntityFrameworkCore.Sqlite.Modules.Runtime;
using Elsa.Requirements;
using Elsa.Scheduling.Extensions;
using Elsa.Workflows.Api.Endpoints.WorkflowDefinitions.Post;
using Elsa.Workflows.Api.Features;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Management.Extensions;
using Elsa.Workflows.Runtime.Extensions;
using FastEndpoints;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite");
var identityOptions = new IdentityOptions();
var identitySection = configuration.GetSection("Identity");
identitySection.Bind(identityOptions);

// Add Elsa services.
services
    .AddElsa(elsa => elsa
        .UseWorkflows()
        .UseWorkflowRuntime()
        .UseWorkflowManagement(management => management
            .AddActivitiesFrom<WriteLine>()
            .AddActivitiesFrom<HttpEndpoint>()
            .AddActivitiesFrom<Delay>()
            .AddActivitiesFrom<RunJavaScript>()
        )
        .UseWorkflowApiEndpoints()
        .UseIdentity(identity =>
        {
            identity.CreateDefaultUser = true;
            identity.IdentityOptions = options => identitySection.Bind(options);
        })
        .UseWorkflowRuntime(runtime => { runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)); })
        .UseLabels(labels => labels.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseActivityDefinitions(feature => feature.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
        .UseJobs()
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
        .UseActivityDefinitions()
    );

services.AddHealthChecks();

// Authentication & Authorization.
services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, identityOptions.ConfigureJwtBearerOptions);

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
app.UseElsaFastEndpoints();
app.UseWorkflows();

app.UseEndpoints(endpoints =>
{
    endpoints.MapFallbackToPage("/Index");
});

app.MapRazorPages();
app.Run();