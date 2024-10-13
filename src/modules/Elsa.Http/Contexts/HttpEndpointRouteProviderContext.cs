using Elsa.Http.Bookmarks;
using JetBrains.Annotations;

namespace Elsa.Http.Contexts;

[UsedImplicitly]
public record HttpEndpointRouteProviderContext(HttpEndpointBookmarkStimulus Stimulus, string TenantId, CancellationToken CancellationToken);