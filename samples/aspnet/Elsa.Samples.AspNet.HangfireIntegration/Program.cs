using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var identitySection = configuration.GetSection("Identity");
var identityTokenSection = identitySection.GetSection("Tokens");

// Add Elsa to the container.
builder.Services.AddElsa(elsa =>
{
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management => management.UseEntityFrameworkCore());

    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseEntityFrameworkCore();
        
        // Use Hangfire to schedule background activities.
        runtime.UseHangfireBackgroundActivityScheduler();
    });
    
    // Expose API endpoints.
    elsa.UseWorkflowsApi();

    // Use Hangfire.
    elsa.UseHangfire(hangfire => hangfire.UseSqliteStorage(sqlite => sqlite.NameOrConnectionString = "elsa.sqlite.db"));
    
    // Use hangfire for scheduling timer events.
    elsa.UseScheduling(scheduling => scheduling.UseHangfireScheduler());
    
    // Configure identity.
    elsa.UseIdentity(identity =>
    {
        identity.IdentityOptions = options => identitySection.Bind(options);
        identity.TokenOptions = options => identityTokenSection.Bind(options);
        identity.UseConfigurationBasedUserProvider(options => identitySection.Bind(options));
        identity.UseConfigurationBasedApplicationProvider(options => identitySection.Bind(options));
        identity.UseConfigurationBasedRoleProvider(options => identitySection.Bind(options));
    });
    
    // Use default authentication (JWT).
    elsa.UseDefaultAuthentication();
});

// Configure CORS to allow designer app hosted on a different origin to invoke the APIs.
builder.Services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// Build the web app.
var app = builder.Build();

// Configure the web app's request pipeline.
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();

// Run the web app.
app.Run();