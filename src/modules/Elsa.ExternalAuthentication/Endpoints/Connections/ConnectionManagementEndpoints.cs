using Elsa.Authorization;
using System.Text;
using System.Text.Json;
using Elsa.Abstractions;
using Elsa.Common.Multitenancy;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Permissions;
using Elsa.ExternalAuthentication.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Elsa.ExternalAuthentication.Endpoints.Connections;

internal sealed class ListConnections(IdentityProviderConnectionManagementService management, IConnectionObservationStore observations, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ElsaEndpoint<ConnectionListRequest, ConnectionListResponse>
{
    public override void Configure()
    {
        Get("/external-authentication/connections");
        RequirePermission(Elsa.ExternalAuthentication.Permissions.ExternalAuthenticationResourcePermissions.Connections, CoreVerbs.View);
    }

    public override async Task<ConnectionListResponse> ExecuteAsync(ConnectionListRequest request, CancellationToken cancellationToken)
    {
        var requestedScope = request.Scope ?? request.ScopeKind;
        var scope = ConnectionScope.Host;
        if ((!string.IsNullOrWhiteSpace(requestedScope) && !requestedScope.Equals("host", StringComparison.OrdinalIgnoreCase)) ||
            !string.IsNullOrWhiteSpace(request.TenantId) ||
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
        var items = await Task.WhenAll(page.Select((x, index) => ConnectionResponse.FromAsync(x, management, adapters, observationResults[index], cancellationToken).AsTask()));
        return new ConnectionListResponse(items, hasNextPage ? EncodeCursor(CursorFor(page[^1])) : null);
    }

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

internal sealed class GetConnection(IdentityProviderConnectionManagementService management, IConnectionObservationStore observations, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/external-authentication/connections/{connectionId}");
        RequirePermission(Elsa.ExternalAuthentication.Permissions.ExternalAuthenticationResourcePermissions.Connections, CoreVerbs.View);
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
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(connection, management, adapters, await observations.FindLatestAsync(connection.Connection.Id, cancellationToken), cancellationToken), cancellationToken);
    }
}

internal sealed class CreateConnection(IdentityProviderConnectionManagementService management, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ElsaEndpoint<ConnectionRequest>
{
    public override void Configure()
    {
        Post("/external-authentication/connections");
        RequirePermission(Elsa.ExternalAuthentication.Permissions.ExternalAuthenticationResourcePermissions.Connections, CoreVerbs.Create);
    }

    public override async Task HandleAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasOnlyHostScope())
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status400BadRequest, "host_scope_required", "Identity provider connections are managed host-wide in this version.", cancellationToken);
            return;
        }
        if (request.SecretBindings is not null)
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status400BadRequest, "secret_bindings_mutation_not_allowed", "Secret bindings must be managed through the dedicated write-only secret endpoint.", cancellationToken);
            return;
        }
        if (ConnectionEndpointSupport.RequiresPolicyManagement(request) && !ConnectionEndpointSupport.HasPermission(User, ExternalAuthenticationPermissions.PoliciesManage))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "The caller may not configure policies or permission grants.", cancellationToken);
            return;
        }

        var result = await management.CreateAsync(request.ToConnection(), User, tenantAccessor.TenantId, request.ConfirmUnsafeSettings, cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }

        var effective = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, ToScope(connection.TenantId), ConnectionValidity.Unknown, false, "database");
        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        HttpContext.Response.Headers.Location = $"/external-authentication/connections/{Uri.EscapeDataString(connection.Id)}";
        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(effective, management, adapters, null, cancellationToken), cancellationToken);
    }

    private static ConnectionScope ToScope(string tenantId) => tenantId == ConnectionScope.HostTenantId ? ConnectionScope.Host : tenantId.Length == 0 ? ConnectionScope.DefaultTenant : new ConnectionScope(ConnectionScopeKind.Tenant, tenantId);
}

