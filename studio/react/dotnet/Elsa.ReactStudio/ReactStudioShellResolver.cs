using CShells;
using CShells.Resolution;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Elsa.ReactStudio;

// Keep existing host endpoints in their original service scope. In particular,
// API authentication and tenant services must never use the Preview shell's copy.
internal sealed class ReactStudioShellResolver(IConfiguration configuration) : IShellResolverStrategy
{
    public Task<ShellId?> ResolveAsync(ShellResolutionContext context, CancellationToken cancellationToken = default)
    {
        var http = context.Get<HttpContext>(ShellResolutionContextKeys.ProtocolContext);
        if (http?.GetEndpoint() != null)
        {
            // ShellMiddleware handles already matched shell-owned endpoints itself.
            return Task.FromResult<ShellId?>(null);
        }
        var path = new PathString(context.Get<string>(ShellResolutionContextKeys.Path) ?? "");
        var mount = (configuration["ReactStudio:Path"] ?? "/studio-preview").TrimEnd('/');
        var isPreview = mount.Length == 0 || path.StartsWithSegments(mount) || path.StartsWithSegments("/_content");
        return Task.FromResult(isPreview ? new ShellId("ReactStudio") : (ShellId?)null);
    }
}
