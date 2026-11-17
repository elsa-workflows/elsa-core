using Elsa.Persistence.VNext.Document;
using Elsa.UserTasks.Models;
using Elsa.UserTasks.Persistence.ConformanceTests.Infrastructure;
using Elsa.UserTasks.Persistence.ConformanceTests.Providers;
using Elsa.UserTasks.Persistence.VNext.Repositories;
using NSubstitute;

namespace Elsa.UserTasks.Persistence.ConformanceTests;

[Collection(VNextCollection.Name), ConformanceProvider(ConformanceProviders.VNext)]
public sealed class VNextUserTaskInvitationLookupTests(VNextUserTaskStoreFixture fixture) : UserTaskConformanceTestBase(fixture)
{
    [ConformanceFact]
    public async Task LookupUsesPointReadsForKnownAndUnknownHashes()
    {
        await ActivateAsync();
        var task = CreateTask(Subject());
        var invitation = CreateInvitation(task);
        task.Invitations.Add(invitation);
        await ProjectAsync(task);
        var store = Substitute.For<IDocumentStore>();
        store.LoadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => fixture.DocumentStore.LoadAsync(call.ArgAt<string>(0), call.ArgAt<string>(1), call.ArgAt<CancellationToken>(2)));
        var repository = new VNextUserTaskRepository(store);

        var match = await repository.FindByInvitationTokenHashAsync(invitation.TokenHash);
        var unknown = await repository.FindByInvitationTokenHashAsync("unknown");

        Assert.Equal(invitation.Id, match?.Invitation.Id);
        Assert.Null(unknown);
        await store.Received(3).LoadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await store.DidNotReceive().QueryAsync(Arg.Any<DocumentQuery>(), Arg.Any<CancellationToken>());
    }

    [ConformanceFact]
    public async Task IndexingAStoredTaskMakesItsLegacyInvitationAvailable()
    {
        await ActivateAsync();
        var task = CreateTask(Subject());
        var invitation = CreateInvitation(task);
        task.Invitations.Add(invitation);
        await ProjectAsync(task);
        Assert.True(await fixture.DocumentStore.DeleteAsync("UserTaskInvitations", invitation.TokenHash));
        Assert.Null(await Repository.FindByInvitationTokenHashAsync(invitation.TokenHash));

        var repository = new VNextUserTaskRepository(fixture.DocumentStore);
        await repository.IndexInvitationTokensAsync(task.TenantId, task.Id);
        await repository.IndexInvitationTokensAsync(task.TenantId, task.Id);

        var match = await Repository.FindByInvitationTokenHashAsync(invitation.TokenHash);
        Assert.Equal(task.Id, match?.Task.Id);
        Assert.Equal(invitation.Id, match?.Invitation.Id);
    }

    [ConformanceFact]
    public async Task WritesIndexNewInvitationsAndLookupRequiresTheCurrentTaskToOwnTheHash()
    {
        await ActivateAsync();
        var task = await ProjectAsync(CreateTask(Subject()));
        var invitation = CreateInvitation(task);
        task.Invitations.Add(invitation);
        await Repository.SaveAsync(task, task.Revision);
        Assert.Equal(invitation.Id, (await Repository.FindByInvitationTokenHashAsync(invitation.TokenHash))?.Invitation.Id);

        var replacement = CreateInvitation(task);
        Assert.True(await Repository.TryMutateAsync(task.TenantId, task.Id, task.Revision, current =>
        {
            current.Invitations.Clear();
            current.Invitations.Add(replacement);
            return true;
        }));

        Assert.Null(await Repository.FindByInvitationTokenHashAsync(invitation.TokenHash));
        Assert.Equal(replacement.Id, (await Repository.FindByInvitationTokenHashAsync(replacement.TokenHash))?.Invitation.Id);
    }

    private UserTaskInvitation CreateInvitation(UserTask task) =>
        new(Guid.NewGuid().ToString("N"), task.TenantId, task.Id, "guest@example.test", $"HASH-{Guid.NewGuid():N}",
            UserTaskInvitationStatus.Pending, Clock.UtcNow, Clock.UtcNow.AddDays(1), "bearer");
}
