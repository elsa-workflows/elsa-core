using Elsa.Elasticsearch.Extensions;
using Elsa.Elasticsearch.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration; 

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management =>
    {
        management.UseWorkflowDefinitions(d => d.UseEntityFrameworkCore());
        management.UseWorkflowInstances(i => i.UseElasticsearch());
    });
    
    // Configure Elasticsearch.
    elsa.UseElasticsearch(options => configuration.GetSection("Elasticsearch").Bind(options));
    
    // Configure runtime feature to use EF Core.
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore();
    });
    
    // Expose API endpoints.
    elsa.UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>());

    // Add services for HTTP activities and workflow middleware.
    elsa.UseHttp();
    
    // Use JavaScript and Liquid.
    elsa.UseJavaScript();
    elsa.UseLiquid();
    
    // Configure identity so that we can create a default admin user.
    elsa.UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "secret-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    });
    
    // Use default authentication (JWT).
    elsa.UseDefaultAuthentication();
});

builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

// Add Razor pages.
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseCors();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.MapRazorPages();
app.Run();