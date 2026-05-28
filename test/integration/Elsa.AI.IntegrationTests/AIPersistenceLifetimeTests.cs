using Elsa.AI.Abstractions.Contracts;
using Elsa.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AIPersistenceLifetimeTests
{
    [Fact(DisplayName = "EF Core conversation persistence resolves with scope validation enabled")]
    public void EFCoreConversationPersistenceResolvesWithScopeValidationEnabled()
    {
        var services = new ServiceCollection();
        services.AddAIHostServices();
        services.AddAIPersistenceStores(_ => { });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAIOrchestrator>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAIConversationStore>());
    }
}
