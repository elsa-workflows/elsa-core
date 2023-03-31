using Elsa.Common.Exceptions;
using Elsa.Identity.Options;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Validates the <see cref="IdentityTokenOptions"/>.
/// </summary>
public class ValidateIdentityTokenOptions : IPostConfigureOptions<IdentityTokenOptions>
{
    /// <inheritdoc />
    public void PostConfigure(string? name, IdentityTokenOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SigningKey))
            throw new MissingConfigurationException("SigningKey is required");
    }
}