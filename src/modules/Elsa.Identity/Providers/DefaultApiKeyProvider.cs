using System.Security.Claims;
using AspNetCore.Authentication.ApiKey;
using Elsa.Identity.Contracts;
using Elsa.Identity.Models;

namespace Elsa.Identity.Providers;

/// <summary>
/// Validates a given API key and returns an instance of <see cref="IApiKey"/> if the key is valid.
/// </summary>
public class DefaultApiKeyProvider : IApiKeyProvider
{
    private readonly IApplicationCredentialsValidator _applicationCredentialsValidator;
    private readonly IRoleProvider _roleProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApiKeyProvider"/> class.
    /// </summary>
    public DefaultApiKeyProvider(IApplicationCredentialsValidator applicationCredentialsValidator, IRoleProvider roleProvider)
    {
        _applicationCredentialsValidator = applicationCredentialsValidator;
        _roleProvider = roleProvider;
    }

    /// <summary>
    /// Gets an instance of <see cref="IApiKey"/>.
    /// </summary>
    /// <param name="key">The API key to validate.</param>
    /// <returns>The API key if a valid key was provided.</returns>
    public async Task<IApiKey?> ProvideAsync(string key)
    {
        var application = await _applicationCredentialsValidator.ValidateAsync(key);

        if (application == null)
            return null;

        var filter = new RoleFilter { Ids = application.Roles.Distinct().ToList() };
        var roles = (await _roleProvider.FindManyAsync(filter)).ToList();
        var permissions = roles.SelectMany(x => x.Permissions).Distinct().ToList();
        var claims = new List<Claim>();

        claims.AddRange(permissions.Select(p => new Claim("permissions", p)));

        return new ApiKey(key, application.ClientId, claims);
    }
}