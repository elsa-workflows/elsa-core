using Elsa.Studio.Localization;
using Microsoft.Extensions.Localization;

namespace Elsa.Studio.Testing;

/// <summary>
/// An <see cref="ILocalizer"/> that passes every key straight through, formatting arguments where given, so tests
/// can assert on the text a component renders without wiring up real localization resources.
/// </summary>
internal sealed class TestLocalizer : ILocalizer
{
    public LocalizedString this[string? key] => new(key ?? string.Empty, key ?? string.Empty);
    public LocalizedString this[string? key, params object[] arguments] => new(key ?? string.Empty, string.Format(key ?? string.Empty, arguments));
}
