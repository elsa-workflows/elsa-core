using CShells.AspNetCore.Extensions;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

services.AddHealthChecks();
builder.AddShells();

var app = builder.Build();

app.MapHealthChecks("/");
app.MapShells();
app.Run();