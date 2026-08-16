using Elsa.ExternalAuthentication.Models;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Workflows;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Applies the provider-independent user resolution and creation policy used by external identity provisioners.
/// </summary>
public sealed class ExternalIdentityUserProvisioningService(
    IUserStore userStore,
    IUserProvider userProvider,
    IRoleProvider roleProvider,
    IIdentityGenerator identityGenerator)
{
    private const int MaximumUserNameAttempts = 10;

    /// <summary>
    /// Resolves an explicitly selected user or creates a credential-less user from the supplied proposal.
    /// </summary>
    public async ValueTask<(User User, bool WasCreated)> ResolveAsync(
        ProvisioningRequest request,
        Func<string, bool>? tryReserveUserName = null,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(request.ExistingUserId))
        {
            var existingUser = await userProvider.FindAsync(new UserFilter { Id = request.ExistingUserId }, cancellationToken)
                ?? throw new InvalidOperationException("The requested Elsa user does not exist.");
            if (!string.Equals(existingUser.TenantId, request.TenantId, StringComparison.Ordinal))
                throw new InvalidOperationException("The requested Elsa user is outside the target tenant.");

            return (existingUser, false);
        }

        var proposal = request.Proposal ?? throw new InvalidOperationException("A user creation proposal is required for an unlinked external identity.");
        var roleIds = await ResolveRoleIdsAsync(proposal.DefaultRoleIds, cancellationToken);
        var prefix = NormalizeUserNamePrefix(proposal.UserNamePrefix);
        for (var attempt = 0; attempt < MaximumUserNameAttempts; attempt++)
        {
            var name = $"{prefix}-{identityGenerator.GenerateId()}";
            if (tryReserveUserName is not null && !tryReserveUserName(name))
                continue;
            if (await userProvider.FindAsync(new UserFilter { Name = name }, cancellationToken) is not null)
                continue;

            var user = new User
            {
                Id = identityGenerator.GenerateId(),
                Name = name,
                TenantId = request.TenantId,
                HashedPassword = null,
                HashedPasswordSalt = null,
                Roles = roleIds.ToList()
            };

            try
            {
                await userStore.SaveAsync(user, cancellationToken);
                return (user, true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                var persistedUser = await userStore.FindAsync(new UserFilter { Id = user.Id }, CancellationToken.None);
                if (persistedUser is not null)
                    await userStore.DeleteAsync(new UserFilter { Id = user.Id }, CancellationToken.None);
                throw;
            }
            catch
            {
                var persistedUser = await userProvider.FindAsync(new UserFilter { Id = user.Id }, cancellationToken);
                if (persistedUser is not null)
                    return (persistedUser, true);
                if (await userProvider.FindAsync(new UserFilter { Name = name }, cancellationToken) is null)
                    throw;
            }
        }

        throw new InvalidOperationException("A unique Elsa user name could not be reserved for the external identity.");
    }

    /// <summary>
    /// Removes a user created by an operation that could not publish its external identity link.
    /// </summary>
    public Task RemoveAsync(User user, CancellationToken cancellationToken = default) =>
        userStore.DeleteAsync(new UserFilter { Id = user.Id }, cancellationToken);

    /// <summary>
    /// Checks that the resolved user still exists in the source that supplied it.
    /// </summary>
    public async ValueTask<bool> ExistsAsync(User user, bool wasCreated, CancellationToken cancellationToken = default) =>
        wasCreated
            ? await userStore.FindAsync(new UserFilter { Id = user.Id }, cancellationToken) is not null
            : await userProvider.FindAsync(new UserFilter { Id = user.Id }, cancellationToken) is not null;

    private static string NormalizeUserNamePrefix(string prefix)
    {
        var normalized = new string((prefix ?? string.Empty).Trim().Where(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '_').ToArray());
        return string.IsNullOrEmpty(normalized) ? "external" : normalized;
    }

    private async ValueTask<IReadOnlyCollection<string>> ResolveRoleIdsAsync(IReadOnlyCollection<string>? roleIds, CancellationToken cancellationToken)
    {
        var requested = (roleIds ?? []).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.Ordinal).ToArray();
        if (requested.Length == 0)
            return [];
        var found = (await roleProvider.FindByIdsAsync(requested, cancellationToken)).Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
        if (!found.SetEquals(requested))
            throw new InvalidOperationException("A configured default role no longer exists.");
        return requested;
    }
}
