using Proto.Cluster;
using Proto.Cluster.AzureContainerApps.Services;
using Proto.Cluster.AzureContainerApps.Stores.Redis;

namespace Elsa.ProtoCluster.ComponentTests.AzureContainerAppTests;

public class RedisClusterMemberStoreTests : IClassFixture<RedisFixture>
{
    private readonly RedisClusterMemberStore _memberStore;
    private readonly Member _member;

    public RedisClusterMemberStoreTests(RedisFixture redisFixture)
    {
        var multiplexerProvider = new RedisConnectionMultiplexerProvider(redisFixture.GetConnectionString());
        _memberStore = new RedisClusterMemberStore(multiplexerProvider, new DefaultSystemClock());

        _member = new Member
        {
            Id = "id1",
            Host = "localhost",
            Port = 8000,
            Kinds = { "kind1", "kind2" }
        };
    }

    [Fact(DisplayName = "Invoking RegisterAsync should register member")]
    public async Task RegisterAsync_ShouldRegisterMember()
    {
        await _memberStore.RegisterAsync("cluster", _member);

        var members = await _memberStore.ListAsync();
        Assert.Contains(members, m => m.Id == _member.Id);
    }

    [Fact(DisplayName = "Invoking UnregisterAsync should unregister member")]
    public async Task UnregisterAsync_ShouldUnregisterMember()
    {
        await _memberStore.RegisterAsync("cluster", _member);
        await _memberStore.UnregisterAsync(_member.Id);

        var members = await _memberStore.ListAsync();
        Assert.DoesNotContain(members, m => m.Id == _member.Id);
    }

    [Fact(DisplayName = "Invoking ClearAsync should clear all members")]
    public async Task ClearAsync_ShouldClearAllMembers()
    {
        await _memberStore.RegisterAsync("cluster", _member);
        await _memberStore.ClearAsync("cluster");

        var members = await _memberStore.ListAsync();
        Assert.Empty(members);
    }

    [Fact(DisplayName = "Invoking ListAsync should return all registered members")]
    public async Task ListAsync_ShouldReturnAllRegisteredMembers()
    {
        await _memberStore.RegisterAsync("cluster", _member);

        var members = await _memberStore.ListAsync();
        Assert.Contains(members, m => m.Id == _member.Id);
    }
}