internal sealed class UpdateConnection(IdentityProviderConnectionManagementService management, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ElsaEndpoint<ConnectionRequest>
{
    public override void Configure()
    {
        Put("/external-authentication/connections/{connectionId}");
        RequirePermission(Elsa.ExternalAuthentication.Permissions.ExternalAuthenticationResourcePermissions.Connections, CoreVerbs.Update);
    }

    public override async Task HandleAsync(ConnectionRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasOnlyHostScope())
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status400BadRequest, "host_scope_required", "Identity provider connections are managed host-wide in this version.", cancellationToken);
            return;
        }
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
        if (request.SecretBindings is not null)
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status400BadRequest, "secret_bindings_mutation_not_allowed", "Secret bindings must be managed through the dedicated write-only secret endpoint.", cancellationToken);
            return;
        }
        if (ConnectionEndpointSupport.RequiresPolicyManagement(request) && !ConnectionEndpointSupport.HasPermission(User, ExternalAuthenticationPermissions.PoliciesManage))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "The caller may not configure policies or permission grants.", cancellationToken);
            return;
        }

        var candidate = request.ToConnection();
        if (request.SecretBindings is null)
            candidate.SecretBindings = IdentityProviderConnectionCloner.Clone(effective.Connection).SecretBindings;

        var result = await management.UpdateAsync(effective.Connection.Id, candidate, revision, User, tenantAccessor.TenantId, request.ConfirmUnsafeSettings, request.ConfirmFinalLoginPathOverride, cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }

        var responseConnection = new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, effective.Scope, ConnectionValidity.Unknown, false, "database");
        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(responseConnection, management, adapters, null, cancellationToken), cancellationToken);
    }
}

internal abstract class ConnectionLifecycleEndpoint(IdentityProviderConnectionManagementService management, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    protected abstract ConnectionLifecycle Action { get; }
    protected abstract string Verb { get; }
    protected abstract void ConfigureRoute();

    public override void Configure()
    {
        ConfigureRoute();
        RequirePermission(ExternalAuthenticationResourcePermissions.Connections, Verb);
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
        var revokeActiveSessions = string.Equals(HttpContext.Request.Query["revokeActiveSessions"], "true", StringComparison.OrdinalIgnoreCase);
        if (revokeActiveSessions && !ConnectionEndpointSupport.HasPermission(User, ExternalAuthenticationPermissions.SessionsRevoke))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "forbidden", "Revoking active sessions requires the external-authentication sessions-revoke permission.", cancellationToken);
            return;
        }
        var result = await management.ChangeLifecycleAsync(effective.Connection.Id, Action, revision, User, tenantAccessor.TenantId, confirmOverride, revokeActiveSessions, cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }

        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, effective.Scope, ConnectionValidity.Unknown, false, "database"), management, adapters, null, cancellationToken), cancellationToken);
    }
}

