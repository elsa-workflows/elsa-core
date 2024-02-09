using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore());
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore());
    elsa.UseWorkflowsApi();
    elsa.UseHttp();
    elsa.UseScheduling();
    elsa.UseJavaScript(javaScript => javaScript.AllowClrAccess = true);
    elsa.UseCSharp();
    elsa.UseLiquid();

    elsa.UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "super-secret-tamper-free-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    });

    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());
    elsa.AddActivitiesFrom<Program>();
    elsa.AddWorkflowsFrom<Program>();
});

builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("*")));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();