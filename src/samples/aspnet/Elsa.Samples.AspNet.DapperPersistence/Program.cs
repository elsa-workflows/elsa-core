using Elsa.Dapper.Extensions;
using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    elsa.UseDapper(dapper => dapper.UseMigrations());
    
    elsa.UseWorkflowManagement(management =>
    {
        management.UseDapper();
    });

    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseDapper();
    });
    
    elsa.UseWorkflowsApi();
    elsa.UseHttp();
    elsa.UseScheduling();
    
    elsa.UseIdentity(identity =>
    {
        identity.UseDapper();
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