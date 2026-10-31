using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Elsa.Identity.Models;
using Elsa.Identity.Services;

namespace Elsa.Identity.UnitTests.Services;

public class CredentiallessUserTests
{
    [Fact(DisplayName = "Existing local-password users remain able to authenticate")]
    public async Task ExistingLocalPasswordUserCanAuthenticate()
    {
        var hasher = new DefaultSecretHasher();
        var password = hasher.HashSecret("correct-password");
        var user = new User
        {
            Id = "local-user",
            Name = "local-user",
            HashedPassword = password.EncodeSecret(),
            HashedPasswordSalt = password.EncodeSalt()
        };
        var validator = new DefaultUserCredentialsValidator(new StaticUserProvider(user), hasher);

        var result = await validator.ValidateAsync(user.Name, "correct-password");

        Assert.Same(user, result);
    }

    [Fact(DisplayName = "Credential-less users cannot authenticate with a local password")]
    public async Task CredentiallessUserCannotAuthenticateLocally()
    {
        var user = new User
        {
            Id = "external-user",
            Name = "external-user",
            HashedPassword = null,
            HashedPasswordSalt = null
        };
        var validator = new DefaultUserCredentialsValidator(new StaticUserProvider(user), new DefaultSecretHasher());

        var result = await validator.ValidateAsync(user.Name, "any-password");

        Assert.Null(result);
    }

    private sealed class StaticUserProvider(User user) : IUserProvider
    {
        public Task<User?> FindAsync(UserFilter filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<User?>(filter.Name == user.Name ? user : null);
        }
    }
}
