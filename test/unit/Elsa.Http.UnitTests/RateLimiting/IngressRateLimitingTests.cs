using System.Net;
using System.Threading.RateLimiting;
using Elsa.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Http.UnitTests.RateLimiting;

public class IngressRateLimitingTests
{
    [Fact]
    public async Task UseWorkflowsApiRateLimiting_AppliesPolicyToApiPrefix()
    {
        await using var app = await CreateAppAsync(app => app.UseWorkflowsApiRateLimiting("elsa/api", "test"));
        var client = app.GetTestClient();

        var firstResponse = await client.GetAsync("/elsa/api/workflow-definitions");
        var secondResponse = await client.GetAsync("/elsa/api/workflow-definitions");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    [Fact]
    public async Task UseWorkflowsRateLimiting_AppliesPolicyToHttpWorkflowBasePath()
    {
        await using var app = await CreateAppAsync(app => app.UseWorkflowsRateLimiting("/workflows", "test"));
        var client = app.GetTestClient();

        var firstResponse = await client.GetAsync("/workflows/hello-world");
        var secondResponse = await client.GetAsync("/workflows/hello-world");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, secondResponse.StatusCode);
    }

    private static async Task<WebApplication> CreateAppAsync(Action<WebApplication> configure)
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddFixedWindowLimiter("test", limiterOptions =>
            {
                limiterOptions.PermitLimit = 1;
                limiterOptions.Window = TimeSpan.FromMinutes(1);
                limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                limiterOptions.QueueLimit = 0;
            });
        });

        var app = builder.Build();
        configure(app);
        app.Run(context => context.Response.WriteAsync("ok"));

        await app.StartAsync();
        return app;
    }
}
