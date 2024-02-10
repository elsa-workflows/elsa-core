using Elsa.ServerAndStudio.Web.Extensions;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Microsoft.AspNetCore.Mvc;

const bool useMassTransit = true;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
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
              identity.IdentityOptions = options => identitySection.Bind(options);
              identity.TokenOptions = options => identityTokenSection.Bind(options);
              identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
              identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
              identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
          })
          .UseDefaultAuthentication()
          .UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
          .UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)))
          .UseScheduling()
          .UseJavaScript(options => options.AllowClrAccess = true)
          .UseLiquid()
          .UseCSharp()
          .UsePython()
          .UseHttp(http => http.ConfigureHttpOptions = options => configuration.GetSection("Http").Bind(options))
          .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
          .UseWebhooks(webhooks => webhooks.WebhookOptions = options => builder.Configuration.GetSection("Webhooks").Bind(options))
          .UseWorkflowsApi()
          .UseRealTimeWorkflows()
          .AddActivitiesFrom<Program>()
          .AddWorkflowsFrom<Program>();

        if (useMassTransit)
        {
            elsa.UseMassTransit();
        }
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
app.UseWorkflowsSignalRHubs();
app.MapFallbackToPage("/_Host");
app.Run();