using System.Text.RegularExpressions;

namespace Elsa.Secrets.Services;

public partial class DefaultSecretNameValidator : ISecretNameValidator
{
    public bool IsValid(string? name, out string? error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Secret name is required.";
            return false;
        }

        if (!SecretNameRegex().IsMatch(name.Trim()))
        {
            error = "Secret name must start with a letter and contain only letters, numbers, dots, dashes, underscores, and colons.";
            return false;
        }

        error = null;
        return true;
    }

    public string Normalize(string name) => name.Trim().ToLowerInvariant();

    [GeneratedRegex("^[A-Za-z][A-Za-z0-9._:-]{1,199}$", RegexOptions.Compiled)]
    private static partial Regex SecretNameRegex();
}
