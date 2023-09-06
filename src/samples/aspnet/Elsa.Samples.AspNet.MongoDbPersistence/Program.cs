using Elsa.Extensions;
using Elsa.MongoDb.Extensions;
using Elsa.MongoDb.Modules.Identity;
using Elsa.MongoDb.Modules.Management;
using Elsa.MongoDb.Modules.Runtime;

var builder = WebApplication.CreateBuilder(args);
var mongoDbConnectionString = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017/elsa";

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    elsa.UseMongoDb(mongoDbConnectionString);
    
    elsa.UseWorkflowManagement(management =>
    {
        management.UseMongoDb();
    });

    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseMongoDb();
    });
    
    elsa.UseWorkflowsApi();
    elsa.UseHttp();
    elsa.UseScheduling();
    
    elsa.UseIdentity(identity =>
    {
        identity.UseMongoDb();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "secret-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    });
    
    elsa.UseDefaultAuthentication();
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