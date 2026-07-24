using System.Text;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Http;

namespace Elsa.ExternalAuthentication.Endpoints.Connections;

internal sealed class ListConnections(IdentityProviderConnectionManagementService management, IConnectionObservationStore observations, ITenantAccessor tenantAccessor) : ElsaEndpoint<ConnectionListRequest, ConnectionListResponse>
{
    public override void Configure()
    {
        Get("/external-authentication/connections");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsRead);
    }

    public override async Task<ConnectionListResponse> ExecuteAsync(ConnectionListRequest request, CancellationToken cancellationToken)
    {
        var requestedScope = request.Scope ?? request.ScopeKind;
        var scope = ToScope(requestedScope, request.TenantId);
        if ((!string.IsNullOrWhiteSpace(requestedScope) && scope is null) ||
            !IsAllowedScope(scope, tenantAccessor.TenantId) ||
            request.PageSize is < 1 or > 100 ||
            !IsKnownSource(request.Source) ||
            !TryDecodeCursor(request.Cursor, out var cursor))
        {
            HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            return new ConnectionListResponse([], null);
        }

        var filter = new ConnectionFilter
        {
            Search = request.Search,
            Ownership = request.Source?.Equals("configuration", StringComparison.OrdinalIgnoreCase) == true ? ConnectionSourceOwnership.Configuration : request.Source?.Equals("database", StringComparison.OrdinalIgnoreCase) == true ? ConnectionSourceOwnership.Database : null,
            Scope = scope,
            AdapterType = request.AdapterType,
            IsEnabled = request.Enabled,
            IsArchived = request.Archived
        };
        var connections = (await management.ListAsync(tenantAccessor.TenantId, filter, cancellationToken))
            .Where(x => !request.Valid.HasValue || request.Valid.Value == (x.Validity == ConnectionValidity.Valid))
            .Where(x => !request.Shadowed.HasValue || request.Shadowed.Value == x.IsShadowed)
            .OrderBy(x => x.Scope.Kind)
            .ThenBy(x => x.Scope.TenantId, StringComparer.Ordinal)
            .ThenBy(x => x.Connection.DisplayOrder)
            .ThenBy(x => x.Connection.Key, StringComparer.Ordinal)
            .ThenBy(x => x.Connection.Id, StringComparer.Ordinal)
            .Where(x => cursor is null || Compare(CursorFor(x), cursor) > 0)
            .Take(request.PageSize.GetValueOrDefault(100) + 1)
            .ToArray();
        var hasNextPage = connections.Length > request.PageSize.GetValueOrDefault(100);
        var page = hasNextPage ? connections[..^1] : connections;
        var observationResults = await Task.WhenAll(page.Select(x => observations.FindLatestAsync(x.Connection.Id, cancellationToken).AsTask()));
        var items = await Task.WhenAll(page.Select((x, index) => ConnectionResponse.FromAsync(x, management, observationResults[index], cancellationToken).AsTask()));
        return new ConnectionListResponse(items, hasNextPage ? EncodeCursor(CursorFor(page[^1])) : null);
    }

    private static ConnectionScope? ToScope(string? kind, string? tenantId) => kind?.ToLowerInvariant() switch
    {
        "host" => ConnectionScope.Host,
        "defaulttenant" or "default-tenant" or "default" => ConnectionScope.DefaultTenant,
        "tenant" when tenantId is not null => new ConnectionScope(ConnectionScopeKind.Tenant, tenantId),
        _ => null
    };

    private static bool IsAllowedScope(ConnectionScope? scope, string tenantId) => scope is null || scope.Kind switch
    {
        ConnectionScopeKind.Host => true,
        ConnectionScopeKind.DefaultTenant => tenantId.Length == 0,
        ConnectionScopeKind.Tenant => string.Equals(scope.TenantId, tenantId, StringComparison.Ordinal),
        _ => false
    };

    private static bool IsKnownSource(string? source) => string.IsNullOrWhiteSpace(source) ||
        source.Equals("configuration", StringComparison.OrdinalIgnoreCase) ||
        source.Equals("database", StringComparison.OrdinalIgnoreCase);

    private static ConnectionCursor CursorFor(EffectiveIdentityProviderConnection connection) => new((int)connection.Scope.Kind, connection.Scope.TenantId, connection.Connection.DisplayOrder, connection.Connection.Key, connection.Connection.Id);
    private static int Compare(ConnectionCursor left, ConnectionCursor right)
    {
        var result = left.ScopeKind.CompareTo(right.ScopeKind);
        result = result != 0 ? result : string.Compare(left.TenantId, right.TenantId, StringComparison.Ordinal);
        result = result != 0 ? result : left.Order.CompareTo(right.Order);
        result = result != 0 ? result : string.Compare(left.Key, right.Key, StringComparison.Ordinal);
        return result != 0 ? result : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
    }

    private static string EncodeCursor(ConnectionCursor cursor) => Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cursor))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static bool TryDecodeCursor(string? value, out ConnectionCursor? cursor)
    {
        cursor = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;
        try
        {
            var padded = value.Replace('-', '+').Replace('_', '/');
            padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
            cursor = JsonSerializer.Deserialize<ConnectionCursor>(Encoding.UTF8.GetString(Convert.FromBase64String(padded)));
            return cursor is not null &&
                cursor.ScopeKind is >= (int)ConnectionScopeKind.Host and <= (int)ConnectionScopeKind.Tenant &&
                cursor.TenantId is not null && cursor.Key is not null && cursor.Id is not null &&
                cursor.TenantId.Length <= 256 && cursor.Key.Length <= 128 && cursor.Id.Length <= 256;
        }
        catch (Exception exception) when (exception is FormatException or JsonException)
        {
            return false;
        }
    }

    private sealed record ConnectionCursor(int ScopeKind, string TenantId, int Order, string Key, string Id);
}

