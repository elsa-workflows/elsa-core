using System.Security.Cryptography;
using System.Text;
using Elsa.Common;
using Elsa.Extensions;
using Elsa.ExternalAuthentication.Contracts;
using Elsa.ExternalAuthentication.Models;
using Elsa.ExternalAuthentication.Options;
using Elsa.ExternalAuthentication.Notifications;
using Elsa.ExternalAuthentication.Validation;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.DataProtection;

namespace Elsa.ExternalAuthentication.Services;

public sealed class ExternalAuthenticationBroker(
    IIdentityProviderConnectionRegistry connectionRegistry,
    IEnumerable<IExternalAuthenticationAdapter> adapters,
    IEnumerable<ISecretBindingResolver> secretBindingResolvers,
    IExternalAuthenticationHandleHasher handleHasher,
    IDataProtectionProvider dataProtectionProvider,
    IExternalIdentityResolver identityResolver,
    IPermissionGrantResolver permissionGrantResolver,
    IExternalAuthenticationStateStore stateStore,
    IAuthorizationGrantStore grantStore,
    IExternalAuthenticationSessionStore sessionStore,
    IExternalAuthenticationTokenIssuer tokenIssuer,
    IUserCredentialsValidator credentialsValidator,
    IUserProvider userProvider,
    IRoleProvider roleProvider,
    IElsaTokenService elsaTokenService,
    ISystemClock clock,
    IOptions<ExternalAuthenticationOptions> options,
    ExternalAuthenticationSecurityNotifier? notifier = null) : IExternalAuthenticationBroker
{
    private ValueTask RecordOutcomeAsync(string flow, string stage, SecurityEventOutcome outcome, BrokerErrorCategory? category, string? tenantId, string? connectionId, string? userId, CancellationToken cancellationToken) =>
        RecordOutcomeCategoryAsync(flow, stage, outcome, category is { } value ? BrokerErrorFactory.Create(value).Error : "success", tenantId, connectionId, userId, cancellationToken);
    private ValueTask RecordOutcomeCategoryAsync(string flow, string stage, SecurityEventOutcome outcome, string category, string? tenantId, string? connectionId, string? userId, CancellationToken cancellationToken) =>
        notifier is null ? ValueTask.CompletedTask : notifier.PublishAsync(new ExternalAuthenticationOutcomeRecorded(
            ExternalAuthenticationSecurityNotifier.Context(null, tenantId, connectionId, userId, outcome, "External authentication broker operation completed."), flow, stage, category), cancellationToken);
    private async ValueTask<BrokerCallbackResult> CallbackOutcomeAsync(BrokerCallbackResult result, string flow, string stage, string? tenantId, string? connectionId, CancellationToken cancellationToken)
    {
        await RecordOutcomeCategoryAsync(flow, stage, result.Error is null ? SecurityEventOutcome.Succeeded : SecurityEventOutcome.Rejected, result.Error?.Error ?? "success", tenantId, connectionId, null, cancellationToken);
        return result;
    }
    private async ValueTask<BrokerTokenResult> TokenOutcomeAsync(BrokerTokenResult result, string flow, string stage, CancellationToken cancellationToken)
    {
        await RecordOutcomeCategoryAsync(flow, stage, result.Error is null ? SecurityEventOutcome.Succeeded : SecurityEventOutcome.Rejected, result.Error?.Error ?? "success", null, null, null, cancellationToken);
        return result;
    }
    private async ValueTask<BrokerLogoutResult> LogoutOutcomeAsync(BrokerLogoutResult result, string stage, CancellationToken cancellationToken)
    {
        await RecordOutcomeCategoryAsync("logout", stage, result.Error is null ? SecurityEventOutcome.Succeeded : SecurityEventOutcome.Rejected, result.Error?.Error ?? "success", null, null, null, cancellationToken);
        return result;
    }

    public async ValueTask<IReadOnlyCollection<LoginMethod>> DiscoverAsync(string targetTenantId, string clientId, CancellationToken cancellationToken = default)
    {
        EnsureClient(clientId);
        var externalMethods = (await connectionRegistry.GetAsync(targetTenantId, cancellationToken)).LoginMethods;
        var localOptions = options.Value.LocalLogin;
        if (!localOptions.IsEnabled)
            return externalMethods;

        var local = new LoginMethod("local", "local", LoginMethodKind.Local, localOptions.DisplayName, localOptions.IconId, localOptions.DisplayOrder, localOptions.IsDefault && !externalMethods.Any(x => x.IsDefault), new Uri("/external-authentication/local/authorize", UriKind.Relative));
        return [local, .. externalMethods];
    }

    public async ValueTask<BrokerInitiationResult> InitiateExternalAsync(BrokerAuthorizationRequest request, string targetTenantId, CancellationToken cancellationToken = default)
    {
        AuthenticationClient client;
        try
        {
            client = EnsureClient(request.ClientId);
            ValidateAuthorizationRequest(request, client);
        }
        catch (InvalidOperationException)
        {
            var error = BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest);
            await RecordOutcomeAsync("external", "initiate", SecurityEventOutcome.Rejected, BrokerErrorCategory.InvalidRequest, targetTenantId, null, null, cancellationToken);
            return BrokerInitiationResult.Fail(error);
        }
        var connection = await connectionRegistry.FindByKeyAsync(targetTenantId, request.ConnectionKey, cancellationToken);
        if (connection is null || connection.IsShadowed || !connection.Connection.IsEnabled || connection.Connection.ArchivedAt != null)
        {
            var error = BrokerErrorFactory.Create(BrokerErrorCategory.MethodUnavailable);
            await RecordOutcomeAsync("external", "initiate", SecurityEventOutcome.Rejected, BrokerErrorCategory.MethodUnavailable, targetTenantId, null, null, cancellationToken);
            return BrokerInitiationResult.Fail(error);
        }

        var adapter = adapters.FirstOrDefault(x => string.Equals(x.Type, connection.Connection.AdapterType, StringComparison.Ordinal));
        if (adapter is null)
        {
            var error = BrokerErrorFactory.Create(BrokerErrorCategory.MethodUnavailable);
            await RecordOutcomeAsync("external", "initiate", SecurityEventOutcome.Rejected, BrokerErrorCategory.MethodUnavailable, targetTenantId, connection.Connection.Id, null, cancellationToken);
            return BrokerInitiationResult.Fail(error);
        }

        var state = CreateOpaqueValue();
        var transaction = new BrokerTransaction
        {
            HandleHash = Hash(state),
            Purpose = BrokerTransactionPurpose.ExternalSignIn,
            ClientId = request.ClientId,
            CallbackUri = request.RedirectUri,
            ReturnPath = request.ReturnPath,
            ClientState = request.ClientState,
            TenantId = targetTenantId,
            ConnectionId = connection.Connection.Id,
            ConnectionMaterialRevision = connection.Connection.MaterialRevision,
            PkceChallenge = request.CodeChallenge,
            ExpiresAt = clock.UtcNow.Add(options.Value.Lifetimes.BrokerTransactionLifetime)
        };
        IReadOnlyDictionary<string, ResolvedSecretBinding> secrets = new Dictionary<string, ResolvedSecretBinding>();
        ExternalAuthorizationRequest adapterRequest;
        try
        {
            secrets = await ResolveSecretsAsync(connection.Connection.SecretBindings, cancellationToken);
            transaction.SecretGenerationFingerprint = GetSecretFingerprint(secrets);
            adapterRequest = await adapter.CreateAuthorizationRequestAsync(new ExternalAuthorizationContext(connection, secrets, transaction, state, clock), cancellationToken);
            transaction.ProtectedPayload = dataProtectionProvider.CreateProtector("Elsa.ExternalAuthentication.AdapterPayload.v1").Protect(adapterRequest.ProtectedAdapterState);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            var error = BrokerErrorFactory.Create(BrokerErrorCategory.TemporarilyUnavailable);
            await RecordOutcomeAsync("external", "initiate", SecurityEventOutcome.Failed, BrokerErrorCategory.TemporarilyUnavailable, targetTenantId, connection.Connection.Id, null, cancellationToken);
            return BrokerInitiationResult.Fail(error);
        }
        finally
        {
            DisposeSecrets(secrets);
        }
        await stateStore.PutAsync(BrokerTransactionPurpose.ExternalSignIn.ToString(), transaction.HandleHash, transaction, transaction.ExpiresAt, cancellationToken);
        await RecordOutcomeAsync("external", "initiate", SecurityEventOutcome.Succeeded, null, targetTenantId, connection.Connection.Id, null, cancellationToken);
        return BrokerInitiationResult.Redirect(adapterRequest.NavigationUri);
    }

    public async ValueTask<BrokerCallbackResult> CompleteCallbackAsync(string connectionId, string state, IReadOnlyDictionary<string, IReadOnlyCollection<string>> parameters, CancellationToken cancellationToken = default)
    {
        var taken = await stateStore.TryTakeAsync<BrokerTransaction>(BrokerTransactionPurpose.ExternalSignIn.ToString(), Hash(state), cancellationToken);
        if (taken is not TakeResult<BrokerTransaction>.Taken { Value: var transaction })
            return await CallbackOutcomeAsync(BrokerCallbackResult.Fail(BrokerErrorFactory.Create(taken is TakeResult<BrokerTransaction>.Expired ? BrokerErrorCategory.FlowExpired : BrokerErrorCategory.InvalidRequest)), "external", "callback", null, connectionId, cancellationToken);
        if (!string.Equals(transaction.ConnectionId, connectionId, StringComparison.Ordinal))
            return await FailTrustedCallbackAsync(transaction, BrokerErrorCategory.InvalidRequest, cancellationToken);

        var connection = await connectionRegistry.FindByIdAsync(transaction.TenantId, connectionId, cancellationToken);
        if (connection is null || connection.IsShadowed || !connection.Connection.IsEnabled || connection.Connection.ArchivedAt != null)
            return await FailTrustedCallbackAsync(transaction, BrokerErrorCategory.MethodUnavailable, cancellationToken);
        if (!string.Equals(connection.Connection.MaterialRevision, transaction.ConnectionMaterialRevision, StringComparison.Ordinal))
            return await FailTrustedCallbackAsync(transaction, BrokerErrorCategory.FlowChanged, cancellationToken);

        var adapter = adapters.FirstOrDefault(x => string.Equals(x.Type, connection.Connection.AdapterType, StringComparison.Ordinal));
        if (adapter is null)
            return await FailTrustedCallbackAsync(transaction, BrokerErrorCategory.MethodUnavailable, cancellationToken);

        try
        {
            var secrets = await ResolveSecretsAsync(connection.Connection.SecretBindings, cancellationToken);
            try
            {
                if (!string.Equals(transaction.SecretGenerationFingerprint, GetSecretFingerprint(secrets), StringComparison.Ordinal))
                    return await FailTrustedCallbackAsync(transaction, BrokerErrorCategory.FlowChanged, cancellationToken);

                var originalPayload = transaction.ProtectedPayload;
                transaction.ProtectedPayload = dataProtectionProvider.CreateProtector("Elsa.ExternalAuthentication.AdapterPayload.v1").Unprotect(originalPayload);
                ExternalAuthenticationResult authentication;
                try
                {
                    authentication = await adapter.AuthenticateCallbackAsync(new ExternalCallbackContext(connection, secrets, transaction, state, parameters, clock), cancellationToken);
                }
                finally
                {
                    transaction.ProtectedPayload = originalPayload;
                }

                var resolution = await identityResolver.ResolveAsync(new ExternalIdentityResolutionContext(transaction.TenantId, connection, authentication.Identity, authentication.ProjectedClaims), cancellationToken);
                var grantResult = await permissionGrantResolver.ResolveAsync(new PermissionGrantResolutionContext(
                    transaction.TenantId,
                    resolution.UserId,
                    connection,
                    authentication.Identity,
                    authentication.ProjectedClaims), cancellationToken);
                var session = new ExternalAuthenticationSession
                {
                    Id = CreateOpaqueValue(),
                    AuthenticationClientId = transaction.ClientId,
                    TenantId = transaction.TenantId,
                    UserId = resolution.UserId,
                    ConnectionId = connection.Connection.Id,
                    ConnectionMaterialRevision = connection.Connection.MaterialRevision,
                    SecretGenerationFingerprint = transaction.SecretGenerationFingerprint,
                    Issuer = authentication.Identity.Issuer,
                    SubjectHash = Hash(authentication.Identity.Subject),
                    ExternalGrants = grantResult.Grants,
                    StartedAt = clock.UtcNow,
                    LastRefreshedAt = clock.UtcNow,
                    ExpiresAt = clock.UtcNow.Add(options.Value.Lifetimes.MaximumSessionAge),
                    RefreshExpiresAt = clock.UtcNow.Add(options.Value.Lifetimes.MaximumSessionAge)
                };
                await sessionStore.SaveAsync(session, cancellationToken);
                var code = CreateOpaqueValue();
                await grantStore.SaveAsync(new AuthorizationGrant { CodeHash = Hash(code), ClientId = transaction.ClientId, CallbackUri = transaction.CallbackUri, TenantId = transaction.TenantId, UserId = resolution.UserId, ExternalSessionId = session.Id, PkceChallenge = transaction.PkceChallenge, ExpiresAt = clock.UtcNow.Add(options.Value.Lifetimes.CompletionCodeLifetime) }, cancellationToken);
                if (notifier is not null)
                    await notifier.PublishAsync(new ExternalSignInCompleted(
                    ExternalAuthenticationSecurityNotifier.Context(null, transaction.TenantId, connection.Connection.Id, resolution.UserId, SecurityEventOutcome.Succeeded, "External sign-in completed."),
                    session.Id,
                    connection.Connection.AdapterType), cancellationToken);
                return await CallbackOutcomeAsync(BrokerCallbackResult.Redirect(AppendCallbackParameters(transaction.CallbackUri, code, transaction.ClientState)), "external", "callback", transaction.TenantId, connection.Connection.Id, cancellationToken);
            }
            finally
            {
                DisposeSecrets(secrets);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return await FailTrustedCallbackAsync(transaction, BrokerErrorCategory.AuthenticationFailed, cancellationToken);
        }
    }

    public async ValueTask<BrokerCallbackResult> InitiateLocalAsync(LocalBrokerAuthorizationRequest request, string targetTenantId, CancellationToken cancellationToken = default)
    {
        AuthenticationClient client;
        try
        {
            client = EnsureClient(request.ClientId);
            ValidateAuthorizationRequest(request, client);
        }
        catch (InvalidOperationException)
        {
            return await CallbackOutcomeAsync(BrokerCallbackResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "local", "initiate", targetTenantId, null, cancellationToken);
        }
        if (!options.Value.LocalLogin.IsEnabled)
            return await CallbackOutcomeAsync(BrokerCallbackResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.MethodUnavailable)), "local", "initiate", targetTenantId, null, cancellationToken);
        Elsa.Identity.Entities.User? user;
        try
        {
            user = await credentialsValidator.ValidateAsync(request.Username.Trim(), request.Password, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return await CallbackOutcomeAsync(BrokerCallbackResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.ServerError)), "local", "initiate", targetTenantId, null, cancellationToken);
        }
        if (user is null || !string.Equals(user.TenantId, targetTenantId, StringComparison.Ordinal))
            return await CallbackOutcomeAsync(BrokerCallbackResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.AuthenticationFailed)), "local", "initiate", targetTenantId, null, cancellationToken);

        var code = CreateOpaqueValue();
        await grantStore.SaveAsync(new AuthorizationGrant { CodeHash = Hash(code), ClientId = request.ClientId, CallbackUri = request.RedirectUri, TenantId = targetTenantId, UserId = user.Id, PkceChallenge = request.CodeChallenge, ExpiresAt = clock.UtcNow.Add(options.Value.Lifetimes.CompletionCodeLifetime) }, cancellationToken);
        return await CallbackOutcomeAsync(BrokerCallbackResult.Redirect(AppendCallbackParameters(request.RedirectUri, code, request.ClientState)), "local", "initiate", targetTenantId, null, cancellationToken);
    }

    public async ValueTask<BrokerTokenResult> ExchangeAsync(BrokerTokenRequest request, CancellationToken cancellationToken = default)
    {
        AuthenticationClient client;
        try
        {
            client = EnsureClient(request.ClientId);
            await ValidateExchangeClientAsync(client, request, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "token_exchange", "validate_client", cancellationToken);
        }

        if (!string.Equals(request.GrantType, "refresh_token", StringComparison.Ordinal) && !string.Equals(request.GrantType, "authorization_code", StringComparison.Ordinal))
            return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "token_exchange", "validate_grant", cancellationToken);
        if (string.Equals(request.GrantType, "refresh_token", StringComparison.Ordinal))
        {
            using var refreshToken = new SensitiveString(request.RefreshToken ?? string.Empty);
            try { return await TokenOutcomeAsync(BrokerTokenResult.Success(await tokenIssuer.RefreshAsync(request.ClientId, refreshToken, cancellationToken)), "refresh", "exchange", cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch { return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.AccessDenied)), "refresh", "exchange", cancellationToken); }
        }

        var grantResult = await grantStore.TryTakeAsync(Hash(request.Code ?? string.Empty), cancellationToken);
        if (grantResult is not TakeResult<AuthorizationGrant>.Taken { Value: var grant })
            return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "token_exchange", "consume_code", cancellationToken);
        if (!string.Equals(grant.ClientId, request.ClientId, StringComparison.Ordinal) || request.RedirectUri != grant.CallbackUri || !VerifyPkce(grant.PkceChallenge, request.CodeVerifier))
            return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "token_exchange", "validate_code", cancellationToken);

        if (grant.ExternalSessionId is { } sessionId)
        {
            var session = await sessionStore.FindByIdAsync(sessionId, cancellationToken);
            if (session is null || session.RevokedAt != null || session.ExpiresAt <= clock.UtcNow || !string.Equals(session.AuthenticationClientId, client.ClientId, StringComparison.Ordinal))
                return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.AccessDenied)), "token_exchange", "validate_session", cancellationToken);
            var connection = await connectionRegistry.FindByIdAsync(session.TenantId, session.ConnectionId, cancellationToken);
            if (connection is null || connection.IsShadowed || !connection.Connection.IsEnabled || connection.Connection.ArchivedAt != null || !string.Equals(connection.Connection.MaterialRevision, session.ConnectionMaterialRevision, StringComparison.Ordinal))
                return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.AccessDenied)), "token_exchange", "validate_connection", cancellationToken);
            try
            {
                return await TokenOutcomeAsync(BrokerTokenResult.Success(await tokenIssuer.IssueAsync(session, cancellationToken)), "token_exchange", "issue", cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
                return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.ServerError)), "token_exchange", "issue", cancellationToken);
            }
        }

        var user = await userProvider.FindAsync(new UserFilter { Id = grant.UserId }, cancellationToken);
        if (user is null)
            return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.AccessDenied)), "token_exchange", "resolve_user", cancellationToken);
        try
        {
            var roles = (await roleProvider.FindByIdsAsync(user.Roles, cancellationToken)).ToArray();
            var context = new TokenIssuanceContext(user, roles.Select(x => x.Name).ToArray(), roles.SelectMany(x => x.Permissions).Distinct().ToArray(), []);
            var access = await elsaTokenService.IssueAccessTokenAsync(context, cancellationToken);
            var refresh = await elsaTokenService.IssueRefreshTokenAsync(context, cancellationToken);
            return await TokenOutcomeAsync(BrokerTokenResult.Success(new ExternalTokenResponse(access.Token, "Bearer", (long)(access.ExpiresAt - clock.UtcNow).TotalSeconds, refresh.Token, 0, 0)), "token_exchange", "issue", cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return await TokenOutcomeAsync(BrokerTokenResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.ServerError)), "token_exchange", "issue", cancellationToken);
        }
    }

    public async ValueTask<BrokerLogoutResult> LogoutAsync(BrokerLogoutRequest request, string externalSessionId, CancellationToken cancellationToken = default)
    {
        AuthenticationClient client;
        try
        {
            client = EnsureClient(request.ClientId);
            if (!client.LogoutCallbackUris.Contains(request.PostLogoutRedirectUri))
                throw new InvalidOperationException();
        }
        catch (InvalidOperationException)
        {
            return await LogoutOutcomeAsync(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "validate", cancellationToken);
        }
        if (!string.Equals(request.Mode, "local", StringComparison.OrdinalIgnoreCase) && !string.Equals(request.Mode, "upstream", StringComparison.OrdinalIgnoreCase))
            return await LogoutOutcomeAsync(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "validate", cancellationToken);

        var session = await sessionStore.FindByIdAsync(externalSessionId, cancellationToken);
        if (session is null || !string.Equals(session.AuthenticationClientId, request.ClientId, StringComparison.Ordinal))
            return await LogoutOutcomeAsync(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.AccessDenied)), "session", cancellationToken);

        var revoked = await sessionStore.RevokeAsync(session.Id, "logout", clock.UtcNow, cancellationToken);
        if (revoked && notifier is not null)
            await notifier.PublishAsync(new ExternalAuthenticationSessionRevoked(
                ExternalAuthenticationSecurityNotifier.Context(null, session.TenantId, session.ConnectionId, session.UserId, SecurityEventOutcome.Succeeded, "External authentication session revoked."),
                session.Id,
                "logout"), cancellationToken);
        var connection = await connectionRegistry.FindByIdAsync(session.TenantId, session.ConnectionId, cancellationToken);
        var wantsUpstream = string.Equals(request.Mode, "upstream", StringComparison.OrdinalIgnoreCase)
            || connection?.Connection.UpstreamLogoutMode == UpstreamLogoutMode.Always;
        if (!wantsUpstream)
            return await LogoutOutcomeAsync(BrokerLogoutResult.Complete(request.PostLogoutRedirectUri), "complete", cancellationToken);
        if (connection is null || connection.Connection.UpstreamLogoutMode == UpstreamLogoutMode.Disabled)
            return await LogoutOutcomeAsync(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "upstream", cancellationToken);

        var adapter = adapters.FirstOrDefault(x => string.Equals(x.Type, connection.Connection.AdapterType, StringComparison.Ordinal));
        if (adapter is null)
            return await LogoutOutcomeAsync(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.MethodUnavailable)), "upstream", cancellationToken);

        var state = CreateOpaqueValue();
        var transaction = new BrokerTransaction
        {
            HandleHash = Hash(state),
            Purpose = BrokerTransactionPurpose.UpstreamLogout,
            ClientId = request.ClientId,
            CallbackUri = request.PostLogoutRedirectUri,
            ReturnPath = "/",
            TenantId = session.TenantId,
            ConnectionId = session.ConnectionId,
            ConnectionMaterialRevision = session.ConnectionMaterialRevision,
            PkceChallenge = string.Empty,
            ExpiresAt = clock.UtcNow.Add(options.Value.Lifetimes.BrokerTransactionLifetime)
        };
        IReadOnlyDictionary<string, ResolvedSecretBinding> logoutSecrets = new Dictionary<string, ResolvedSecretBinding>();
        ExternalLogoutRequest? upstream;
        try
        {
            logoutSecrets = await ResolveSecretsAsync(connection.Connection.SecretBindings, cancellationToken);
            upstream = await adapter.CreateLogoutRequestAsync(new ExternalLogoutContext(connection, logoutSecrets, transaction, state, clock), cancellationToken);
            transaction.ProtectedPayload = dataProtectionProvider.CreateProtector("Elsa.ExternalAuthentication.AdapterPayload.v1").Protect(upstream?.ProtectedAdapterState ?? []);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return await LogoutOutcomeAsync(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.TemporarilyUnavailable)), "upstream", cancellationToken);
        }
        finally
        {
            DisposeSecrets(logoutSecrets);
        }
        if (upstream is null)
            return await LogoutOutcomeAsync(BrokerLogoutResult.Complete(request.PostLogoutRedirectUri), "upstream", cancellationToken);

        await stateStore.PutAsync(BrokerTransactionPurpose.UpstreamLogout.ToString(), transaction.HandleHash, transaction, transaction.ExpiresAt, cancellationToken);
        var continuationHandle = CreateOpaqueValue();
        await stateStore.PutAsync("UpstreamLogoutContinue", Hash(continuationHandle), new BrokerTransaction
        {
            HandleHash = Hash(continuationHandle), Purpose = BrokerTransactionPurpose.UpstreamLogout, ClientId = request.ClientId,
            CallbackUri = request.PostLogoutRedirectUri, ReturnPath = upstream.NavigationUri.AbsoluteUri, TenantId = session.TenantId,
            ExpiresAt = transaction.ExpiresAt
        }, transaction.ExpiresAt, cancellationToken);
        return await LogoutOutcomeAsync(BrokerLogoutResult.Navigate(new Uri($"/external-authentication/logout/continue/{Uri.EscapeDataString(continuationHandle)}", UriKind.Relative)), "initiate", cancellationToken);
    }

    public async ValueTask<BrokerCallbackResult> CompleteLogoutAsync(string connectionId, string state, CancellationToken cancellationToken = default)
    {
        var taken = await stateStore.TryTakeAsync<BrokerTransaction>(BrokerTransactionPurpose.UpstreamLogout.ToString(), Hash(state), cancellationToken);
        if (taken is not TakeResult<BrokerTransaction>.Taken { Value: var transaction })
            return await CallbackOutcomeAsync(BrokerCallbackResult.Fail(BrokerErrorFactory.Create(taken is TakeResult<BrokerTransaction>.Expired ? BrokerErrorCategory.FlowExpired : BrokerErrorCategory.InvalidRequest)), "logout", "callback", null, connectionId, cancellationToken);
        if (!string.Equals(transaction.ConnectionId, connectionId, StringComparison.Ordinal))
            return await CallbackOutcomeAsync(BrokerCallbackResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "logout", "callback", transaction.TenantId, connectionId, cancellationToken);
        var connection = await connectionRegistry.FindByIdAsync(transaction.TenantId, connectionId, cancellationToken);
        return connection is null || connection.IsShadowed || !connection.Connection.IsEnabled || connection.Connection.ArchivedAt is not null || !string.Equals(connection.Connection.MaterialRevision, transaction.ConnectionMaterialRevision, StringComparison.Ordinal)
            ? await CallbackOutcomeAsync(BrokerCallbackResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.FlowChanged)), "logout", "callback", transaction.TenantId, connectionId, cancellationToken)
            : await CallbackOutcomeAsync(BrokerCallbackResult.Redirect(transaction.CallbackUri), "logout", "callback", transaction.TenantId, connectionId, cancellationToken);
    }

    public async ValueTask<BrokerLogoutResult> ContinueLogoutAsync(string handle, CancellationToken cancellationToken = default)
    {
        var taken = await stateStore.TryTakeAsync<BrokerTransaction>("UpstreamLogoutContinue", Hash(handle), cancellationToken);
        if (taken is not TakeResult<BrokerTransaction>.Taken { Value: var transaction } || !Uri.TryCreate(transaction.ReturnPath, UriKind.Absolute, out var navigation))
            return await LogoutOutcomeAsync(BrokerLogoutResult.Fail(BrokerErrorFactory.Create(BrokerErrorCategory.InvalidRequest)), "continue", cancellationToken);
        return await LogoutOutcomeAsync(BrokerLogoutResult.Navigate(navigation), "continue", cancellationToken);
    }

    private AuthenticationClient EnsureClient(string clientId) => options.Value.Clients.FirstOrDefault(x => x.IsEnabled && string.Equals(x.ClientId, clientId, StringComparison.Ordinal)) ?? throw new InvalidOperationException("Authentication client is unavailable.");
    private static void ValidateAuthorizationRequest(BrokerAuthorizationRequest request, AuthenticationClient client)
    {
        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal) || !string.Equals(request.CodeChallengeMethod, "S256", StringComparison.Ordinal) || !client.CallbackUris.Contains(request.RedirectUri) || !ClientReturnPathValidator.TryValidateForClient(request.ReturnPath, client.AllowedReturnPathPrefixes, out _))
            throw new InvalidOperationException("The broker authorization request is invalid.");
    }
    private static bool VerifyPkce(string challenge, string? verifier) => !string.IsNullOrWhiteSpace(verifier) && string.Equals(challenge, Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))), StringComparison.Ordinal);
    private static string CreateOpaqueValue() => Base64Url(RandomNumberGenerator.GetBytes(32));
    private string Hash(string value) => handleHasher.Hash(value);
    private static string Base64Url(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static Uri AppendCallbackParameters(Uri uri, string code, string? clientState)
    {
        var result = AppendQuery(uri, "code", code);
        return string.IsNullOrWhiteSpace(clientState) ? result : AppendQuery(result, "state", clientState);
    }
    private static Uri AppendQuery(Uri uri, string key, string value) { var separator = string.IsNullOrEmpty(uri.Query) ? "?" : "&"; return new Uri(uri + separator + Uri.EscapeDataString(key) + "=" + Uri.EscapeDataString(value), uri.IsAbsoluteUri ? UriKind.Absolute : UriKind.Relative); }
    private async ValueTask<BrokerCallbackResult> FailTrustedCallbackAsync(BrokerTransaction transaction, BrokerErrorCategory category, CancellationToken cancellationToken)
    {
        var error = BrokerErrorFactory.Create(category);
        return await CallbackOutcomeAsync(BrokerCallbackResult.Fail(error, AppendQuery(AppendQuery(transaction.CallbackUri, "error", error.Error), "correlation_id", error.CorrelationId)), "external", "callback", transaction.TenantId, transaction.ConnectionId, cancellationToken);
    }

    private async ValueTask<IReadOnlyDictionary<string, ResolvedSecretBinding>> ResolveSecretsAsync(IDictionary<string, SecretBinding> bindings, CancellationToken cancellationToken)
    {
        var resolved = new Dictionary<string, ResolvedSecretBinding>(StringComparer.Ordinal);
        try
        {
            foreach (var (name, binding) in bindings)
            {
                var resolver = secretBindingResolvers.FirstOrDefault(x => string.Equals(x.Type, binding.ResolverType, StringComparison.Ordinal))
                    ?? throw new InvalidOperationException("A required secret binding resolver is unavailable.");
                resolved[name] = await resolver.ResolveAsync(binding, cancellationToken);
            }
            return resolved;
        }
        catch
        {
            DisposeSecrets(resolved);
            throw;
        }
    }

    private static string GetSecretFingerprint(IReadOnlyDictionary<string, ResolvedSecretBinding> secrets) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("\n", secrets.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => $"{x.Key}:{x.Value.GenerationFingerprint}")))));
    private static void DisposeSecrets(IReadOnlyDictionary<string, ResolvedSecretBinding> secrets)
    {
        foreach (var secret in secrets.Values)
            secret.Value.Dispose();
    }

    private async ValueTask ValidateExchangeClientAsync(AuthenticationClient client, BrokerTokenRequest request, CancellationToken cancellationToken)
    {
        if (client.ClientType == AuthenticationClientType.Public)
        {
            if (string.IsNullOrWhiteSpace(request.Origin) || !client.AllowedOrigins.Contains(request.Origin) || !string.IsNullOrEmpty(request.ClientSecret))
                throw new InvalidOperationException();
            return;
        }

        if (client.SecretBinding is null || string.IsNullOrWhiteSpace(request.ClientSecret) || !string.Equals(client.ClientId, request.BasicClientId, StringComparison.Ordinal))
            throw new InvalidOperationException();
        var resolver = secretBindingResolvers.FirstOrDefault(x => string.Equals(x.Type, client.SecretBinding.ResolverType, StringComparison.Ordinal))
            ?? throw new InvalidOperationException();
        using var configured = (await resolver.ResolveAsync(client.SecretBinding, cancellationToken)).Value;
        using var supplied = new SensitiveString(request.ClientSecret);
        if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(configured.Reveal()), Encoding.UTF8.GetBytes(supplied.Reveal())))
            throw new InvalidOperationException();
    }
}

