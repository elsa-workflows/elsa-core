using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;

namespace Elsa.Identity.Services;

/// <summary>
/// Validates user credentials
/// </summary>
public class DefaultUserCredentialsValidator : IUserCredentialsValidator
{
    private readonly IUserProvider _userProvider;
    private readonly ISecretHasher _secretHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUserCredentialsValidator"/> class.
    /// </summary>
    public DefaultUserCredentialsValidator(IUserProvider userProvider, ISecretHasher secretHasher)
    {
        _userProvider = userProvider;
        _secretHasher = secretHasher;
    }

    /// <inheritdoc />
    public async ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userProvider.FindByNameAsync(username, cancellationToken);

        if (user == null)
            return default;

        var isValidPassword = _secretHasher.VerifySecret(password, user.HashedPassword, user.HashedPasswordSalt);

        return isValidPassword ? user : default;
    }
}