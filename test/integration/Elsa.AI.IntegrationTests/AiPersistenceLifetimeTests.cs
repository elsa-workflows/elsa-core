using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Host.Extensions;
using Elsa.AI.Persistence.EFCore.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AiPersistenceLifetimeTests
{
    [Fact(DisplayName = "EF Core conversation persistence resolves with scope validation enabled")]
    public void EFCoreConversationPersistenceResolvesWithScopeValidationEnabled()
    {
        var services = new ServiceCollection();
        services.AddAiHostServices();
        services.AddAiPersistenceStores(_ => { });

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAiOrchestrator>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAiConversationStore>());
    }
}
