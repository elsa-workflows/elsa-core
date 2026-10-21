using System.Diagnostics.CodeAnalysis;

namespace Elsa.Bpmn.Hosting;

/// <summary>
/// Maps a BPMN expression language to the Elsa expression type that evaluates a condition written in it, per ADR
/// "Evaluate BPMN conditions with Elsa expressions" (<c>docs/adr/2026-10-20-elsa-evaluates-bpmn-conditions.md</c>).
/// </summary>
/// <remarks>
/// Public so <c>Elsa.Bpmn.Interchange</c> refuses at import exactly the languages the runtime would fail on, rather
/// than restating the mapping. The language is the condition's own, else the document's
/// <c>definitions/@expressionLanguage</c>; <c>Bpmn.Semantics</c> resolves that precedence before it asks.
/// </remarks>
public static class BpmnConditionLanguages
{
    /// <summary>The Elsa expression type a condition is evaluated as when neither it nor its document names a language.</summary>
    public const string DefaultExpressionType = "JavaScript";

    private static readonly IReadOnlyDictionary<string, string> ExpressionTypesByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["javascript"] = "JavaScript",
        ["csharp"] = "CSharp",
        ["liquid"] = "Liquid"
    };

    /// <summary>
    /// Resolves <paramref name="language"/> to an Elsa expression type: <c>javascript</c>, <c>csharp</c> or
    /// <c>liquid</c>, case-insensitively, written as that name or as an absolute URI whose last path segment is that
    /// name. A missing or blank language is <see cref="DefaultExpressionType"/>. Any other language is not mapped
    /// onto one of these: it is refused.
    /// </summary>
    public static bool TryResolveExpressionType(string? language, [NotNullWhen(true)] out string? expressionType)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            expressionType = DefaultExpressionType;
            return true;
        }

        var name = language.Trim();

        if (Uri.TryCreate(name, UriKind.Absolute, out var uri) && !uri.IsFile)
        {
            name = uri.Segments.Length > 0 ? uri.Segments[^1].TrimEnd('/') : string.Empty;
        }

        return ExpressionTypesByName.TryGetValue(name, out expressionType);
    }
}
