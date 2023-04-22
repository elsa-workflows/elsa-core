using System.Net.Http.Headers;
using Elsa.Samples.Webhooks.ExternalApp.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure the background worker with an HTTP client that can report workflow task completion.
builder.Services.AddHttpClient<DeliverFoodJob>(httpClient =>
{
    httpClient.BaseAddress = new Uri("https://localhost:5001/elsa/api/");
    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", Guid.Empty.ToString()); // Use the admin API key.
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();