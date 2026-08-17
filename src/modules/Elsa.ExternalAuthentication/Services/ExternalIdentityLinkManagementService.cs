using System.Security.Claims;
using Elsa.Common;
using Elsa.Common.Models;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.ExternalAuthentication.Services;

/// <summary>
/// Coordinates tenant-safe administrator operations over external identity links.
/// </summary>
public sealed class ExternalIdentityLinkManagementService(
    IExternalIdentityProvisioner provisioner,
    IExternalIdentityLinkManagementStore links,
    IIdentityProviderConnectionRegistry connections,
    IUserStore users,
    ISystemClock clock,
    IServiceProvider services)
{
    public async ValueTask<Page<ExternalIdentityLink>> ListAsync(string tenantId, ExternalIdentityLinkFilter filter, CancellationToken cancellationToken = default)
    {
        ValidateTargetTenant(tenantId);
        ArgumentNullException.ThrowIfNull(filter);
        filter.TenantId = tenantId;
        return await links.FindAsync(filter, cancellationToken);
    }

    public async ValueTask<Page<ExternalIdentityLinkUser>> FindUsersAsync(string tenantId, string? search, CancellationToken cancellationToken = default)
    {
        ValidateTargetTenant(tenantId);
        var normalizedSearch = search?.Trim();
        var usersInTenant = (await users.FindManyAsync(new UserFilter { TenantId = tenantId }, cancellationToken))
            .Where(x => string.IsNullOrEmpty(normalizedSearch) || x.Name.Contains(normalizedSearch, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Name, StringComparer.Ordinal)
            .ThenBy(x => x.Id, StringComparer.Ordinal)
            .Select(x => new ExternalIdentityLinkUser(x.Id, x.Name))
            .ToArray();
        return Page.Of<ExternalIdentityLinkUser>(usersInTenant, usersInTenant.Length);
    }

    public async ValueTask<ExternalIdentityLinkPrelinkResult> PrelinkAsync(string tenantId, string userId, string connectionKey, string issuer, string subject, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        ValidateTargetTenant(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionKey);
        var normalizedIssuer = NormalizeIssuer(issuer);
        var normalizedSubject = NormalizeSubject(subject);

        var user = await users.FindAsync(new UserFilter { Id = userId }, cancellationToken);
        if (user is null || !string.Equals(user.TenantId, tenantId, StringComparison.Ordinal))
            return new ExternalIdentityLinkPrelinkResult.UserNotFound();

        // Manual administration may target disabled or invalid connections, but never archived, shadowed, or cross-tenant definitions.
        var connection = await FindManageableConnectionAsync(tenantId, connectionKey, cancellationToken);
        if (connection is null)
            return new ExternalIdentityLinkPrelinkResult.ConnectionNotFound();

        try
        {
            var result = await provisioner.CreateLinkOrGetExistingAsync(new ProvisioningRequest(
                tenantId,
                ConnectionRevisionCalculator.NormalizeKey(connection.Connection.Key),
                new ExternalIdentity(normalizedIssuer, normalizedSubject, new Dictionary<string, IReadOnlyCollection<string>>()),
                null,
                user.Id), cancellationToken);

            if (!string.Equals(result.UserId, user.Id, StringComparison.Ordinal))
                return new ExternalIdentityLinkPrelinkResult.Conflict(result.Link);

            if (result.WasLinkCreated)
                await PublishAsync(actor, result.Link, "prelinked", cancellationToken);
            return new ExternalIdentityLinkPrelinkResult.Success(result.Link, result.WasLinkCreated);
        }
        catch (InvalidOperationException)
        {
            // The provisioner protects its own user/tenant invariant. Do not turn a malformed or cross-tenant target into a disclosure.
            return new ExternalIdentityLinkPrelinkResult.UserNotFound();
        }
    }

    public async ValueTask<bool> UnlinkAsync(string tenantId, string linkId, ClaimsPrincipal actor, CancellationToken cancellationToken = default)
    {
        ValidateTargetTenant(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(linkId);
        var existing = await links.FindAsync(new ExternalIdentityLinkFilter { TenantId = tenantId }, cancellationToken);
        var link = existing.Items.FirstOrDefault(x => string.Equals(x.Id, linkId, StringComparison.Ordinal));
        if (link is null || !await links.DeleteAsync(tenantId, linkId, cancellationToken))
            return false;

        await PublishAsync(actor, link, "unlinked", cancellationToken);
        return true;
    }

    public async ValueTask<ExternalIdentityLinkReplaceResult> ReplaceAsync(
        string tenantId,
        string linkId,
        string userId,
        string connectionKey,
        string issuer,
        string subject,
        ClaimsPrincipal actor,
        CancellationToken cancellationToken = default)
    {
        ValidateTargetTenant(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(linkId);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionKey);
        var normalizedIssuer = NormalizeIssuer(issuer);
        var normalizedSubject = NormalizeSubject(subject);

        var user = await users.FindAsync(new UserFilter { Id = userId }, cancellationToken);
        if (user is null || !string.Equals(user.TenantId, tenantId, StringComparison.Ordinal))
            return new ExternalIdentityLinkReplaceResult.NotFound();

        var connection = await FindManageableConnectionAsync(tenantId, connectionKey, cancellationToken);
        if (connection is null)
            return new ExternalIdentityLinkReplaceResult.NotFound();

        ExternalIdentityLinkReplaceResult result;
        try
        {
            result = await provisioner.ReplaceAsync(
                new ExternalIdentityLinkReplaceRequest(
                    tenantId,
                    linkId,
                    user.Id,
                    ConnectionRevisionCalculator.NormalizeKey(connection.Connection.Key),
                    new ExternalIdentity(normalizedIssuer, normalizedSubject, new Dictionary<string, IReadOnlyCollection<string>>())),
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return new ExternalIdentityLinkReplaceResult.NotFound();
        }

        switch (result)
        {
            case ExternalIdentityLinkReplaceResult.Success success:
                await PublishReplacementAsync(actor, success.OldLink, success.NewLink, null, user.Id, success.NewLink.ConnectionKey, SecurityEventOutcome.Succeeded, cancellationToken);
                break;
            case ExternalIdentityLinkReplaceResult.Conflict conflict:
                await PublishReplacementAsync(
                    actor,
                    conflict.OldLink,
                    null,
                    conflict.ConflictingLink,
                    user.Id,
                    ConnectionRevisionCalculator.NormalizeKey(connection.Connection.Key),
                    SecurityEventOutcome.Failed,
                    cancellationToken);
                break;
        }

        return result;
    }

    private async ValueTask<EffectiveIdentityProviderConnection?> FindManageableConnectionAsync(string tenantId, string connectionKey, CancellationToken cancellationToken)
    {
        var normalizedKey = ConnectionRevisionCalculator.NormalizeKey(connectionKey);
        var registry = await connections.GetAsync(tenantId, cancellationToken);
        return registry.Connections.FirstOrDefault(x =>
            !x.IsShadowed &&
            !x.Connection.ArchivedAt.HasValue &&
            string.Equals(ConnectionRevisionCalculator.NormalizeKey(x.Connection.Key), normalizedKey, StringComparison.Ordinal));
    }

    private async ValueTask PublishAsync(ClaimsPrincipal actor, ExternalIdentityLink link, string operation, CancellationToken cancellationToken)
    {
        var sender = services.GetService<INotificationSender>();
        if (sender is null)
            return;

        var context = new SecurityEventContext(
            actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? actor.FindFirstValue("sub"),
            link.TenantId,
            link.ConnectionKey,
            link.UserId,
            clock.UtcNow,
            SecurityEventOutcome.Succeeded,
            Guid.NewGuid().ToString("N"),
            "External identity link management operation completed.");
        await sender.SendAsync(new ExternalIdentityLinkChanged(context, operation, link.Id), cancellationToken);
    }

    private async ValueTask PublishReplacementAsync(
        ClaimsPrincipal actor,
        ExternalIdentityLink oldLink,
        ExternalIdentityLink? newLink,
        ExternalIdentityLink? conflictingLink,
        string targetUserId,
        string targetConnectionKey,
        SecurityEventOutcome outcome,
        CancellationToken cancellationToken)
    {
        var sender = services.GetService<INotificationSender>();
        if (sender is null)
            return;

        var context = new SecurityEventContext(
            actor.FindFirstValue(ClaimTypes.NameIdentifier) ?? actor.FindFirstValue("sub"),
            oldLink.TenantId,
            targetConnectionKey,
            targetUserId,
            clock.UtcNow,
            outcome,
            Guid.NewGuid().ToString("N"),
            outcome == SecurityEventOutcome.Succeeded
                ? "External identity link replacement completed."
                : "External identity link replacement was rejected because the identity is already linked.");
        await sender.SendAsync(
            new ExternalIdentityLinkReplaced(
                context,
                oldLink.Id,
                newLink?.Id,
                oldLink.UserId,
                targetUserId,
                oldLink.ConnectionKey,
                targetConnectionKey,
                conflictingLink?.Id,
                conflictingLink?.UserId,
                conflictingLink?.ConnectionKey),
            cancellationToken);
    }

    private static string NormalizeIssuer(string issuer)
    {
        if (!Uri.TryCreate(issuer?.Trim(), UriKind.Absolute, out var uri) || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new ArgumentException("The issuer must be an absolute HTTPS URI without a query or fragment.", nameof(issuer));
        return uri.GetComponents(UriComponents.AbsoluteUri, UriFormat.UriEscaped).TrimEnd('/');
    }

    private static string NormalizeSubject(string subject)
    {
        var normalized = subject?.Trim();
        if (string.IsNullOrEmpty(normalized) || normalized.Length > 4096)
            throw new ArgumentException("The subject is required and must not exceed 4096 characters.", nameof(subject));
        return normalized;
    }

    private static void ValidateTargetTenant(string tenantId)
    {
        ArgumentNullException.ThrowIfNull(tenantId);
        if (string.Equals(tenantId, ConnectionScope.HostTenantId, StringComparison.Ordinal))
            throw new ArgumentException("External identity links require a resolved tenant.", nameof(tenantId));
    }
}

public sealed record ExternalIdentityLinkUser(string Id, string DisplayName);

public abstract record ExternalIdentityLinkPrelinkResult
{
    private ExternalIdentityLinkPrelinkResult() { }
    public sealed record Success(ExternalIdentityLink Link, bool WasCreated) : ExternalIdentityLinkPrelinkResult;
    public sealed record Conflict(ExternalIdentityLink Link) : ExternalIdentityLinkPrelinkResult;
    public sealed record UserNotFound : ExternalIdentityLinkPrelinkResult;
    public sealed record ConnectionNotFound : ExternalIdentityLinkPrelinkResult;
}
