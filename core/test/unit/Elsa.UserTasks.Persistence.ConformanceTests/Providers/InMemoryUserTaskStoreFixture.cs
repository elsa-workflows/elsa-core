using Elsa.UserTasks.Contracts;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Repositories;
using Elsa.UserTasks.Services;

namespace Elsa.UserTasks.Persistence.ConformanceTests.Providers;

/// <summary>
/// The in-process stores. They are the reference implementation the durable providers are held against,
/// so they run the same suite rather than a reduced one.
/// </summary>
public sealed class InMemoryUserTaskStoreFixture : UserTaskStoreFixture
{
    private readonly InMemoryUserTaskRepository _repository = new();

    public InMemoryUserTaskStoreFixture() : base(ConformanceProviders.InMemory)
    {
        GuestSessions = new InMemoryUserTaskGuestSessionIssuer(Clock, Options);
        Outbox = new InMemoryUserTaskInvitationOutbox(DataProtection, Clock, Options);
    }

    public override IUserTaskRepository Repository => _repository;
    public override IUserTaskGuestSessionIssuer GuestSessions { get; }
    public override IUserTaskInvitationOutbox Outbox { get; }

    // One dictionary backs the store, so a "second" repository is the same instance. Concurrency here is
    // enforced by the revision compare-and-swap, not by separate connections.
    public override IUserTaskRepository CreateSecondRepository() => _repository;

    protected override Task ActivateCoreAsync() => Task.CompletedTask;
}
