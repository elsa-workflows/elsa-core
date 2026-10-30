using Elsa.Common;
using Elsa.Common.Services;
using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.Secrets.Management.UnitTests;

public class DefaultSecretManagerTests
{
    [Fact]
    public async Task DeleteManyAsync_WhenFilteredByVersionId_DeletesAllVersionsOfSelectedSecrets()
    {
        // Arrange
        var store = new MemorySecretStore(new MemoryStore<Secret>());
        var selectedV1 = CreateVersion("selected-v1", "secret-selected", version: 1, isLatest: false);
        var selectedV2 = CreateVersion("selected-v2", "secret-selected", version: 2, isLatest: true);
        var otherV1 = CreateVersion("other-v1", "secret-other", version: 1, isLatest: false);
        var otherV2 = CreateVersion("other-v2", "secret-other", version: 2, isLatest: true);

        await store.AddAsync(selectedV1);
        await store.AddAsync(selectedV2);
        await store.AddAsync(otherV1);
        await store.AddAsync(otherV2);

        var manager = new DefaultSecretManager(
            store,
            Substitute.For<IEncryptor>(),
            Substitute.For<ISecretUpdater>(),
            Substitute.For<IIdentityGenerator>(),
            Substitute.For<ISystemClock>(),
            Substitute.For<INotificationSender>());

        // Act
        var count = await manager.DeleteManyAsync(new SecretFilter
        {
            Ids = [selectedV2.Id]
        });

        // Assert
        var remaining = (await store.ListAsync()).ToList();

        Assert.Equal(2, count);
        Assert.DoesNotContain(remaining, x => x.SecretId == selectedV1.SecretId);
        Assert.Equal(2, remaining.Count);
        Assert.Contains(remaining, x => x.Id == otherV1.Id);
        Assert.Contains(remaining, x => x.Id == otherV2.Id);
    }

    private static Secret CreateVersion(string id, string secretId, int version, bool isLatest) => new()
    {
        Id = id,
        SecretId = secretId,
        Name = secretId,
        EncryptedValue = $"value-{version}",
        Version = version,
        IsLatest = isLatest
    };
}
