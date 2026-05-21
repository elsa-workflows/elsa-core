using System.Text.RegularExpressions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Context;

public class AiContextResolver(IEnumerable<IAiContextProvider> providers)
{
    private static readonly string[] SensitiveKeyFragments = ["secret", "token", "password", "apikey", "api_key", "api-key", "authorization", "credential", "bearer"];
    private static readonly Regex[] SensitiveValuePatterns =
    [
        new(@"\bBearer\s+[A-Za-z0-9._~+/\-=]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant),
        new(@"\b(?:api[_-]?key|token|secret|password)\s*[:=]\s*['""]?[A-Za-z0-9._~+/\-=]{8,}['""]?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)
    ];
    private const string Redacted = "[redacted]";
    private readonly Dictionary<string, IAiContextProvider> _providers = BuildProviders(providers);

    public async ValueTask<IReadOnlyCollection<AiResolvedContext>> ResolveAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        var resolved = new List<AiResolvedContext>();

        foreach (var attachment in request.Attachments)
        {
            if (!_providers.TryGetValue(attachment.Kind, out var provider))
                continue;

            var context = await provider.ResolveAsync(new AiContextResolutionRequest
            {
                Attachment = attachment,
                TenantId = request.TenantId,
                UserId = request.UserId
            }, cancellationToken);

            resolved.Add(Redact(context));
        }

        return resolved;
    }

    private static AiResolvedContext Redact(AiResolvedContext context) =>
        context with
        {
            Summary = RedactText(context.Summary),
            Data = RedactObject(context.Data),
            Metadata = RedactObject(context.Metadata)
        };

    private static JsonObject RedactObject(JsonObject source)
    {
        var redacted = new JsonObject();

        foreach (var property in source)
            redacted[property.Key] = IsSensitiveKey(property.Key) ? Redacted : RedactNode(property.Value);

        return redacted;
    }

    private static JsonNode? RedactNode(JsonNode? node) =>
        node switch
        {
            JsonObject jsonObject => RedactObject(jsonObject),
            JsonArray jsonArray => new JsonArray(jsonArray.Select(RedactNode).ToArray()),
            JsonValue jsonValue when jsonValue.TryGetValue<string>(out var value) => JsonValue.Create(RedactText(value)),
            JsonValue jsonValue => jsonValue.DeepClone(),
            _ => node?.DeepClone()
        };

    private static string RedactText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        return SensitiveValuePatterns.Aggregate(text, (current, pattern) => pattern.Replace(current, Redacted));
    }

    private static bool IsSensitiveKey(string key) =>
        SensitiveKeyFragments.Any(fragment => key.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static Dictionary<string, IAiContextProvider> BuildProviders(IEnumerable<IAiContextProvider> providers)
    {
        return providers
            .GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                x => x.Key,
                x =>
                {
                    var group = x.ToList();
                    if (group.Count > 1)
                    {
                        var providerNames = string.Join(", ", group.Select(provider => provider.GetType().Name));
                        throw new InvalidOperationException($"Multiple AI context providers are registered for kind '{x.Key}': {providerNames}.");
                    }

                    return group[0];
                },
                StringComparer.OrdinalIgnoreCase);
    }
}
