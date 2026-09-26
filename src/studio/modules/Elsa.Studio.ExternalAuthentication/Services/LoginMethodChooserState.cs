using Elsa.Studio.Authentication.Abstractions.Models;

namespace Elsa.Studio.ExternalAuthentication.Services;

/// <summary>Pure chooser rules shared by both Studio hosts and deliberately easy to test.</summary>
public static class LoginMethodChooserState
{
    public static IReadOnlyList<LoginMethodDescriptor> Order(IEnumerable<LoginMethodDescriptor> methods) => methods
        .OrderBy(method => method.Order)
        .ThenBy(method => method.DisplayName, StringComparer.OrdinalIgnoreCase)
        .ThenBy(method => method.Key, StringComparer.Ordinal)
        .ToArray();
}

/// <summary>Guards local post-authentication paths before a host constructs broker requests.</summary>
public static class LocalReturnPath
{
    public static string Normalize(string? candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate) ||
            !candidate.StartsWith("/", StringComparison.Ordinal) ||
            candidate.StartsWith("//", StringComparison.Ordinal) ||
            candidate.Contains('\\'))
            return "/";

        return Uri.TryCreate(candidate, UriKind.Relative, out _) ? candidate : "/";
    }
}
