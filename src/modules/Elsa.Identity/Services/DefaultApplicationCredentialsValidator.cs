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
    private readonly IApplicationStore _applicationStore;
    private readonly ISecretHasher _secretHasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultApplicationCredentialsValidator"/> class.
    /// </summary>
    public DefaultApplicationCredentialsValidator(IApiKeyParser apiKeyParser, IApplicationProvider applicationProvider, IApplicationStore applicationStore, ISecretHasher secretHasher)
    {
        _apiKeyParser = apiKeyParser;
        _applicationProvider = applicationProvider;
        _applicationStore = applicationStore;
        _secretHasher = secretHasher;
    }
    
    /// <inheritdoc />
    public async ValueTask<Application?> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if(string.IsNullOrWhiteSpace(apiKey))
            return null;
        
        var clientId = _apiKeyParser.Parse(apiKey);
        var application = await _applicationProvider.FindByClientIdAsync(clientId, cancellationToken);
        
        if(application == null)
            return null;
        
        var isValidApiKey = _secretHasher.VerifySecret(apiKey, application.HashedApiKey, application.HashedApiKeySalt, out var needsRehash);

        if (!isValidApiKey)
            return null;

        if (needsRehash)
        {
            var hashedApiKey = _secretHasher.HashSecret(apiKey);
            application.HashedApiKey = hashedApiKey.EncodeSecret();
            application.HashedApiKeySalt = hashedApiKey.EncodeSalt();
            await _applicationStore.SaveAsync(application, cancellationToken);
        }

        return application;
    }
}
