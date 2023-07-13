using Proto.Cluster;
using Proto.Cluster.AzureContainerApps.Stores.Redis;
using StackExchange.Redis;

namespace Elsa.ProtoCluster.ComponentTests.AzureContainerAppTests;

public class RedisClusterMemberStoreTests : IClassFixture<RedisFixture>
{
    private readonly RedisClusterMemberStore _memberStore;
    private readonly Member _member;

    public RedisClusterMemberStoreTests(RedisFixture redisFixture)
    {
        
        var multiplexer = ConnectionMultiplexer.Connect(redisFixture.GetConnectionString());
        _memberStore = new RedisClusterMemberStore(multiplexer);

        _member = new Member
        {
            Id = "id1",
            Host = "localhost",
            Port = 8000,
            Kinds = { "kind1", "kind2" }
        };
    }

    [Fact]
    public async Task RegisterAsync_ShouldRegisterMember()
    {
        await _memberStore.RegisterAsync("cluster", _member);

        var members = await _memberStore.ListAsync();
        Assert.Contains(members, m => m.Id == _member.Id);
    }

    [Fact]
    public async Task UnregisterAsync_ShouldUnregisterMember()
    {
        await _memberStore.RegisterAsync("cluster", _member);
        await _memberStore.UnregisterAsync(_member.Id);

        var members = await _memberStore.ListAsync();
        Assert.DoesNotContain(members, m => m.Id == _member.Id);
    }

    [Fact]
    public async Task ClearAsync_ShouldClearAllMembers()
    {
        await _memberStore.RegisterAsync("cluster", _member);
        await _memberStore.ClearAsync("cluster");

        var members = await _memberStore.ListAsync();
        Assert.Empty(members);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnAllRegisteredMembers()
    {
        await _memberStore.RegisterAsync("cluster", _member);

        var members = await _memberStore.ListAsync();
        Assert.Contains(members, m => m.Id == _member.Id);
    }
}
