using Elsa.Http.Bookmarks;
using JetBrains.Annotations;

namespace Elsa.Http.Contexts;

[UsedImplicitly]
public record HttpEndpointRouteProviderContext(HttpEndpointBookmarkPayload Payload, string? TenantId, CancellationToken CancellationToken);