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

        internal CredentialFixture()
        {
            Authorizer.AuthorizeAsync(Arg.Any<ConnectionUseRequest>(), Arg.Any<CancellationToken>()).Returns(true);
            Store.FindAsync("connection", "tenant", "environment", Arg.Any<CancellationToken>()).Returns(Connection);
            Secrets.ResolveGenerationAsync("generation-secret", "connection", "generation", Arg.Any<CancellationToken>())
                .Returns(SecretPayload.FromValue(JsonSerializer.Serialize(new { kind = 1, accessToken = Token, refreshToken = (string?)null, accessTokenExpiresAt = (DateTimeOffset?)null })));
            Reader = new(SocketModeTestData.Configuration(), Authorizer, Store, Secrets, TimeProvider.System, Tenants);
        }
    }
}
