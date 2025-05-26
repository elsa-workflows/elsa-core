using System.Net;
using Elsa.Http;
using Elsa.Resilience.IntegrationTests;
using Elsa.Resilience.Models;
using Elsa.Resilience.Recorders;
using Elsa.Testing.Shared;
using Elsa.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace Elsa.Resilience.IntegrationTests;

public class FlowSendHttpRequestResilienceTests
{
    private readonly IServiceProvider _services;
    private readonly TestRetryAttemptRecorder _recorder = new();

    public FlowSendHttpRequestResilienceTests(ITestOutputHelper output)
    {
        var handler = new SequentialStatusHandler(new[] { HttpStatusCode.TooManyRequests, HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK });

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Resilience:Strategies:0:$type"] = "HttpResilienceStrategy",
                ["Resilience:Strategies:0:Id"] = "test",
                ["Resilience:Strategies:0:DisplayName"] = "Test",
                ["Resilience:Strategies:0:MaxRetryAttempts"] = "5",
                ["Resilience:Strategies:0:Delay"] = "00:00:00.001"
            })
            .Build();

        _services = new TestApplicationBuilder(output)
            .ConfigureServices(s =>
            {
                s.AddSingleton<IConfiguration>(configuration);
            })
            .ConfigureElsa(module =>
            {
                module.UseHttp(http =>
                {
                    http.HttpClientBuilder = builder => builder.ConfigurePrimaryHttpMessageHandler(() => handler);
                });

                module.UseResilience(resilience =>
                {
                    resilience.WithRetryAttemptRecorder(_ => _recorder);
                });
            })
            .Build();
    }

    [Fact(DisplayName = "FlowSendHttpRequest retries using selected resilience strategy")]
    public async Task InvokesResilienceStrategy()
    {
        var activity = new FlowSendHttpRequest
        {
            Url = new(new Uri("http://localhost/test")),
            Method = new("GET"),
            ExpectedStatusCodes = new(new[] { 200 })
        };

        activity.CustomProperties["resilienceStrategy"] = new ResilienceStrategyConfig
        {
            Mode = ResilienceStrategyConfigMode.Identifier,
            StrategyId = "test"
        };

        var result = await _services.RunActivityAsync(activity);
        var statusCode = result.GetActivityOutput<int>(activity, nameof(SendHttpRequestBase.StatusCode));

        Assert.Equal(200, statusCode);
        Assert.Equal(2, _recorder.Attempts.Count);
        Assert.Collection(_recorder.Attempts,
            first => Assert.Equal(1, first.AttemptNumber),
            second => Assert.Equal(2, second.AttemptNumber));
    }
}
