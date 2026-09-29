using Elsa.Studio.Localization;
using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Administration.Tests;

/// <summary>Returns every key as its own translation.</summary>
internal sealed class TestLocalizer : ILocalizer
{
    public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
    public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
}
