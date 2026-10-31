using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Persistence.EFCore;
using Elsa.Extensions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.IntegrationTests;

public class AIPersistenceLifetimeTests
{
    [Fact(DisplayName = "EF Core conversation persistence resolves with scope validation enabled")]
    public void EFCoreConversationPersistenceResolvesWithScopeValidationEnabled()
    {
        var services = new ServiceCollection();
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        services.AddAIHostServices();
        services.AddAIPersistenceStores(options => options.UseSqlite(connection));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AIDbContext>().Database.EnsureCreated();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAIOrchestrator>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAIConversationStore>());
    }
}
