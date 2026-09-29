namespace Elsa.Studio.ExternalAuthentication.BlazorWasm.Services;

/// <summary>Validates targets which remain inside the registered Studio client.</summary>
public static class ExternalAuthenticationReturnPath
{
    /// <summary>Returns a safe client-local path, falling back to the Studio root.</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !value.StartsWith("/", StringComparison.Ordinal) ||
            value.StartsWith("//", StringComparison.Ordinal) ||
            value.Contains("\\", StringComparison.Ordinal) ||
            !Uri.TryCreate(value, UriKind.Relative, out var uri) ||
            uri.IsAbsoluteUri)
        {
            return "/";
        }

        return value;
    }
}
