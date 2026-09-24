using Elsa.Connections.Credentials.WorkerProcess;
using Elsa.Secrets.Contracts;
using Elsa.Secrets.Models;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Elsa.Connections.Credentials.Persistence.PostgreSql.IntegrationTests;

public sealed class GenerationAvailabilityProbeTests
{
    private const string ConnectionId = "connection-a";
    private const string GenerationId = "generation-a";
    private static readonly string SecretName = ManagedSecretNames.ForGeneration(ConnectionId, GenerationId);

    [Theory]
    [InlineData(false, true, SecretStatus.Active, true)]
    [InlineData(true, false, SecretStatus.Active, true)]
    [InlineData(true, true, SecretStatus.Revoked, true)]
    [InlineData(true, true, SecretStatus.Active, false)]
    public async Task MetadataKnownUnavailableSkipsPayloadResolution(
        bool ownerMatches,
        bool generationMatches,
        SecretStatus status,
        bool hasActiveVersion)
    {
        var secretManager = Substitute.For<ISecretManager>();
        var managedSecretManager = Substitute.For<IManagedSecretManager>();
        var secret = CreateSecret(ownerMatches, generationMatches, status, hasActiveVersion);
        secretManager.GetAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Secret?>(secret));
        using var services = CreateServices(secretManager, managedSecretManager);

        var available = await WorkerCommandHost.IsGenerationAvailableAsync(services, ConnectionId, GenerationId);

        Assert.False(available);
        await managedSecretManager.DidNotReceive()
            .ResolveGenerationAsync(SecretName, ConnectionId, GenerationId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task PayloadFailureWithStillAvailableMetadataPropagates()
    {
        var secretManager = Substitute.For<ISecretManager>();
        var managedSecretManager = Substitute.For<IManagedSecretManager>();
        var secret = CreateSecret(ownerMatches: true, generationMatches: true, SecretStatus.Active, hasActiveVersion: true);
        var failure = new InvalidOperationException("payload read failed");
        secretManager.GetAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Secret?>(secret));
        managedSecretManager.ResolveGenerationAsync(SecretName, ConnectionId, GenerationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SecretPayload>(failure));
        using var services = CreateServices(secretManager, managedSecretManager);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => WorkerCommandHost.IsGenerationAvailableAsync(services, ConnectionId, GenerationId));

        Assert.Same(failure, exception);
    }

    [Fact]
    public async Task PayloadFailureAfterMetadataBecomesUnavailableReturnsFalse()
    {
        var secretManager = Substitute.For<ISecretManager>();
        var managedSecretManager = Substitute.For<IManagedSecretManager>();
        var availableSecret = CreateSecret(ownerMatches: true, generationMatches: true, SecretStatus.Active, hasActiveVersion: true);
        var revokedSecret = CreateSecret(ownerMatches: true, generationMatches: true, SecretStatus.Revoked, hasActiveVersion: false);
        var metadataReads = 0;
        secretManager.GetAsync(SecretName, Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult<Secret?>(++metadataReads == 1 ? availableSecret : revokedSecret));
        managedSecretManager.ResolveGenerationAsync(SecretName, ConnectionId, GenerationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<SecretPayload>(new InvalidOperationException("generation became unavailable")));
        using var services = CreateServices(secretManager, managedSecretManager);

        var available = await WorkerCommandHost.IsGenerationAvailableAsync(services, ConnectionId, GenerationId);

        Assert.False(available);
        Assert.Equal(2, metadataReads);
    }

    private static Secret CreateSecret(bool ownerMatches, bool generationMatches, SecretStatus status, bool hasActiveVersion)
    {
        return new Secret
        {
            Name = SecretName,
            ManagedOwnerId = ownerMatches ? ConnectionId : "connection-b",
            ManagedGenerationId = generationMatches ? GenerationId : "generation-b",
            Status = status,
            Versions = hasActiveVersion ? [new SecretVersion { Version = 1, Status = SecretStatus.Active }] : []
        };
    }

    private static ServiceProvider CreateServices(ISecretManager secretManager, IManagedSecretManager managedSecretManager)
    {
        return new ServiceCollection()
            .AddSingleton(secretManager)
            .AddSingleton(managedSecretManager)
            .BuildServiceProvider();
    }
}
