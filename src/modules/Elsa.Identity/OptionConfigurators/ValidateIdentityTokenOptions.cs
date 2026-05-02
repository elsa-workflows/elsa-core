using Elsa.Identity.Options;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Validates the <see cref="IdentityTokenOptions"/>.
/// </summary>
public class ValidateIdentityTokenOptions : IValidateOptions<IdentityTokenOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, IdentityTokenOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey))
            return ValidateOptionsResult.Fail("SigningKey is required");

        return ValidateOptionsResult.Success;
    }
}