internal sealed class GetConnection(IdentityProviderConnectionManagementService management, IConnectionObservationStore observations, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/external-authentication/connections/{connectionId}");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsRead);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await management.FindAsync(Route<string>("connectionId")!, tenantAccessor.TenantId, cancellationToken);
        if (result is not ManagementConnectionLookupResult.Found(var connection))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken);
            return;
        }

        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(connection, management, await observations.FindLatestAsync(connection.Connection.Id, cancellationToken), cancellationToken), cancellationToken);
    }
}

internal sealed class CreateConnection(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpoint<ConnectionRequest>
{
    public override void Configure()
    {
        Post("/external-authentication/connections");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsCreate);
    }

    public override async Task HandleAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        if (ConnectionEndpointSupport.RequiresPolicyManagement(request) && !ConnectionEndpointSupport.HasPermission(User, ExternalAuthenticationPermissions.PoliciesManage))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "The caller may not configure policies or permission grants.", cancellationToken);
            return;
        }

        var result = await management.CreateAsync(request.ToConnection(tenantAccessor.TenantId), User, tenantAccessor.TenantId, request.ConfirmUnsafeSettings, cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }

        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, ToScope(connection.TenantId), ConnectionValidity.Unknown, false, "database");
        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        HttpContext.Response.Headers.Location = $"/external-authentication/connections/{Uri.EscapeDataString(connection.Id)}";
        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(effective, management, null, cancellationToken), cancellationToken);
    }

    private static ConnectionScope ToScope(string tenantId) => tenantId == ConnectionScope.HostTenantId ? ConnectionScope.Host : tenantId.Length == 0 ? ConnectionScope.DefaultTenant : new ConnectionScope(ConnectionScopeKind.Tenant, tenantId);
}

