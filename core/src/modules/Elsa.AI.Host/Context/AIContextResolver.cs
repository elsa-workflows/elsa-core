using System.Text.RegularExpressions;
using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Elsa.AI.Host.Context;

public class AIContextResolver(IServiceScopeFactory scopeFactory, ILogger<AIContextResolver> logger)
{
    private static readonly string[] SensitiveKeyFragments = ["secret", "token", "password", "apikey", "api_key", "api-key", "authorization", "credential", "bearer"];
    private static readonly Regex[] SensitiveValuePatterns =
    [
        new(@"\bBearer\s+[A-Za-z0-9._~+/\-=]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled),
        new(@"\b(?:api[_-]?key|token|secret|password)\s*[:=]\s*['""]?[A-Za-z0-9._~+/\-=]{8,}['""]?", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled)
    ];
    private const string Redacted = "[redacted]";

    public async ValueTask<IReadOnlyCollection<AIResolvedContext>> ResolveAsync(AIChatRequest request, CancellationToken cancellationToken = default)
    {
        using var scope = scopeFactory.CreateScope();
        var providers = BuildProviders(scope.ServiceProvider.GetServices<IAIContextProvider>());
        var resolved = new List<AIResolvedContext>();
        var attachmentsWithProviders = request.Attachments
            .Select(attachment => new
            {
                Attachment = attachment,
                Provider = providers.GetValueOrDefault(attachment.Kind)
            })
            .Where(x => x.Provider != null);

        foreach (var item in attachmentsWithProviders)
        {
            if (item.Provider is IPlaceholderAIContextProvider)
                logger.LogWarning(
                    "AI context kind {ContextKind} is using placeholder provider {ProviderType}. Replace it before production use.",
                    item.Attachment.Kind,
                    item.Provider.GetType().Name);

            var context = await item.Provider!.ResolveAsync(new AIContextResolutionRequest
            {
                Attachment = item.Attachment,
                TenantId = request.TenantId,
                UserId = request.UserId
            }, cancellationToken);

            resolved.Add(Redact(context));
        }

        return resolved;
    }

    private static AIResolvedContext Redact(AIResolvedContext context) =>
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

    private static Dictionary<string, IAIContextProvider> BuildProviders(IEnumerable<IAIContextProvider> providers)
    {
        return providers
            .GroupBy(x => x.Kind, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, SelectProvider, StringComparer.OrdinalIgnoreCase);
    }

    private static IAIContextProvider SelectProvider(IEnumerable<IAIContextProvider> providers)
    {
        var providerList = providers.ToList();
        return providerList.LastOrDefault(x => x is not IPlaceholderAIContextProvider) ?? providerList.Last();
    }
}
