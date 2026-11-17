using Elsa.Common.Multitenancy;
using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using Microsoft.AspNetCore.Http;
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
    private readonly ILocalPasswordAttemptLimiter? _attemptLimiter;
    private readonly ITenantAccessor? _tenantAccessor;
    private readonly IHttpContextAccessor? _httpContextAccessor;

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

    /// <summary>Initializes the host-managed validator with shared password attempt budgets.</summary>
    public DefaultUserCredentialsValidator(
        IUserProvider userProvider,
        IUserStore? userStore,
        ISecretHasher secretHasher,
        ILogger<DefaultUserCredentialsValidator>? logger,
        ILocalPasswordAttemptLimiter attemptLimiter,
        ITenantAccessor tenantAccessor,
        IHttpContextAccessor httpContextAccessor) : this(userProvider, userStore, secretHasher, logger)
    {
        _attemptLimiter = attemptLimiter;
        _tenantAccessor = tenantAccessor;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <inheritdoc />
    public async ValueTask<User?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (_attemptLimiter is null)
            return await ValidateCredentialsAsync(username, password, cancellationToken);

        var httpContext = _httpContextAccessor!.HttpContext;
        var source = httpContext?.Connection.RemoteIpAddress;
        if (source?.IsIPv4MappedToIPv6 == true)
            source = source.MapToIPv4();

        await using var attempt = await _attemptLimiter.TryBeginAsync(
            _tenantAccessor!.TenantId, username.Trim().ToUpperInvariant(),
            httpContext is null ? null : source?.ToString() ?? "unknown", cancellationToken);
        if (attempt is null)
            return null;

        var user = await ValidateCredentialsAsync(username, password, cancellationToken);
        if (user is null)
            attempt.MarkFailure();
        return user;
    }

    private async ValueTask<User?> ValidateCredentialsAsync(string username, string password, CancellationToken cancellationToken)
    {
        var user = await _userProvider.FindByNameAsync(username, cancellationToken);

        if (user == null || string.IsNullOrEmpty(user.HashedPassword) || string.IsNullOrEmpty(user.HashedPasswordSalt))
        {
            // Use the configured password hashing cost for accounts without verifiable local credentials.
            _ = _secretHasher.HashSecret(password);
            return null;
        }

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