internal sealed class UpdateConnection(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpoint<ConnectionRequest>
{
    public override void Configure()
    {
        Put("/external-authentication/connections/{connectionId}");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsUpdate);
    }

    public override async Task HandleAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!ConnectionEndpointSupport.TryGetExpectedRevision(HttpContext, out var revision))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status428PreconditionRequired, "precondition_required", "If-Match with the current connection revision is required.", cancellationToken);
            return;
        }

        var existing = await management.FindAsync(Route<string>("connectionId")!, tenantAccessor.TenantId, cancellationToken);
        if (existing is not ManagementConnectionLookupResult.Found(var effective))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken);
            return;
        }
        if (!ConnectionEndpointSupport.IsDatabaseOwned(effective))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "Configuration-owned connections are read-only.", cancellationToken);
            return;
        }
        if (ConnectionEndpointSupport.RequiresPolicyManagement(request) && !ConnectionEndpointSupport.HasPermission(User, ExternalAuthenticationPermissions.PoliciesManage))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "The caller may not configure policies or permission grants.", cancellationToken);
            return;
        }

        var result = await management.UpdateAsync(effective.Connection.Id, request.ToConnection(effective.Connection.TenantId), revision, User, tenantAccessor.TenantId, request.ConfirmUnsafeSettings, request.ConfirmFinalLoginPathOverride, cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }

        var responseConnection = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, effective.Scope, ConnectionValidity.Unknown, false, "database");
        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(responseConnection, management, null, cancellationToken), cancellationToken);
    }
}

internal abstract class ConnectionLifecycleEndpoint(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    protected abstract ConnectionLifecycle Action { get; }
    protected abstract string Permission { get; }
    protected abstract void ConfigureRoute();

    public override void Configure()
    {
        ConfigureRoute();
        ConfigurePermissions(Permission);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (!ConnectionEndpointSupport.TryGetExpectedRevision(HttpContext, out var revision))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status428PreconditionRequired, "precondition_required", "If-Match with the current connection revision is required.", cancellationToken);
            return;
        }
        var existing = await management.FindAsync(Route<string>("connectionId")!, tenantAccessor.TenantId, cancellationToken);
        if (existing is not ManagementConnectionLookupResult.Found(var effective))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken);
            return;
        }
        if (!ConnectionEndpointSupport.IsDatabaseOwned(effective))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "Configuration-owned connections are read-only.", cancellationToken);
            return;
        }

        var confirmOverride = string.Equals(HttpContext.Request.Query["confirmFinalLoginPathOverride"], "true", StringComparison.OrdinalIgnoreCase);
        var result = await management.ChangeLifecycleAsync(effective.Connection.Id, Action, revision, User, tenantAccessor.TenantId, confirmOverride, cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }

        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, effective.Scope, ConnectionValidity.Unknown, false, "database"), management, null, cancellationToken), cancellationToken);
    }
}

