namespace Elsa.Studio.Authentication.Abstractions.Models;

/// <summary>Safe presentation data for one login method.</summary>
public sealed record LoginMethodDescriptor(
    string Id,
    string Key,
    string Kind,
    string DisplayName,
    string? IconId,
    int Order,
    bool IsPreferred,
    string InitiationUri)
;

public sealed record LoginMethodCatalogResult(
    IReadOnlyCollection<LoginMethodDescriptor> Methods,
    string? PreferredMethodKey = null,
    string? SecurityWarning = null);

public sealed record LoginMethodComponentContext(
    LoginMethodDescriptor Method,
    string ReturnPath,
    Func<string, Task> ReportFailureAsync);

public sealed record LoginMethodIcon(string Svg, string AccessibleName);

public sealed record LoginMethodIconRegistration(string IconId, LoginMethodIcon Icon);
