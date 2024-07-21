using Elsa.Extensions;
using Elsa.Samples.AspNet.Webhooks.WorkflowServer.Workflows;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHandlersFrom<Program>();

builder.Services.AddElsa(elsa =>
{
    elsa.AddWorkflow<HungryWorkflow>()
    .UseHttp()
    .UseIdentity(identity =>
    {
        identity.UseAdminUserProvider();
        identity.TokenOptions = options =>
        {
            options.SigningKey = "secret-token-signing-key";
            options.AccessTokenLifetime = TimeSpan.FromDays(1);
        };
    })
    .UseWorkflowsApi()
    .UseDefaultAuthentication(auth => auth.UseAdminApiKey())
    .UseWebhooks(webhooks => webhooks.ConfigureSinks = options => builder.Configuration.GetSection("Webhooks:Sinks").Bind(options));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseWorkflowsApi();
app.UseWorkflows();
app.Run();