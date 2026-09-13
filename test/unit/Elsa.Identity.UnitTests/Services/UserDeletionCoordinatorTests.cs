using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using System.Threading.Tasks;

namespace Elsa.Identity.UnitTests.Services;

public class UserDeletionCoordinatorTests
{
    [Test]
    public async Task CancellationAfterDeletionDoesNotCancelUserRestoration()
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        var user = new User { Id = "user-a", Name = "alice", TenantId = "tenant-a" };
        var store = new CancellationAwareUserStore(user);
        var coordinator = new UserDeletionCoordinator(store, [new CancelAfterDeletionContributor(cancellationTokenSource)]);

        await Assert.That(async () =>
        {
            await coordinator.DeleteAsync(user.Id, cancellationTokenSource.Token);
        }).Throws<OperationCanceledException>();

        await Assert.That(store.User).IsSameReferenceAs(user);
        await Assert.That(store.RestorationToken).IsEqualTo(CancellationToken.None);
    }

    private sealed class CancelAfterDeletionContributor(CancellationTokenSource cancellationTokenSource) : IUserDeletionDependencyContributor
    {
        private int _inspectionCount;

        public string Source => "test";

        public ValueTask<UserDeletionDependency?> InspectAsync(User user, CancellationToken cancellationToken = default)
        {
            if (Interlocked.Increment(ref _inspectionCount) == 2)
            {
                cancellationTokenSource.Cancel();
                throw new OperationCanceledException(cancellationTokenSource.Token);
            }

            return ValueTask.FromResult<UserDeletionDependency?>(null);
        }
    }

    private sealed class CancellationAwareUserStore(User user) : IUserStore
    {
        public User? User { get; private set; } = user;
        public CancellationToken? RestorationToken { get; private set; }

        public Task SaveAsync(User userToSave, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            User = userToSave;
            RestorationToken = cancellationToken;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            User = null;
            return Task.CompletedTask;
        }

        public Task<IEnumerable<User>> FindManyAsync(UserFilter filter, CancellationToken cancellationToken = default) =>
            Task.FromResult(User is null ? Enumerable.Empty<User>() : [User]);

        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(User);
        }
    }
}
