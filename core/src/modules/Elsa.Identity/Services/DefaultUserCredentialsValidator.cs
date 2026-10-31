using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Elsa.Identity.Services;

/// <summary>
/// Validates user credentials
/// </summary>
public class DefaultUserCredentialsValidator : IUserCredentialsValidator
{
    private readonly IUserProvider _userProvider;
    private readonly IUserStore? _userStore;
    private readonly ISecretHasher _secretHasher;
    private readonly ILogger<DefaultUserCredentialsValidator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUserCredentialsValidator"/> class.
    /// </summary>
    public DefaultUserCredentialsValidator(IUserProvider userProvider, ISecretHasher secretHasher) : this(userProvider, null, secretHasher, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUserCredentialsValidator"/> class.
    /// </summary>
    public DefaultUserCredentialsValidator(IUserProvider userProvider, IUserStore userStore, ISecretHasher secretHasher) : this(userProvider, userStore, secretHasher, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultUserCredentialsValidator"/> class.
    /// </summary>
    public DefaultUserCredentialsValidator(IUserProvider userProvider, IUserStore? userStore, ISecretHasher secretHasher, ILogger<DefaultUserCredentialsValidator>? logger)
    {
        _userProvider = userProvider;
        _userStore = userStore;
        _secretHasher = secretHasher;
        _logger = logger ?? NullLogger<DefaultUserCredentialsValidator>.Instance;
    }

    /// <inheritdoc />
    public async ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _userProvider.FindByNameAsync(username, cancellationToken);

        if (user == null)
            return null;

        if (string.IsNullOrEmpty(user.HashedPassword) || string.IsNullOrEmpty(user.HashedPasswordSalt))
            return null;

        var isValidPassword = _secretHasher.VerifySecret(password, user.HashedPassword, user.HashedPasswordSalt, out var needsRehash);

        if (!isValidPassword)
            return null;

        if (needsRehash && _userStore != null)
        {
            var previousHashedPassword = user.HashedPassword;
            var previousHashedPasswordSalt = user.HashedPasswordSalt;
            var hashedPassword = _secretHasher.HashSecret(password);
            user.HashedPassword = hashedPassword.EncodeSecret();
            user.HashedPasswordSalt = hashedPassword.EncodeSalt();
            try
            {
                await _userStore.SaveAsync(user, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                user.HashedPassword = previousHashedPassword;
                user.HashedPasswordSalt = previousHashedPasswordSalt;
                throw;
            }
            catch (Exception e)
            {
                user.HashedPassword = previousHashedPassword;
                user.HashedPasswordSalt = previousHashedPasswordSalt;
                _logger.LogWarning(e, "Failed to save upgraded password hash for user {UserId}.", user.Id);
            }
        }

        return user;
    }
}
