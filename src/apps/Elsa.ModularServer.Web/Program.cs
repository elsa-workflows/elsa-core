using CShells.AspNetCore.Configuration;
using CShells.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Configure CShells for multi-tenancy with ASP.NET Core integration
// This automatically registers shell-aware authentication and authorization providers
builder.AddShells(shells => shells.WithAuthenticationAndAuthorization());
services.AddHealthChecks();

// Add minimal authentication and authorization services in root
// These are required for middleware validation - shells provide the actual configurations
services.AddAuthentication();
services.AddAuthorization();

var app = builder.Build();

app.MapHealthChecks("/");
app.MapShells();           // Sets HttpContext.RequestServices to shell's scoped provider
app.UseAuthentication();   // Runs after MapShells to access shell-specific auth schemes
app.UseAuthorization();    // Runs after MapShells to access shell-specific policies
app.Run();