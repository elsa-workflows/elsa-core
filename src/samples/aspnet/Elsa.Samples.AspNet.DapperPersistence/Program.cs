using Elsa.Dapper.Extensions;
using Elsa.Dapper.Modules.Management.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    elsa.UseDapperMigrations();
    
    elsa.UseWorkflowManagement(management =>
    {
        management.UseEntityFrameworkCore();
        management.UseWorkflowDefinitions(f => f.UseDapper());
    });

    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseDefaultRuntime(dr => dr.UseEntityFrameworkCore());
        runtime.UseExecutionLogRecords(e => e.UseEntityFrameworkCore());
        runtime.UseAsyncWorkflowStateExporter();
    });
    
    elsa.UseWorkflowsApi();
    elsa.UseHttp();
    elsa.UseScheduling();
    
    elsa.UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "secret-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    });
    
    elsa.UseDefaultAuthentication(auth => auth.UseAdminApiKey());
    elsa.AddActivitiesFrom<Program>();
});

builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();