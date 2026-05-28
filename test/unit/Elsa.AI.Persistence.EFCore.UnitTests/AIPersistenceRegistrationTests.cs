using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Elsa.AI.Persistence.EFCore.Extensions;
using Elsa.AI.Persistence.EFCore.Stores;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.AI.Persistence.EFCore.UnitTests;

public class AIPersistenceRegistrationTests
{
    [Fact(DisplayName = "AI persistence store registration configures DbContext")]
    public void AIPersistenceStoreRegistrationConfiguresDbContext()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var services = new ServiceCollection();

        services.AddAIPersistenceStores(options => options.UseSqlite(connection));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AIDbContext>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<IAIProposalStore>());
        Assert.IsType<EFCoreAIConversationStore>(scope.ServiceProvider.GetRequiredService<IAIConversationStore>());
    }

    [Fact(DisplayName = "AI persistence store registration replaces existing conversation store")]
    public void AIPersistenceStoreRegistrationReplacesExistingConversationStore()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var services = new ServiceCollection();
        services.AddSingleton<IAIConversationStore, StubConversationStore>();
        services.AddSingleton<IAIProposalStore, StubProposalStore>();

        services.AddAIPersistenceStores(options => options.UseSqlite(connection));

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.IsType<EFCoreAIConversationStore>(scope.ServiceProvider.GetRequiredService<IAIConversationStore>());
        Assert.IsType<EFCoreAIProposalStore>(scope.ServiceProvider.GetRequiredService<IAIProposalStore>());
    }

    [Fact(DisplayName = "AI persistence store registration requires a DbContext provider")]
    public void AIPersistenceStoreRegistrationRequiresDbContextProvider()
    {
        var services = new ServiceCollection();

        Assert.Throws<InvalidOperationException>(() => services.AddAIPersistenceStores());
    }

    [Fact(DisplayName = "AI persistence store registration uses preconfigured DbContext options")]
    public void AIPersistenceStoreRegistrationUsesPreconfiguredDbContextOptions()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        var services = new ServiceCollection();
        services.AddSingleton(new DbContextOptionsBuilder<AIDbContext>().UseSqlite(connection).Options);

        services.AddAIPersistenceStores();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        using var scope = provider.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService<AIDbContext>());
        Assert.IsType<EFCoreAIConversationStore>(scope.ServiceProvider.GetRequiredService<IAIConversationStore>());
    }

    private class StubConversationStore : IAIConversationStore
    {
        public ValueTask<AIConversation?> FindAsync(string id, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AIConversation?>(null);

        public ValueTask SaveAsync(AIConversation conversation, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }

    private class StubProposalStore : IAIProposalStore
    {
        public ValueTask<AIProposal?> FindAsync(string id, string? tenantId, CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<AIProposal?>(null);

        public ValueTask SaveAsync(AIProposal proposal, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
