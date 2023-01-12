using Elsa.EntityFrameworkCore.Extensions;
using Elsa.EntityFrameworkCore.Modules.Management;
using Elsa.EntityFrameworkCore.Modules.Runtime;
using Elsa.Extensions;
using Elsa.Samples.MassTransitActivities.Messages;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
var rabbitMqConnectionString = configuration.GetConnectionString("RabbitMq")!;

// Add services to the container.
builder.Services.AddElsa(elsa =>
{
    // Configure management feature to use EF Core.
    elsa.UseWorkflowManagement(management =>
    {
        management.UseWorkflowDefinitions(dm => dm.UseEntityFrameworkCore(ef => ef.UseSqlite()));
        management.UseWorkflowInstances(w => w.UseEntityFrameworkCore(ef => ef.UseSqlite()));
    });
    
    // Configure runtime feature to use EF Core.
    elsa.UseWorkflowRuntime(runtime =>
    {
        runtime.UseDefaultRuntime(d => d.UseEntityFrameworkCore(ef => ef.UseSqlite()));
        runtime.UseExecutionLogRecords(d => d.UseEntityFrameworkCore(ef => ef.UseSqlite()));
    });
    
    // Expose API endpoints.
    elsa.UseWorkflowsApi();

    // Add services for HTTP activities and workflow middleware.
    elsa.UseHttp();
    
    // Use JavaScript and Liquid.
    elsa.UseJavaScript();
    elsa.UseLiquid();
    
    // Configure identity so that we can create a default admin user.
    elsa.UseIdentity(identity =>
    {
        identity.IdentityOptions.CreateDefaultAdmin = builder.Environment.IsDevelopment();
        identity.TokenOptions.SigningKey = "secret-token-signing-key";
        identity.TokenOptions.Lifetime = TimeSpan.FromDays(1);
    });
    
    // Use default authentication (JWT).
    elsa.UseDefaultAuthentication();
    
    // Configure MassTransit.
    elsa.UseMassTransit(massTransit =>
    {
        massTransit.UseRabbitMq(rabbitMqConnectionString);
        massTransit.AddMessageType<OrderCompleted>();
        massTransit.AddMessageType<OrderCreated>();
    });
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