internal sealed class EnableConnection(IdentityProviderConnectionManagementService management, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ConnectionLifecycleEndpoint(management, adapters, tenantAccessor)
{
    protected override ConnectionLifecycle Action => ConnectionLifecycle.Enabled;
    protected override string Verb => CoreVerbs.Update;
    protected override void ConfigureRoute() => Post("/external-authentication/connections/{connectionId}/enable");
}

internal sealed class DisableConnection(IdentityProviderConnectionManagementService management, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ConnectionLifecycleEndpoint(management, adapters, tenantAccessor)
{
    protected override ConnectionLifecycle Action => ConnectionLifecycle.Disabled;
    protected override string Verb => CoreVerbs.Update;
    protected override void ConfigureRoute() => Post("/external-authentication/connections/{connectionId}/disable");
}

internal sealed class ArchiveConnection(IdentityProviderConnectionManagementService management, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ConnectionLifecycleEndpoint(management, adapters, tenantAccessor)
{
    protected override ConnectionLifecycle Action => ConnectionLifecycle.Archived;
    protected override string Verb => "archive";
    protected override void ConfigureRoute() => Delete("/external-authentication/connections/{connectionId}");
}

internal sealed class RestoreConnection(IdentityProviderConnectionManagementService management, IExternalAuthenticationAdapterRegistry adapters, ITenantAccessor tenantAccessor) : ConnectionLifecycleEndpoint(management, adapters, tenantAccessor)
{
    protected override ConnectionLifecycle Action => ConnectionLifecycle.Draft;
    protected override string Verb => "archive";
    protected override void ConfigureRoute() => Post("/external-authentication/connections/{connectionId}/restore");
}

internal sealed class ValidateConnection(IdentityProviderConnectionManagementService management, ITenantAccessor tenantAccessor) : ElsaEndpointWithoutRequest
{
    public override void Configure()
    {
        Post("/external-authentication/connections/{connectionId}/validate");
        RequirePermission(Elsa.ExternalAuthentication.Permissions.ExternalAuthenticationResourcePermissions.Connections, CoreVerbs.View);
    }

    public override async Task HandleAsync(CancellationToken cancellationToken)
    {
        var result = await management.FindAsync(Route<string>("connectionId")!, tenantAccessor.TenantId, cancellationToken);
        if (result is not ManagementConnectionLookupResult.Found(var connection))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The connection was not found.", cancellationToken);
            return;
        }

        var validation = await management.ValidateAsync(connection.Connection, User, tenantAccessor.TenantId, requireCompleteConfiguration: true, confirmUnsafeSettings: ConnectionEndpointSupport.HasPermission(User, ExternalAuthenticationPermissions.ProviderTrustUnsafe), cancellationToken: cancellationToken);
        await HttpContext.Response.WriteAsJsonAsync(new ConnectionValidationResponse(validation.IsValid, validation.Errors, validation.Warnings), cancellationToken);
    }
}

/// <summary>Stores a write-only secret through an installed managed secret writer.</summary>
internal sealed class ReplaceManagedSecretBinding(
    IdentityProviderConnectionManagementService management,
    IExternalAuthenticationAdapterRegistry adapters,
    IIdentityProviderConnectionStore store,
    IEnumerable<IManagedSecretBindingWriter> managedSecretBindingWriters,
    ITenantAccessor tenantAccessor,
    ILogger<ReplaceManagedSecretBinding> logger) : ElsaEndpoint<ManagedSecretBindingRequest>
{
    private readonly IReadOnlyDictionary<string, IManagedSecretBindingWriter> _writers = managedSecretBindingWriters.ToDictionary(x => x.ResolverType, StringComparer.Ordinal);

    public override void Configure()
    {
        Put("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}/managed");
        RequirePermission(Elsa.ExternalAuthentication.Permissions.ExternalAuthenticationResourcePermissions.Connections, CoreVerbs.Update);
    }

    public override async Task HandleAsync(ManagedSecretBindingRequest request, CancellationToken cancellationToken)
    {
        if (!ConnectionEndpointSupport.TryGetExpectedRevision(HttpContext, out var revision))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status428PreconditionRequired, "precondition_required", "If-Match with the current connection revision is required.", cancellationToken);
            return;
        }

        var fieldName = Route<string>("fieldName")!;
        if (string.IsNullOrWhiteSpace(request.Value) || string.IsNullOrWhiteSpace(request.ResolverType) || !_writers.TryGetValue(request.ResolverType, out var writer))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status400BadRequest, "invalid_managed_secret", "A non-empty value and an installed managed secret resolver type are required.", cancellationToken);
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
        if (effective.Connection.Revision != revision)
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status412PreconditionFailed, "revision_conflict", "The connection has changed; reload it before replacing its secret.", cancellationToken);
            return;
        }
        if (!adapters.TryGet(effective.Connection.AdapterType, out var adapter) || !adapter.Describe().Fields.Any(x => x.IsSecretBinding && string.Equals(x.Name, fieldName, StringComparison.Ordinal)))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status400BadRequest, "undeclared_secret_field", "The adapter does not declare this secret field.", cancellationToken);
            return;
        }

        using var value = new SensitiveString(request.Value);
        var stagedBinding = await writer.StageAsync(new ManagedSecretBindingWriteRequest(effective.Connection.Id, fieldName, value), cancellationToken);
        if (effective.Connection.SecretBindings.TryGetValue(fieldName, out var liveBinding) &&
            string.Equals(liveBinding.ResolverType, stagedBinding.ResolverType, StringComparison.Ordinal) &&
            string.Equals(liveBinding.Reference, stagedBinding.Reference, StringComparison.Ordinal))
            throw new InvalidOperationException("A managed secret writer returned the live secret reference instead of a fresh staged reference.");
        var candidate = IdentityProviderConnectionCloner.Clone(effective.Connection);
        candidate.SecretBindings[fieldName] = stagedBinding;
        ManagementConnectionMutationResult result;
        try
        {
            result = await management.UpdateAsync(candidate.Id, candidate, revision, User, tenantAccessor.TenantId, false, cancellationToken: cancellationToken);
        }
        catch
        {
            await CleanupAfterExceptionalFailureAsync();
            throw;
        }
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ManagedSecretBindingCleanup.TryRemoveAsync(writer, stagedBinding, effective.Connection.Id, logger);
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }

        if (effective.Connection.SecretBindings.TryGetValue(fieldName, out var previousBinding) &&
            previousBinding.Ownership == SecretBindingOwnership.Managed &&
            _writers.TryGetValue(previousBinding.ResolverType, out var previousWriter))
            await ManagedSecretBindingCleanup.TryRemoveAsync(previousWriter, previousBinding, effective.Connection.Id, logger);

        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, effective.Scope, ConnectionValidity.Unknown, false, "database"), management, adapters, null, cancellationToken), cancellationToken);

        return;

        async ValueTask CleanupAfterExceptionalFailureAsync()
        {
            try
            {
                var persisted = await store.FindByIdAsync(effective.Connection.Id, CancellationToken.None);
                var stagedBindingWasPublished = persisted?.SecretBindings.TryGetValue(fieldName, out var publishedBinding) == true &&
                    string.Equals(publishedBinding.ResolverType, stagedBinding.ResolverType, StringComparison.Ordinal) &&
                    string.Equals(publishedBinding.Reference, stagedBinding.Reference, StringComparison.Ordinal);
                if (!stagedBindingWasPublished)
                    await ManagedSecretBindingCleanup.TryRemoveAsync(writer, stagedBinding, effective.Connection.Id, logger);
            }
            catch (Exception verificationException)
            {
                logger.LogWarning(
                    verificationException,
                    "Could not verify whether staged external-authentication secret material was published for connection {ConnectionId}; it was retained to avoid deleting live material.",
                    effective.Connection.Id);
            }
        }
    }
}

