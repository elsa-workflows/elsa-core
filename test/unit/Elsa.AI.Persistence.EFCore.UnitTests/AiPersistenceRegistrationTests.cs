using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Extensions;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class AiPersistenceRegistrationTests
{
    [Fact(DisplayName = "AI persistence store registration configures DbContext")]
    public void AiPersistenceStoreRegistrationConfiguresDbContext()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var services = new ServiceCollection();

        services.AddAiPersistenceStores(options => options.UseSqlite(connection));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AiDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAiProposalStore>());
        Assert.IsType<EFCoreAiConversationStore>(scope.ServiceProvider.GetRequiredService<IAiConversationStore>());
    }

    [Fact(DisplayName = "AI persistence store registration replaces existing conversation store")]
    public void AiPersistenceStoreRegistrationReplacesExistingConversationStore()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var services = new ServiceCollection();
        services.AddSingleton<IAiConversationStore, StubConversationStore>();

        services.AddAiPersistenceStores(options => options.UseSqlite(connection));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.IsType<EFCoreAiConversationStore>(scope.ServiceProvider.GetRequiredService<IAiConversationStore>());
    }

    [Fact(DisplayName = "AI persistence store registration requires a DbContext provider")]
    public void AiPersistenceStoreRegistrationRequiresDbContextProvider()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddAiPersistenceStores());
    }

    private class StubConversationStore : IAiConversationStore
    {
        public ValueTask<AiConversation?> FindAsync(string id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AiConversation?>(null);

        public ValueTask SaveAsync(AiConversation conversation, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
