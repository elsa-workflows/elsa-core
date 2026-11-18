using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;
using NSubstitute;

namespace Elsa.Identity.UnitTests.Services;

public class DefaultUserCredentialsValidatorTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidAccountsWithoutLocalCredentialsPerformPasswordHashing(bool accountExists)
    {
        var users = Substitute.For<IUserProvider>();
        var hasher = Substitute.For<ISecretHasher>();
        var user = accountExists ? new User { Id = "user-1", Name = "alice" } : null;
        users.FindAsync(Arg.Any<UserFilter>(), Arg.Any<CancellationToken>()).Returns(user);
        var validator = new DefaultUserCredentialsValidator(users, hasher);

        Assert.Null(await validator.ValidateAsync("alice", "candidate"));

        hasher.Received(1).HashSecret("candidate");
        hasher.DidNotReceiveWithAnyArgs().VerifySecret(default!, default!, default!, out _);
    }
}
