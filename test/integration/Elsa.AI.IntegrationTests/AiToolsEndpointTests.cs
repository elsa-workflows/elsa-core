using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Host.Endpoints.Ai.Tools;
using Elsa.AI.Host.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AiToolsEndpointTests
{
    [Fact(DisplayName = "Tools endpoint returns enabled registry results")]
    public async Task ToolsEndpointReturnsEnabledRegistryResults()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        using var provider = services.BuildServiceProvider();
        var endpoint = new Endpoint(provider.GetRequiredService<IAiToolRegistry>());

        var tools = await endpoint.ExecuteAsync(CancellationToken.None);

        Assert.Empty(tools);
    }
}
