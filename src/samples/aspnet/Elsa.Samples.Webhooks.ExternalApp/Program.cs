using Elsa.Samples.Webhooks.ExternalApp.Controllers;
using Elsa.Samples.Webhooks.ExternalApp.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure the background worker with an HTTP client that can report workflow task completion.
builder.Services.AddHttpClient<DeliverFoodJob>(httpClient =>
{
    httpClient.BaseAddress = new Uri("https://localhost:7164/elsa/api/");
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();