public interface IExternalAuthenticationBroker
{
    ValueTask<IReadOnlyCollection<LoginMethod>> DiscoverAsync(string targetTenantId, string clientId, CancellationToken cancellationToken = default);
    ValueTask<BrokerInitiationResult> InitiateExternalAsync(BrokerAuthorizationRequest request, string targetTenantId, CancellationToken cancellationToken = default);
    ValueTask<BrokerCallbackResult> CompleteCallbackAsync(string connectionId, string state, IReadOnlyDictionary<string, IReadOnlyCollection<string>> parameters, CancellationToken cancellationToken = default);
    ValueTask<BrokerCallbackResult> InitiateLocalAsync(LocalBrokerAuthorizationRequest request, string targetTenantId, CancellationToken cancellationToken = default);
    ValueTask<BrokerTokenResult> ExchangeAsync(BrokerTokenRequest request, CancellationToken cancellationToken = default);
    ValueTask<BrokerLogoutResult> LogoutAsync(BrokerLogoutRequest request, string externalSessionId, CancellationToken cancellationToken = default);
    ValueTask<BrokerLogoutResult> ContinueLogoutAsync(string handle, CancellationToken cancellationToken = default);
    ValueTask<BrokerCallbackResult> CompleteLogoutAsync(string connectionId, string state, CancellationToken cancellationToken = default);
}

