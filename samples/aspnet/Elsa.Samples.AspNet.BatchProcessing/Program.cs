using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Samples.AspNet.BatchProcessing.Models;

var builder = WebApplication.CreateBuilder(args);
var sqliteConnectionString = "Data Source=App_Data/elsa.db";

builder.Services.AddElsa(elsa =>
{
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));
    elsa.UseWorkflowRuntime(runtime => runtime.UseEntityFrameworkCore(ef => ef.UseSqlite(sqliteConnectionString)));
    elsa.UseWorkflowsApi();
    elsa.UseHttp();
    elsa.UseScheduling();
    elsa.UseJavaScript(javaScript => javaScript.AllowClrAccess = true);
    elsa.UseCSharp(csharp =>
    {
        csharp.Assemblies.Add(typeof(Program).Assembly);
        csharp.Namespaces.Add(typeof(Order).Namespace!);
    });
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

    elsa.AddVariableTypeAndAlias<Order>("Order", "Warehousing");
    elsa.AddVariableTypeAndAlias<IAsyncEnumerable<ICollection<Order>>>("BatchedOrderStream", "Warehousing");
});

builder.Services.AddHealthChecks();
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("*")));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseHealthChecks("/");
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();