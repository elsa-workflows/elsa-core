using System.Security.Claims;
using System.Text.Json;
using Elsa.Common.Multitenancy;
using Elsa.Connections.Contracts;
using Elsa.Connections.Models;
using Elsa.Connections.Services;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Elsa.Slack.SocketMode;
using Elsa.Slack.SocketMode.Credentials;
using Elsa.Slack.Tests.SocketMode;
using NSubstitute;

namespace Elsa.Slack.Tests.Credentials;

public sealed class SlackSocketCredentialTests
{
    [Theory]
    [InlineData(CredentialOperationStatus.None)]
    [InlineData(CredentialOperationStatus.Completed)]
    public async Task FixedListenerPurposeUsesCurrentGenerationAndRestoresTenant(CredentialOperationStatus operation)
    {
        var test = new CredentialFixture();
        test.Connection.OperationStatus = operation;
        using var outer = test.Tenants.PushContext(new Tenant { Id = "outer", Name = "outer" });
        var credential = await test.Reader.ResolveCurrentAsync(CancellationToken.None);
        Assert.Equal(ConnectionCredentialKind.ApiKey, credential.Kind);
        Assert.Equal(CredentialFixture.Token, credential.ApiKey);
        Assert.Equal("outer", test.Tenants.TenantId);
        Assert.DoesNotContain(CredentialFixture.Token, credential.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(CredentialFixture.Token, JsonSerializer.Serialize(credential), StringComparison.Ordinal);
        await test.Authorizer.Received(1).AuthorizeAsync(Arg.Is<ConnectionUseRequest>(x =>
            x.Kind == ConnectionUseKind.BackgroundSystem && x.TenantId == "tenant" && x.EnvironmentId == "environment" &&
            x.ConnectionId == "connection" && x.Purpose == "listen:slack-socket" &&
            x.Principal.Identity!.IsAuthenticated && x.Principal.Identity.AuthenticationType == "Elsa.Slack.SocketMode.Listener" &&
            x.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value == "elsa-slack-socket-listener"), Arg.Any<CancellationToken>());
        await test.Store.Received(2).FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>());
        await test.Secrets.Received(1).ResolveGenerationAsync("generation-secret", "connection", "generation", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeniedPurposeCannotReadStoreOrSecrets()
    {
        var test = new CredentialFixture();
        test.Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(false);
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.ResolveCurrentAsync(CancellationToken.None));
        await test.Store.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CancellationBeforeOrDuringPolicyEvaluationCannotReadCredential(bool duringPolicy)
    {
        var test = new CredentialFixture();
        using var cancellation = new CancellationTokenSource();
        if (duringPolicy)
        {
            test.Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
            {
                cancellation.Cancel();
                return Task.FromResult(true);
            });
        }
        else
        {
            cancellation.Cancel();
        }
        var error = await Assert.ThrowsAsync<OperationCanceledException>(() => test.Reader.ResolveCurrentAsync(cancellation.Token));
        Assert.Equal(cancellation.Token, error.CancellationToken);
        await test.Store.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ConnectionStatus.Disconnected, CredentialOperationStatus.None)]
    [InlineData(ConnectionStatus.RecoveryRequired, CredentialOperationStatus.None)]
    [InlineData(ConnectionStatus.Active, CredentialOperationStatus.Claimed)]
    [InlineData(ConnectionStatus.Active, CredentialOperationStatus.ProviderCallStarted)]
    [InlineData(ConnectionStatus.Active, CredentialOperationStatus.Staged)]
    [InlineData(ConnectionStatus.Active, CredentialOperationStatus.RecoveryRequired)]
    public async Task InactiveOrUncertainLifecycleNeverResolvesAGeneration(ConnectionStatus status, CredentialOperationStatus operation)
    {
        var test = new CredentialFixture();
        test.Connection.Status = status;
        test.Connection.OperationStatus = operation;
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.ResolveCurrentAsync(CancellationToken.None));
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ChangedGenerationBetweenSecretReadAndFinalReadIsDenied()
    {
        var test = new CredentialFixture();
        var reads = 0;
        test.Store.FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>()).Returns(_ =>
            ++reads == 1 ? test.Connection : new IntegrationConnection
            {
                Id = "connection", TenantId = "tenant", EnvironmentId = "environment", Revision = 2,
                Status = ConnectionStatus.Active, CurrentGenerationId = "replacement", CurrentSecretName = "replacement-secret"
            });
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.ResolveCurrentAsync(CancellationToken.None));
        Assert.Equal(2, reads);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task MissingCurrentGenerationMetadataCannotResolveSecret(bool missingName)
    {
        var test = new CredentialFixture();
        if (missingName)
        {
            test.Connection.CurrentSecretName = null;
        }
        else
        {
            test.Connection.CurrentGenerationId = null;
        }
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.ResolveCurrentAsync(CancellationToken.None));
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("{\"kind\":0,\"accessToken\":\"oauth\",\"refreshToken\":\"refresh\",\"accessTokenExpiresAt\":\"2099-01-01T00:00:00Z\"}")]
    [InlineData("{\"accessToken\":\"legacy-oauth\",\"refreshToken\":\"refresh\",\"accessTokenExpiresAt\":\"2099-01-01T00:00:00Z\"}")]
    [InlineData("{\"kind\":1,\"accessToken\":\"key\",\"refreshToken\":\"unexpected\"}")]
    [InlineData("invalid")]
    public async Task ListenerNeverFallsBackToOAuthOrMalformedMaterial(string envelope)
    {
        var test = new CredentialFixture();
        test.Secrets.ResolveGenerationAsync("generation-secret", "connection", "generation", Arg.Any<CancellationToken>())
            .Returns(SecretPayload.FromValue(envelope));
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.ResolveCurrentAsync(CancellationToken.None));
    }

    [Fact]
    public async Task ProviderExceptionDoesNotExportSecretOrConnectionDiagnostics()
    {
        var test = new CredentialFixture();
        test.Secrets.ResolveGenerationAsync("generation-secret", "connection", "generation", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<SecretPayload>(new InvalidOperationException(CredentialFixture.Token)));
        var error = await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.ResolveCurrentAsync(CancellationToken.None));
        Assert.Null(error.InnerException);
        Assert.DoesNotContain(CredentialFixture.Token, error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CurrentLeaseReauthorizesAndReadsMetadataWithoutResolvingBearerAgain()
    {
        var test = new CredentialFixture();
        var lease = await test.Reader.ResolveCurrentLeaseAsync(CancellationToken.None);
        Assert.Equal(ConnectionCredentialKind.ApiKey, lease.Credential.Kind);
        Assert.Equal(CredentialFixture.Token, lease.Credential.AccessToken);
        Assert.Equal(1, lease.Revision);
        Assert.Equal("generation", lease.GenerationId);
        Assert.Equal("generation-secret", lease.SecretName);
        Assert.Equal(SocketModeTestData.Configuration().BindingFingerprint, lease.BindingFingerprint);
        Assert.Equal("SlackSocketCredentialLease { Redacted = true }", lease.ToString());
        Assert.Equal("{}", JsonSerializer.Serialize(lease));
        test.ClearCalls();
        using var outer = test.Tenants.PushContext(new Tenant { Id = "outer", Name = "outer" });
        test.Store.FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>()).Returns(_ =>
        {
            Assert.Equal("tenant", test.Tenants.TenantId);
            return test.Connection;
        });
        await test.Reader.DemandCurrentAsync(lease, CancellationToken.None);
        Assert.Equal("outer", test.Tenants.TenantId);
        await test.Authorizer.Received(1).AuthorizeAsync(Arg.Is<ConnectionUseRequest>(request =>
            request.Kind == ConnectionUseKind.BackgroundSystem && request.TenantId == "tenant" && request.EnvironmentId == "environment" &&
            request.ConnectionId == "connection" && request.Purpose == SlackSocketListenerCredentialReader.Purpose &&
            request.Principal.Identity!.IsAuthenticated && request.Principal.Identity.AuthenticationType == SlackSocketListenerCredentialReader.AuthenticationType &&
            request.Principal.FindFirst(ClaimTypes.NameIdentifier)!.Value == SlackSocketListenerCredentialReader.PrincipalId), Arg.Any<CancellationToken>());
        await test.Store.Received(1).FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>());
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("revision")]
    [InlineData("generation")]
    [InlineData("secret-name")]
    [InlineData("missing-generation")]
    [InlineData("missing-name")]
    [InlineData("connection-id")]
    [InlineData("tenant")]
    [InlineData("environment")]
    [InlineData("disconnected")]
    [InlineData("recovery")]
    [InlineData("claimed")]
    [InlineData("provider-started")]
    [InlineData("credential-received")]
    [InlineData("staged")]
    [InlineData("operation-recovery")]
    public async Task StaleRotatedOffboardedOrForeignMetadataInvalidatesLease(string change)
    {
        var test = new CredentialFixture();
        var lease = await test.Reader.ResolveCurrentLeaseAsync(CancellationToken.None);
        test.ClearCalls();
        switch (change)
        {
            case "revision": test.Connection.Revision++; break;
            case "generation": test.Connection.CurrentGenerationId = "rotated"; break;
            case "secret-name": test.Connection.CurrentSecretName = "rotated-secret"; break;
            case "missing-generation": test.Connection.CurrentGenerationId = null; break;
            case "missing-name": test.Connection.CurrentSecretName = null; break;
            case "connection-id": test.Connection.Id = "another-connection"; break;
            case "tenant": test.Connection.TenantId = "another-tenant"; break;
            case "environment": test.Connection.EnvironmentId = "another-environment"; break;
            case "disconnected": test.Connection.Status = ConnectionStatus.Disconnected; break;
            case "recovery": test.Connection.Status = ConnectionStatus.RecoveryRequired; break;
            case "claimed": test.Connection.OperationStatus = CredentialOperationStatus.Claimed; break;
            case "provider-started": test.Connection.OperationStatus = CredentialOperationStatus.ProviderCallStarted; break;
            case "credential-received": test.Connection.OperationStatus = CredentialOperationStatus.CredentialReceived; break;
            case "staged": test.Connection.OperationStatus = CredentialOperationStatus.Staged; break;
            case "operation-recovery": test.Connection.OperationStatus = CredentialOperationStatus.RecoveryRequired; break;
            default: throw new ArgumentOutOfRangeException(nameof(change));
        }
        using var outer = test.Tenants.PushContext(new Tenant { Id = "outer", Name = "outer" });
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.DemandCurrentAsync(lease, CancellationToken.None));
        Assert.Equal("outer", test.Tenants.TenantId);
        await test.Authorizer.Received(1).AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>());
        await test.Store.Received(1).FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>());
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MissingConnectionInvalidatesLeaseWithoutSecretLookup()
    {
        var test = new CredentialFixture();
        var lease = await test.Reader.ResolveCurrentLeaseAsync(CancellationToken.None);
        test.ClearCalls();
        test.Store.FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>()).Returns((IntegrationConnection?)null);
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.DemandCurrentAsync(lease, CancellationToken.None));
        await test.Store.Received(1).FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>());
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ForeignListenerBindingCannotReuseLease()
    {
        var origin = new CredentialFixture();
        var lease = await origin.Reader.ResolveCurrentLeaseAsync(CancellationToken.None);
        var other = new CredentialFixture(SocketModeTestData.Configuration(teamId: "T_OTHER"));
        await Assert.ThrowsAsync<ConnectionUnavailableException>(() => other.Reader.DemandCurrentAsync(lease, CancellationToken.None));
        await other.Authorizer.DidNotReceive().AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>());
        await other.Store.DidNotReceive().FindAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await other.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("denied")]
    [InlineData("pre-canceled")]
    [InlineData("policy-canceled")]
    [InlineData("store-canceled")]
    public async Task LeaseRevalidationCannotOutrunDeniedPolicyOrCancellation(string boundary)
    {
        var test = new CredentialFixture();
        var lease = await test.Reader.ResolveCurrentLeaseAsync(CancellationToken.None);
        test.ClearCalls();
        using var cancellation = new CancellationTokenSource();
        switch (boundary)
        {
            case "denied":
                test.Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(false);
                break;
            case "pre-canceled":
                cancellation.Cancel();
                break;
            case "policy-canceled":
                test.Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(_ =>
                {
                    cancellation.Cancel();
                    return Task.FromResult(true);
                });
                break;
            case "store-canceled":
                test.Store.FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>()).Returns(_ =>
                {
                    cancellation.Cancel();
                    return test.Connection;
                });
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(boundary));
        }
        using var outer = test.Tenants.PushContext(new Tenant { Id = "outer", Name = "outer" });
        if (boundary == "denied")
        {
            await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.DemandCurrentAsync(lease, cancellation.Token));
        }
        else
        {
            var error = await Assert.ThrowsAsync<OperationCanceledException>(() => test.Reader.DemandCurrentAsync(lease, cancellation.Token));
            Assert.Equal(cancellation.Token, error.CancellationToken);
        }
        Assert.Equal("outer", test.Tenants.TenantId);
        await test.Authorizer.Received(boundary == "pre-canceled" ? 0 : 1).AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>());
        await test.Store.Received(boundary == "store-canceled" ? 1 : 0).FindAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaseStoreFailureIsRedactedAndRestoresOuterTenant()
    {
        var test = new CredentialFixture();
        var lease = await test.Reader.ResolveCurrentLeaseAsync(CancellationToken.None);
        test.ClearCalls();
        test.Store.FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<IntegrationConnection?>(new InvalidOperationException(CredentialFixture.Token)));
        using var outer = test.Tenants.PushContext(new Tenant { Id = "outer", Name = "outer" });
        var error = await Assert.ThrowsAsync<ConnectionUnavailableException>(() => test.Reader.DemandCurrentAsync(lease, CancellationToken.None));
        Assert.Null(error.InnerException);
        Assert.DoesNotContain(CredentialFixture.Token, error.ToString(), StringComparison.Ordinal);
        Assert.Equal("outer", test.Tenants.TenantId);
        await test.Secrets.DidNotReceive().ResolveGenerationAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    private sealed class CredentialFixture
    {
        internal const string Token = "synthetic-offline-app-token";
        internal IConnectionUseAuthorizer Authorizer { get; } = Substitute.For<IConnectionUseAuthorizer>();
        internal IConnectionLifecycleStore Store { get; } = Substitute.For<IConnectionLifecycleStore>();
        internal IManagedSecretManager Secrets { get; } = Substitute.For<IManagedSecretManager>();
        internal DefaultTenantAccessor Tenants { get; } = new();
        internal IntegrationConnection Connection { get; } = new()
        {
            Id = "connection", TenantId = "tenant", EnvironmentId = "environment", Revision = 1,
            ProviderId = "slack", ProviderAccountId = "installation", Status = ConnectionStatus.Active,
            CurrentGenerationId = "generation", CurrentSecretName = "generation-secret"
        };
        internal SlackSocketListenerCredentialReader Reader { get; }

        internal CredentialFixture(SlackSocketModeConfiguration? configuration = null)
        {
            Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(true);
            Store.FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>()).Returns(Connection);
            Secrets.ResolveGenerationAsync("generation-secret", "connection", "generation", Arg.Any<CancellationToken>())
                .Returns(SecretPayload.FromValue(JsonSerializer.Serialize(new { kind = 1, accessToken = Token, refreshToken = (string?)null, accessTokenExpiresAt = (DateTimeOffset?)null })));
            Reader = new(configuration ?? SocketModeTestData.Configuration(), Authorizer, Store, Secrets, TimeProvider.System, Tenants);
        }

        internal void ClearCalls()
        {
            Authorizer.ClearReceivedCalls();
            Store.ClearReceivedCalls();
            Secrets.ClearReceivedCalls();
        }
    }
}
