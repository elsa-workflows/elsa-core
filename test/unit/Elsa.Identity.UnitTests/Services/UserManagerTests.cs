using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Services;
using Elsa.Testing.Shared.Multitenancy;
using Elsa.Workflows;
using NSubstitute;
using System.Threading.Tasks;

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

    [Test]
    public async Task CreateUserAsyncWithSuppliedPasswordMarksItAsNotGenerated()
    {
        var result = await _manager.CreateUserAsync("alice", " supplied-secret ", ["admin"]);

        await Assert.That(result.IsPasswordGenerated).IsFalse();
        await Assert.That(result.Password).IsEqualTo("supplied-secret");
        _secretGenerator.DidNotReceive().Generate(Arg.Any<int>());
        await AssertStoredCredentials(result.User);
    }

    [Test]
    [Arguments(null)]
    [Arguments("")]
    [Arguments("   ")]
    public async Task CreateUserAsyncWithoutPasswordGeneratesOneAndMarksIt(string? password)
    {
        var result = await _manager.CreateUserAsync("alice", password);

        await Assert.That(result.IsPasswordGenerated).IsTrue();
        await Assert.That(result.Password).IsEqualTo("generated-secret");
        await AssertStoredCredentials(result.User);
    }

    private static async Task AssertStoredCredentials(User user)
    {
        await Assert.That(user.Id).IsEqualTo("user-1");
        await Assert.That(user.TenantId).IsEqualTo("tenant-a");
        await Assert.That(string.IsNullOrEmpty(user.HashedPassword)).IsFalse();
        await Assert.That(string.IsNullOrEmpty(user.HashedPasswordSalt)).IsFalse();
    }
}