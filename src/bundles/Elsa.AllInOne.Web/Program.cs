using Elsa.EntityFrameworkCore.Extensions;
using Elsa.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Webhooks.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();
var services = builder.Services;
var configuration = builder.Configuration;
var sqliteConnectionString = configuration.GetConnectionString("Sqlite")!;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

// Add Elsa services.
services
    .AddElsa(elsa => elsa
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
        .UseWorkflowRuntime(runtime =>
        {
            runtime.UseDefaultRuntime(dr => dr.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));
            runtime.UseExecutionLogRecords(e => e.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));
            runtime.UseAsyncWorkflowStateExporter();
        })
        .UseScheduling()
        .UseJavaScript()
        .UseLiquid()
        .UseHttp()
        .UseEmail(email => email.ConfigureOptions = options => configuration.GetSection("Smtp").Bind(options))
        .UseWebhooks(webhooks => webhooks.WebhookOptions = options => builder.Configuration.GetSection("Webhooks").Bind(options))
        .UseWorkflowsApi()
    );

services.AddHealthChecks();

static bool AllowAny(IList<string> Values) => Values.Count == 0 || Values[0] == "*";
services.AddCors(cors => {
    IConfigurationSection corsPolicyConfiguration = configuration.GetSection("CorsPolicy");
    CorsPolicy corsPolicy = corsPolicyConfiguration.Get<CorsPolicy>() ?? new CorsPolicy();
    cors.AddDefaultPolicy(policy => {
        var headers = corsPolicy.Headers;
        var origins = corsPolicy.Origins;
        var methods = corsPolicy.Methods;

        if (AllowAny(headers))
            policy.AllowAnyHeader();
        else
            policy.WithHeaders(headers.ToArray());

        if (AllowAny(origins))
            policy.AllowAnyOrigin();
        else
            policy.WithOrigins(origins.ToArray());

        if (AllowAny(methods))
            policy.AllowAnyMethod();
        else
            policy.WithMethods(methods.ToArray());
    });
});

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
app.MapHealthChecks("/health");
app.UseCors();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapRazorPages();
app.Run();