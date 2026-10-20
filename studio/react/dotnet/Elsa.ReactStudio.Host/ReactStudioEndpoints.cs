using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Elsa.ReactStudio.Host;

/// <summary>Explicit opt-in endpoints for the React Studio Preview.</summary>
public static class ReactStudioEndpoints
{
    private static readonly Assembly AssetsAssembly = typeof(ReactStudioEndpoints).Assembly;
    private static readonly Dictionary<string, string> Assets = AssetsAssembly.GetManifestResourceNames()
        .ToDictionary(name => name.Replace('\\', '/'), name => name, StringComparer.Ordinal);
    private static readonly FileExtensionContentTypeProvider ContentTypes = new();
    private static readonly string Version = AssetsAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

    /// <summary>Maps Preview assets, runtime configuration and the History API fallback at the configured mount.</summary>
    public static IEndpointRouteBuilder MapReactStudio(this IEndpointRouteBuilder endpoints, Action<ReactStudioOptions>? configure = null)
    {
        var options = new ReactStudioOptions();
        configure?.Invoke(options);
        var path = options.Path.TrimEnd('/');
        if (path.Length > 0 && (!path.StartsWith('/') || path.Contains("..") ||
            path.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not '/' and not '-' and not '_')))
        {
            throw new ArgumentException("React Studio mount must be a site-relative path with letters, digits, slashes, hyphens or underscores.", nameof(configure));
        }

        endpoints.MapGet(path + "/studio-config", async (HttpContext context, IHostEnvironment host) =>
        {
            context.Response.Headers.CacheControl = "no-store";
            try
            {
                var config = await StudioConfiguration.LoadAsync(host.ContentRootPath, options.ConfigurationFile,
                    Environment.GetEnvironmentVariable, context.RequestAborted);
                return Results.Json(config);
            }
            catch (Exception exception) when (exception is JsonException or InvalidDataException or InvalidOperationException or IOException or ArgumentException)
            {
                // Configuration can contain API keys; neither response nor logs echo document values.
                context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("Elsa.ReactStudio.Host")
                    .LogError("React Studio configuration could not be resolved ({ErrorType})", exception.GetType().Name);
                return Results.Problem("React Studio configuration is invalid. Check the host configuration.");
            }
        });

        endpoints.MapGet($"/_content/Elsa.ReactStudio.Host/{Version}/{{**asset}}", (string? asset) => Asset(asset, true));
        endpoints.MapGet(path + "/{**studioPath}", async (HttpContext context, string? studioPath) =>
        {
            var resource = "ReactStudio/" + studioPath;
            if (Assets.ContainsKey(resource) && studioPath != "index.html")
            {
                return Asset(studioPath, false);
            }
            // Missing resources must never return HTML with a successful status.
            var navigation = context.Request.Headers.Accept.Any(value => value?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
            if (studioPath?.StartsWith("assets/", StringComparison.Ordinal) == true ||
                (studioPath != "index.html" && System.IO.Path.HasExtension(studioPath) && !navigation))
            {
                return Results.NotFound();
            }
            using var stream = AssetsAssembly.GetManifestResourceStream("ReactStudio/index.html");
            if (stream == null)
            {
                return Results.Problem("React Studio assets are missing.");
            }
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync(context.RequestAborted);
            var root = context.Request.PathBase + path + "/";
            var assetRoot = context.Request.PathBase + $"/_content/Elsa.ReactStudio.Host/{Version}/assets/";
            html = html.Replace("<base href=\"/\"", $"<base href=\"{HtmlEncoder.Default.Encode(root)}\"")
                .Replace("\"./assets/", $"\"{HtmlEncoder.Default.Encode(assetRoot)}");
            context.Response.Headers.CacheControl = "no-store";
            return Results.Content(html, "text/html; charset=utf-8");
        });
        return endpoints;
    }

    private static IResult Asset(string? asset, bool immutable)
    {
        if (string.IsNullOrEmpty(asset) || !Assets.ContainsKey("ReactStudio/" + asset))
        {
            return Results.NotFound();
        }
        var stream = AssetsAssembly.GetManifestResourceStream(Assets["ReactStudio/" + asset])!;
        ContentTypes.TryGetContentType(asset, out var contentType);
        return new AssetResult(stream, contentType ?? "application/octet-stream", immutable);
    }

    private sealed class AssetResult(Stream stream, string contentType, bool immutable) : IResult
    {
        public Task ExecuteAsync(HttpContext context)
        {
            context.Response.Headers.CacheControl = immutable ? "public,max-age=31536000,immutable" : "no-cache";
            return Results.Stream(stream, contentType).ExecuteAsync(context);
        }
    }
}
