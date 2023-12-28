using Elsa;
using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Http.Contracts;
using Elsa.Http.Models;
using Elsa.Samples.AspNet.CustomUIHandler;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
EndpointSecurityOptions.SecurityIsEnabled = false;
// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management => { management.UseEntityFrameworkCore(ef => ef.UseSqlite()); });

    // Configure runtime feature to use EF Core.
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore();
    });

    // Expose API endpoints.
    elsa.UseWorkflowsApi(api => api.AddFastEndpointsAssembly<Program>());

    // Add services for HTTP activities and workflow middleware.
    elsa.UseHttp(configure =>
    {
        var s = configure.Services;
        //s.AddSingleton<test>();
        //configure.HttpEndpointAuthorizationHandler = (sp) => { return sp.GetRequiredService<test>(); };
    });

    // Use JavaScript and Liquid.
    elsa.UseJavaScript();
    elsa.UseLiquid();

    // Configure identity so that we can create a default admin user.
    elsa.UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "c7dc81876a782d502084763fa322429fca015941eac90ce8ca7ad95fc8752035";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    });

    // Use default authentication (JWT).
    elsa.UseDefaultAuthentication();

    elsa.AddActivity<VehiculeActivity>();

});

builder.Services.AddSingleton<VehiculeUIHandler>();
//builder.Services.AddSingleton<VehiculeOptionsProvider>();

builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseCors();
//app.UseAuthentication();
//app.UseAuthorization();
app.UseWorkflowsApi();
app.Map("/test",c=> c.UseWorkflows());
app.Run();

public class test : IHttpEndpointAuthorizationHandler
{
    public ValueTask<bool> AuthorizeAsync(AuthorizeHttpEndpointContext context)
    {
        context.HttpContext.AuthenticateAsync();
        throw new NotImplementedException();
    }
}