internal sealed class RemoveSecretBinding(
    IdentityProviderConnectionManagementService management,
    IExternalAuthenticationAdapterRegistry adapters,
    IEnumerable<IManagedSecretBindingWriter> managedSecretBindingWriters,
    ITenantAccessor tenantAccessor,
    ILogger<RemoveSecretBinding> logger) : ElsaEndpointWithoutRequest
{
    private readonly IReadOnlyDictionary<string, IManagedSecretBindingWriter> _writers = managedSecretBindingWriters.ToDictionary(x => x.ResolverType, StringComparer.Ordinal);

    public override void Configure()
    {
        Delete("/external-authentication/connections/{connectionId}/secret-bindings/{fieldName}");
        RequirePermission(Elsa.ExternalAuthentication.Permissions.ExternalAuthenticationResourcePermissions.Connections, CoreVerbs.Update);
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
        var fieldName = Route<string>("fieldName")!;
        if (!effective.Connection.SecretBindings.TryGetValue(fieldName, out var existingBinding))
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status404NotFound, "not_found", "The secret binding was not found.", cancellationToken);
            return;
        }
        if (existingBinding.Ownership != SecretBindingOwnership.Managed)
        {
            await ConnectionEndpointSupport.SendErrorAsync(HttpContext, StatusCodes.Status403Forbidden, "external_secret_binding_read_only", "Deployment-owned external secret bindings cannot be removed through this API.", cancellationToken);
            return;
        }
        var candidate = Elsa.ExternalAuthentication.Services.IdentityProviderConnectionCloner.Clone(effective.Connection);
        candidate.SecretBindings.Remove(fieldName);
        var result = await management.UpdateAsync(candidate.Id, candidate, revision, User, tenantAccessor.TenantId, false, cancellationToken: cancellationToken);
        if (result is not ManagementConnectionMutationResult.Success(var connection))
        {
            await ConnectionEndpointSupport.SendMutationResultAsync(HttpContext, result, management, cancellationToken);
            return;
        }
        if (_writers.TryGetValue(existingBinding.ResolverType, out var writer))
            await ManagedSecretBindingCleanup.TryRemoveAsync(writer, existingBinding, effective.Connection.Id, logger);
        ConnectionEndpointSupport.SetEtag(HttpContext, connection.Revision);
        await HttpContext.Response.WriteAsJsonAsync(await ConnectionResponse.FromAsync(new EffectiveIdentityProviderConnection(connection, ConnectionSourceOwnership.Database, effective.Scope, ConnectionValidity.Unknown, false, "database"), management, adapters, null, cancellationToken), cancellationToken);
    }
}

internal static class ManagedSecretBindingCleanup
{
    public static async ValueTask TryRemoveAsync(IManagedSecretBindingWriter writer, SecretBinding binding, string connectionId, ILogger logger)
    {
        try
        {
            await writer.RemoveAsync(binding, CancellationToken.None);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to remove detached external-authentication secret material for connection {ConnectionId}.", connectionId);
        }
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
