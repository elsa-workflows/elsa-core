using Elsa.Multitenancy;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseServiceProviderFactory(
    new AutofacMultitenantServiceProviderFactory(container => MultitenantContainerFactory.CreateSampleMultitenantContainer(container)));

var services = builder.Services.AddElsaServices();

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