using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Elsa.Secrets.Options;

/// <summary>Protects the encrypted secret store from missing or sample keys in hosted production deployments.</summary>
public sealed class ValidateSecretsOptions(IHostEnvironment? environment = null) : IValidateOptions<SecretsOptions>
{
    private static readonly byte[] SampleKey = Encoding.UTF8.GetBytes("CHANGE_ME_TO_A_SECURE_RANDOM_KEY");

    public ValidateOptionsResult Validate(string? name, SecretsOptions options)
    {
        if (environment is null || environment.IsDevelopment() || environment.IsEnvironment("Demo"))
            return ValidateOptionsResult.Success;

        var key = options.EncryptionKey;
        return key is { Length: 16 or 24 or 32 } && !key.AsSpan().SequenceEqual(SampleKey)
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail("Configure a non-sample encryption key for the Elsa encrypted secrets store.");
    }
}
