using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Services;

public class UserManagerTests
{
    private readonly ISecretGenerator _secretGenerator = Substitute.For<ISecretGenerator>();
    private readonly IUserStore _userStore = Substitute.For<IUserStore>();
    private readonly UserManager _manager;

    public UserManagerTests()
    {
        var identityGenerator = Substitute.For<IIdentityGenerator>();
        identityGenerator.GenerateId().Returns("user-1");
        _secretGenerator.Generate(Arg.Any<int>()).Returns("generated-secret");
        _manager = new(identityGenerator, _secretGenerator, new DefaultSecretHasher(), _userStore, new TestTenantAccessor("tenant-a"));
    }

    [Fact]
    public async Task CreateUserAsyncWithSuppliedPasswordMarksItAsNotGenerated()
    {
        var result = await _manager.CreateUserAsync("alice", " supplied-secret ", ["admin"]);

        Assert.False(result.IsPasswordGenerated);
        Assert.Equal("supplied-secret", result.Password);
        _secretGenerator.DidNotReceive().Generate(Arg.Any<int>());
        AssertStoredCredentials(result.User);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateUserAsyncWithoutPasswordGeneratesOneAndMarksIt(string? password)
    {
        var result = await _manager.CreateUserAsync("alice", password);

        Assert.True(result.IsPasswordGenerated);
        Assert.Equal("generated-secret", result.Password);
        AssertStoredCredentials(result.User);
    }

    private static void AssertStoredCredentials(User user)
    {
        Assert.Equal("user-1", user.Id);
        Assert.Equal("tenant-a", user.TenantId);
        Assert.False(string.IsNullOrEmpty(user.HashedPassword));
        Assert.False(string.IsNullOrEmpty(user.HashedPasswordSalt));
    }
}
