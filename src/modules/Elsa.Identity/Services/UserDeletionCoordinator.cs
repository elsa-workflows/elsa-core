using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;

namespace Elsa.Identity.Services;

/// <inheritdoc />
public sealed class UserDeletionCoordinator(
    IUserStore userStore,
    IEnumerable<IUserDeletionDependencyContributor> contributors) : IUserDeletionCoordinator
{
    /// <inheritdoc />
    public async ValueTask<UserDeletionOperationResult> DeleteAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userStore.FindAsync(new() { Id = userId }, cancellationToken);
        if (user is null)
            return new UserDeletionOperationResult.NotFound();

        var dependencies = await InspectDependenciesAsync(user, cancellationToken);

        if (dependencies.Count > 0)
            return new UserDeletionOperationResult.Blocked(dependencies);

        await userStore.DeleteAsync(new() { Id = user.Id }, cancellationToken);

        // A link writer can race the first inspection when Identity and the contributing module use different stores.
        // Recheck after deletion and restore the aggregate if that writer committed first. Link writers perform the
        // complementary post-commit user check, so either ordering converges without a dangling reference.
        try
        {
            dependencies = await InspectDependenciesAsync(user, cancellationToken);
        }
        catch
        {
            await userStore.SaveAsync(user, CancellationToken.None);
            throw;
        }

        if (dependencies.Count > 0)
        {
            await userStore.SaveAsync(user, CancellationToken.None);
            return new UserDeletionOperationResult.Blocked(dependencies);
        }

        return new UserDeletionOperationResult.Deleted();
    }

    private async ValueTask<List<UserDeletionDependency>> InspectDependenciesAsync(User user, CancellationToken cancellationToken)
    {
        var dependencies = new List<UserDeletionDependency>();
        foreach (var contributor in contributors)
        {
            var dependency = await contributor.InspectAsync(user, cancellationToken);
            if (dependency is not null)
                dependencies.Add(dependency);
        }

        return dependencies;
    }
}
