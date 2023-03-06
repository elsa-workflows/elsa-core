using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Services;

public class DefaultCredentialsValidator : ICredentialsValidator
{
    private readonly IUserStore _userStore;
    private readonly IPasswordHasher _passwordHasher;

    public DefaultCredentialsValidator(IUserStore userStore, IPasswordHasher passwordHasher)
    {
        _userStore = userStore;
        _passwordHasher = passwordHasher;
    }

    public async ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userStore.FindAsync(username, cancellationToken);

        if (user == null)
            return default;

        var isValidPassword = _passwordHasher.VerifyHashedPassword(user.HashedPassword, password);

        return !isValidPassword ? default : user;
    }
}