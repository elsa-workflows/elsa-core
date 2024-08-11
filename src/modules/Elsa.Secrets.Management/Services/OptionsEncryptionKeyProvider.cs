using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Management;

/// An implementation that provides encryption keys read from configuration. 
public class OptionsEncryptionKeyProvider(IOptions<EncryptionKeyOptions> options) : IEncryptionKeyProvider
{
    public Task<EncryptionKey> GetAsync(string id, CancellationToken cancellationToken = default)
    {
        var encryptionKey = options.Value.EncryptionKeys.FirstOrDefault(x => x.Id == id);
        
        if(encryptionKey == null)
            throw new Exception($"Could not find encryption key with id: {id}");
        
        return Task.FromResult(encryptionKey);
    }
}