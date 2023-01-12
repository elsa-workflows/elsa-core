using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.Extensions;
using Elsa.Identity;
using Elsa.Requirements;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management =>
    {
        management.UseDefaultManagement(dm => dm.UseEntityFrameworkCore(ef => ef.UseSqlite()));
        management.UseWorkflowInstances(w => w.UseEntityFrameworkCore(ef => ef.UseSqlite()));
    });
    
    // Expose API endpoints.
    elsa.UseWorkflowsApi();

    // Add services for HTTP activities and workflow middleware.
    elsa.UseHttp();
    
    // Configure identity so that we can create a default admin user.
    elsa.UseIdentity(identity =>
    {
        identity.IdentityOptions.CreateDefaultAdmin = builder.Environment.IsDevelopment();
        identity.TokenOptions.SigningKey = "secret-token-signing-key";
    });
    
    // Use default authentication (JWT).
    elsa.UseDefaultAuthentication();
});

// Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();