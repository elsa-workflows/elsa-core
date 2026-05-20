using Elsa.Identity.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Validates the <see cref="IdentityTokenOptions"/>.
/// </summary>
public class ValidateIdentityTokenOptions : IValidateOptions<IdentityTokenOptions>
{
    private const int MinimumSigningKeyByteLength = 32;
    private const string DemoEnvironmentName = "Demo";

    private static readonly HashSet<string> KnownDefaultSigningKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "sufficiently-large-secret-signing-key",
        "CHANGE_ME_TO_A_SECURE_RANDOM_KEY"
    };

    private readonly IHostEnvironment? _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateIdentityTokenOptions"/> class.
    /// </summary>
    public ValidateIdentityTokenOptions()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateIdentityTokenOptions"/> class.
    /// </summary>
    public ValidateIdentityTokenOptions(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, IdentityTokenOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey))
            return ValidateOptionsResult.Fail("SigningKey is required. Configure a secure random JWT signing key through configuration, environment variables, or a secrets manager.");

        var signingKey = options.SigningKey.Trim();

        if (!string.Equals(signingKey, options.SigningKey, StringComparison.Ordinal))
            return ValidateOptionsResult.Fail("SigningKey must not contain leading or trailing whitespace. Configure the exact secure random JWT signing key through configuration, environment variables, or a secrets manager.");

        if (KnownDefaultSigningKeys.Contains(signingKey))
        {
            if (!IsDemoOrDevelopment())
                return ValidateOptionsResult.Fail("SigningKey uses a known public default value. Replace it with a secure random JWT signing key through configuration, environment variables, or a secrets manager. Known defaults are allowed only in the Development or Demo environment.");

            return ValidateOptionsResult.Success;
        }

        if (!IsPrintableAscii(signingKey))
            return ValidateOptionsResult.Fail("SigningKey contains non-printable or non-ASCII characters. Configure a secure random JWT signing key using only printable ASCII characters (0x20-0x7E) through configuration, environment variables, or a secrets manager.");

        if (signingKey.Length < MinimumSigningKeyByteLength)
            return ValidateOptionsResult.Fail($"SigningKey must be at least {MinimumSigningKeyByteLength} ASCII characters long. Configure a secure random JWT signing key through configuration, environment variables, or a secrets manager.");

        return ValidateOptionsResult.Success;
    }

    private bool IsDemoOrDevelopment()
    {
        return _environment is not null && (_environment.IsDevelopment() || string.Equals(_environment.EnvironmentName, DemoEnvironmentName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPrintableAscii(string value) => value.All(x => x is >= ' ' and <= '~');
}
