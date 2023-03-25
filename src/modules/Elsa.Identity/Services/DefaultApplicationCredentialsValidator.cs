using Elsa.Extensions;
using Elsa.Identity.Contracts;
using Elsa.Identity.Entities;
using JetBrains.Annotations;

namespace Elsa.Identity.Services;

/// <inheritdoc />
[PublicAPI]
public class DefaultApplicationCredentialsValidator : IApplicationCredentialsValidator
{
    private readonly IApiKeyParser _apiKeyParser;
    private readonly IApplicationProvider _applicationProvider;
    private readonly ISecretHasher _secretHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApplicationCredentialsValidator"/> class.
    /// </summary>
    public DefaultApplicationCredentialsValidator(IApiKeyParser apiKeyParser, IApplicationProvider applicationProvider, ISecretHasher secretHasher)
    {
        _apiKeyParser = apiKeyParser;
        _applicationProvider = applicationProvider;
        _secretHasher = secretHasher;
    }
    
    /// <inheritdoc />
    public async ValueTask<Application?> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        var clientId = _apiKeyParser.Parse(apiKey);
        var application = await _applicationProvider.FindByClientIdAsync(clientId, cancellationToken);
        
        if(application == null)
            return default;
        
        var isValidApiKey = _secretHasher.VerifySecret(apiKey, application.HashedApiKey, application.HashedApiKeySalt);
        return isValidApiKey ? application : default;
    }
}