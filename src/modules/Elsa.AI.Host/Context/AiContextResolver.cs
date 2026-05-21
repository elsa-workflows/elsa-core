using Elsa.AI.Abstractions.Contracts;
using Elsa.AI.Abstractions.Models;

namespace Elsa.AI.Host.Context;

public class AiContextResolver(IEnumerable<IAiContextProvider> providers)
{
    private readonly Dictionary<string, IAiContextProvider> _providers = providers.ToDictionary(x => x.Kind, StringComparer.OrdinalIgnoreCase);

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
        context with { Summary = context.Summary.Replace("secret", "[redacted]", StringComparison.OrdinalIgnoreCase) };
}
