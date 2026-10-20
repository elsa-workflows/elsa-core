using System.Security.Cryptography;
using System.Text.Json;
using CShells.AspNetCore.Features;
using CShells.Features;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Elsa.ReactStudio.Spike;

[ShellFeature("ReactStudioSpike")]
public sealed class PreviewFeature : IWebShellFeature
{
    private const string BasePath = "/studio-preview";
    private const string Version = "0.0.1-spike";

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints, IHostEnvironment? environment)
    {
        endpoints.MapGet(BasePath + "/manifest", (IEnumerable<StudioModule> modules, IConfiguration configuration) =>
        {
            var dev = !string.IsNullOrEmpty(configuration["Spike:ViteUrl"]);
            var all = modules.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
            return Results.Json(new
            {
                modules = all.Where(IsCompatible).Select(x => new
                {
                    x.Id,
                    x.Version,
                    entry = dev ? $"{BasePath}/vite/src/{x.Id}.ts" : $"{BasePath}/modules/{x.Id}/{x.Version}/entry.js",
                    css = dev ? null : $"{BasePath}/modules/{x.Id}/{x.Version}/style.css"
                }),
                diagnostics = all.Where(x => !IsCompatible(x)).Select(x => new
                {
                    x.Id,
                    message = $"Module {x.Id} requires host {x.HostMajor} and SDK {x.SdkMajor}; this preview provides host 0 and SDK 0."
                })
            });
        });

        endpoints.MapGet(BasePath + "/modules/{id}/{version}/{file}",
            (string id, string version, string file, IEnumerable<StudioModule> modules) =>
            {
                var module = modules.SingleOrDefault(x => x.Id == id && x.Version == version && IsCompatible(x));
                return module == null ? Results.NotFound() : Asset(module.Assembly, "module", file);
            });
        endpoints.MapGet(BasePath + "/{bundle:regex(^(shell|shared)$)}/{version}/{file}",
            (string bundle, string version, string file) => version == Version
                ? Asset(typeof(PreviewFeature).Assembly, bundle, file)
                : Results.NotFound());

        endpoints.MapGet(BasePath + "/{**path}", (HttpContext context, IConfiguration configuration) =>
        {
            var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
            var dev = !string.IsNullOrEmpty(configuration["Spike:ViteUrl"]);
            context.Response.Headers.ContentSecurityPolicy =
                $"default-src 'none'; script-src 'self' 'nonce-{nonce}'; style-src 'self' 'nonce-{nonce}'; connect-src 'self'; img-src 'self' data:; base-uri 'none'; frame-ancestors 'none'";
            context.Response.Headers.CacheControl = "no-store";
            var imports = new Dictionary<string, string>
            {
                ["react"] = $"{BasePath}/shared/{Version}/entry.js",
                ["react-dom/client"] = $"{BasePath}/shared/{Version}/entry.js",
                ["@tanstack/react-query"] = $"{BasePath}/shared/{Version}/entry.js",
                ["@elsa-workflows/react-studio-sdk"] = $"{BasePath}/shared/{Version}/entry.js"
            };
            var entry = dev ? BasePath + "/vite/src/shell.ts" : $"{BasePath}/shell/{Version}/entry.js";
            var hmr = dev ? $"<script type=\"module\" nonce=\"{nonce}\" src=\"{BasePath}/vite/@vite/client\"></script>" : "";
            var css = dev ? "" : $"<link rel=\"stylesheet\" href=\"{BasePath}/shell/{Version}/style.css\">";
            return Results.Content($$"""
                <!doctype html><html lang="en"><head><meta charset="utf-8">
                <title>React Studio modularity spike</title>
                <meta property="csp-nonce" nonce="{{nonce}}">
                <script type="importmap" nonce="{{nonce}}">{{JsonSerializer.Serialize(new { imports })}}</script>
                {{css}}{{hmr}}</head><body><div id="root"></div>
                <script type="module" nonce="{{nonce}}" src="{{entry}}"></script>
                </body></html>
                """, "text/html");
        });
    }

    private static bool IsCompatible(StudioModule module) => module.HostMajor == 0 && module.SdkMajor == 0;

    private static IResult Asset(System.Reflection.Assembly assembly, string prefix, string file)
    {
        if (file is not ("entry.js" or "style.css"))
        {
            return Results.NotFound();
        }
        var stream = assembly.GetManifestResourceStream($"{prefix}/{file}");
        return stream == null ? Results.NotFound() : Results.Stream(stream, file.EndsWith(".js") ? "text/javascript" : "text/css");
    }
}
