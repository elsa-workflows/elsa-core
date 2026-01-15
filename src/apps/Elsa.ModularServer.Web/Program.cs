using CShells.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

builder.AddShells();
services.AddHealthChecks();
services.AddAuthorization();
services.AddAuthentication();

var app = builder.Build();

app.MapHealthChecks("/");
app.UseAuthorization();
app.UseAuthentication();
app.MapShells();
app.Run();