public record BrokerAuthorizationRequest(string ClientId, Uri RedirectUri, string ResponseType, string CodeChallenge, string CodeChallengeMethod, string ReturnPath, string ConnectionKey, string? ClientState = null);
public sealed record LocalBrokerAuthorizationRequest(string ClientId, Uri RedirectUri, string ResponseType, string CodeChallenge, string CodeChallengeMethod, string ReturnPath, string Username, string Password, string? ClientState = null) : BrokerAuthorizationRequest(ClientId, RedirectUri, ResponseType, CodeChallenge, CodeChallengeMethod, ReturnPath, "local", ClientState);
public sealed record BrokerLogoutRequest(string ClientId, Uri PostLogoutRedirectUri, string Mode);
public sealed record BrokerInitiationResult(Uri? NavigationUri, PublicBrokerError? Error) { public static BrokerInitiationResult Redirect(Uri uri) => new(uri, null); public static BrokerInitiationResult Fail(PublicBrokerError error) => new(null, error); }
public sealed record BrokerCallbackResult(Uri? RedirectUri, PublicBrokerError? Error) { public static BrokerCallbackResult Redirect(Uri uri) => new(uri, null); public static BrokerCallbackResult Fail(PublicBrokerError error, Uri? redirectUri = null) => new(redirectUri, error); }
public sealed record BrokerTokenRequest(string GrantType, string ClientId, Uri? RedirectUri, string? Code, string? CodeVerifier, string? RefreshToken, string? Origin = null, string? BasicClientId = null, string? ClientSecret = null);
public sealed record BrokerTokenResult(ExternalTokenResponse? Token, PublicBrokerError? Error) { public static BrokerTokenResult Success(ExternalTokenResponse token) => new(token, null); public static BrokerTokenResult Fail(PublicBrokerError error) => new(null, error); }
public sealed record BrokerLogoutResult(bool Completed, Uri? NavigationUri, Uri? RedirectUri, PublicBrokerError? Error)
{
    public static BrokerLogoutResult Complete(Uri redirectUri) => new(true, null, redirectUri, null);
    public static BrokerLogoutResult Navigate(Uri navigationUri) => new(false, navigationUri, null, null);
    public static BrokerLogoutResult Fail(PublicBrokerError error) => new(false, null, null, error);
}