internal sealed class EnableConnection(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ConnectionLifecycleEndpoint(management, tenantAccessor)
{
    protected override ConnectionLifecycle Action => ConnectionLifecycle.Enabled;
    protected override string Permission => ExternalAuthenticationPermissions.ConnectionsUpdate;
    protected override void ConfigureRoute() => Post("/external-authentication/connections/{connectionId}/enable");
}

internal sealed class DisableConnection(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ConnectionLifecycleEndpoint(management, tenantAccessor)
{
    protected override ConnectionLifecycle Action => ConnectionLifecycle.Disabled;
    protected override string Permission => ExternalAuthenticationPermissions.ConnectionsUpdate;
    protected override void ConfigureRoute() => Post("/external-authentication/connections/{connectionId}/disable");
}

internal sealed class ArchiveConnection(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ConnectionLifecycleEndpoint(management, tenantAccessor)
{
    protected override ConnectionLifecycle Action => ConnectionLifecycle.Archived;
    protected override string Permission => ExternalAuthenticationPermissions.ConnectionsArchive;
    protected override void ConfigureRoute() => Delete("/external-authentication/connections/{connectionId}");
}

internal sealed class RestoreConnection(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ConnectionLifecycleEndpoint(management, tenantAccessor)
{
    protected override ConnectionLifecycle Action => ConnectionLifecycle.Draft;
    protected override string Permission => ExternalAuthenticationPermissions.ConnectionsArchive;
    protected override void ConfigureRoute() => Post("/external-authentication/connections/{connectionId}/restore");
}

internal sealed class ValidateConnection(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/external-authentication/connections/{connectionId}/validate");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsRead);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await management.FindAsync(Route<string>("connectionId")!, tenantAccessor.TenantId, cancellationToken);
        if (result is not ManagementConnectionLookupResult.Found(var connection))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken);
            return;
        }

        var validation = await management.ValidateAsync(connection.Connection, User, tenantAccessor.TenantId, requireCompleteConfiguration: false, confirmUnsafeSettings: ConnectionEndpointSupport.HasPermission(User, ExternalAuthenticationPermissions.ProviderTrustUnsafe), cancellationToken: cancellationToken);
        await HttpContext.Response.WriteAsJsonAsync(new ConnectionValidationResponse(validation.IsValid, validation.Errors, validation.Warnings), cancellationToken);
    }
}

internal sealed class ReplaceSecretBinding(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpoint<SecretBindingRequest>
{
    public override void Configure()
    {
        Put("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsUpdate);
    }

    public override Task HandleAsync(SecretBindingRequest request, CancellationToken cancellationToken) => MutateAsync(request.ToBinding(), cancellationToken);

    private async Task MutateAsync(SecretBinding request, CancellationToken cancellationToken)
    {
        if (!ConnectionEndpointSupport.TryGetExpectedRevision(HttpContext, out var revision))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status428PreconditionRequired, "precondition_required", "If-Match with the current connection revision is required.", cancellationToken);
            return;
        }
        var lookup = await management.FindAsync(Route<string>("connectionId")!, tenantAccessor.TenantId, cancellationToken);
        if (lookup is not ManagementConnectionLookupResult.Found(var effective))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken);
            return;
        }
        if (!ConnectionEndpointSupport.IsDatabaseOwned(effective))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "Configuration-owned connections are read-only.", cancellationToken);
            return;
        }
        var candidate = Elsa.ExternalAuthentication.Services.IdentityProviderConnectionCloner.Clone(effective.Connection);
        candidate.SecretBindings[Route<string>("fieldName")!] = request;
        var result = await management.UpdateAsync(candidate.Id, candidate, revision, User, tenantAccessor.TenantId, false, cancellationToken: cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }
        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, effective.Scope, ConnectionValidity.Unknown, false, "database"), management, null, cancellationToken), cancellationToken);
    }
}

internal sealed class RemoveSecretBinding(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}");
        ConfigurePermissions(ExternalAuthenticationPermissions.ConnectionsUpdate);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        if (!ConnectionEndpointSupport.TryGetExpectedRevision(HttpContext, out var revision))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status428PreconditionRequired, "precondition_required", "If-Match with the current connection revision is required.", cancellationToken);
            return;
        }
        var lookup = await management.FindAsync(Route<string>("connectionId")!, tenantAccessor.TenantId, cancellationToken);
        if (lookup is not ManagementConnectionLookupResult.Found(var effective))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken);
            return;
        }
        if (!ConnectionEndpointSupport.IsDatabaseOwned(effective))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "Configuration-owned connections are read-only.", cancellationToken);
            return;
        }
        var candidate = Elsa.ExternalAuthentication.Services.IdentityProviderConnectionCloner.Clone(effective.Connection);
        candidate.SecretBindings.Remove(Route<string>("fieldName")!);
        var result = await management.UpdateAsync(candidate.Id, candidate, revision, User, tenantAccessor.TenantId, false, cancellationToken: cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }
        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, effective.Scope, ConnectionValidity.Unknown, false, "database"), management, null, cancellationToken), cancellationToken);
    }
}

internal sealed class ConnectionListRequest
{
    public string? Search { get; set; }
    public string? Source { get; set; }
    public string? Scope { get; set; }
    public string? ScopeKind { get; set; }
    public string? TenantId { get; set; }
    public string? AdapterType { get; set; }
    public bool? Enabled { get; set; }
    public bool? Valid { get; set; }
    public bool? Shadowed { get; set; }
    public bool? Archived { get; set; }
    public string? Cursor { get; set; }
    public int? PageSize { get; set; }
}
