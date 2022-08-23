using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;

// Add services.
services.AddHealthChecks();
services.AddCors(cors => cors.AddDefaultPolicy(policy => policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()));
services.AddElsaApiEndpoints();

services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options => configuration.GetSection("Auth0").Bind(options));

services.AddAuthorization();
services.AddElsa();

// Build app.
var app = builder.Build();

// Map endpoints.
app.UseCors();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHealthChecks("/");

    // Secure ALL controllers (i.e. including the ones exposed by Elsa).
    endpoints.MapControllers().RequireAuthorization();
});

// Start server.
app.Run();