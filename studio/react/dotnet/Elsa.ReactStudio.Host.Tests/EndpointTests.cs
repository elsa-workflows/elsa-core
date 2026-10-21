using System.Net;
using System.Text.RegularExpressions;
using System.Text.Json;
using CShells.Resolution;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.ReactStudio.Host.Tests;

public sealed class EndpointTests : IAsyncLifetime
{
    private readonly string _configuration = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid() + ".json");
    private readonly WebApplicationFactory<Program> _factory;

    public EndpointTests()
    {
        File.WriteAllText(_configuration, """{"title":"Endpoint fixture","servers":[{"url":"/elsa/api"}]}""");
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ReactStudio:Path"] = "/custom-preview",
                    ["ReactStudio:ConfigurationFile"] = _configuration
                })));
    }

    [Fact]
    public async Task Preview_resolution_preserves_existing_host_endpoints_and_unrelated_paths()
    {
        using var client = _factory.CreateClient();
        // Activate the shell before checking: a default-shell fallback can otherwise
        // capture unrelated API requests only after the first Preview navigation.
        (await client.GetAsync("/custom-preview/")).EnsureSuccessStatusCode();
        var resolver = _factory.Services.GetRequiredService<IShellResolver>();
        var context = new ShellResolutionContext();
        context.Set(ShellResolutionContextKeys.Path, "/elsa/api/workflow-definitions");
        Assert.Null(await resolver.ResolveAsync(context));
        context.Set(ShellResolutionContextKeys.Path, "/custom-preview-other");
        Assert.Null(await resolver.ResolveAsync(context));
        context.Set(ShellResolutionContextKeys.Path, "/custom-preview/");
        Assert.NotNull(await resolver.ResolveAsync(context));
        var http = new DefaultHttpContext();
        http.SetEndpoint(new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "Existing host endpoint"));
        context.Set(ShellResolutionContextKeys.ProtocolContext, (HttpContext)http);
        Assert.Null(await resolver.ResolveAsync(context));
    }

    [Fact]
    public async Task Enabled_server_features_publish_versioned_module_assets_and_shared_imports()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/custom-preview/studio-manifest");
        response.EnsureSuccessStatusCode();
        Assert.True(response.Headers.CacheControl!.NoStore);
        using var manifest = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var modules = manifest.RootElement.GetProperty("modules").EnumerateArray().ToArray();
        Assert.Equal(16, modules.Length);
        Assert.Equal(16, modules.Select(module => module.GetProperty("id").GetString()).Distinct().Count());
        foreach (var module in modules)
        {
            Assert.Equal(1, module.GetProperty("hostApiVersion").GetInt32());
            Assert.Equal(1, module.GetProperty("sdkApiVersion").GetInt32());
            var entry = await client.GetAsync(module.GetProperty("entry").GetString());
            entry.EnsureSuccessStatusCode();
            Assert.Contains("javascript", entry.Content.Headers.ContentType!.MediaType!);
            Assert.Contains(entry.Headers.CacheControl!.Extensions, extension => extension.Name == "immutable");
            var css = await client.GetAsync(module.GetProperty("styles")[0].GetString());
            css.EnsureSuccessStatusCode();
            Assert.Equal("text/css", css.Content.Headers.ContentType!.MediaType);
        }
        var html = await client.GetStringAsync("/custom-preview/");
        var json = Regex.Match(html, "<script type=\"importmap\">(.*?)</script>").Groups[1].Value;
        using var map = JsonDocument.Parse(json);
        var shared = map.RootElement.GetProperty("imports");
        Assert.StartsWith("/_content/Elsa.ReactStudio.Host/", shared.GetProperty("react").GetString());
        Assert.True((await client.GetAsync(shared.GetProperty("react/jsx-runtime").GetString())).IsSuccessStatusCode);
        Assert.True((await client.GetAsync(shared.GetProperty("@elsa-workflows/react-studio-auth").GetString())).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Disabled_server_features_are_absent_from_manifest_and_asset_routes()
    {
        using var enabledClient = _factory.CreateClient();
        using var manifest = JsonDocument.Parse(await enabledClient.GetStringAsync("/custom-preview/studio-manifest"));
        var labels = manifest.RootElement.GetProperty("modules").EnumerateArray()
            .Single(module => module.GetProperty("id").GetString() == "labels");
        using var disabledFactory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?> { ["ReactStudio:Modules:0"] = "dashboard" })));
        using var client = disabledFactory.CreateClient();
        using var disabled = JsonDocument.Parse(await client.GetStringAsync("/custom-preview/studio-manifest"));
        var ids = disabled.RootElement.GetProperty("modules").EnumerateArray()
            .Select(module => module.GetProperty("id").GetString()).ToArray();
        Assert.DoesNotContain("labels", ids);
        Assert.Contains("shell", ids);
        Assert.Contains("workflow-definitions", ids);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(labels.GetProperty("entry").GetString())).StatusCode);
    }

    [Fact]
    public async Task Deep_links_use_the_configured_base_and_versioned_assets()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/custom-preview/workflows/definitions?search=example");
        response.EnsureSuccessStatusCode();
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("<base href=\"/custom-preview/\"", html);
        Assert.True(response.Headers.CacheControl!.NoStore);
        var asset = Regex.Match(html, "src=\"([^\"]+\\.js)\"").Groups[1].Value;
        Assert.StartsWith("/_content/Elsa.ReactStudio.Host/", asset);
        var assetResponse = await client.GetAsync(asset);
        assetResponse.EnsureSuccessStatusCode();
        Assert.Contains("javascript", assetResponse.Content.Headers.ContentType!.MediaType!);
        Assert.Contains(assetResponse.Headers.CacheControl!.Extensions, extension => extension.Name == "immutable");
        Assert.True((await assetResponse.Content.ReadAsByteArrayAsync()).Length > 1000);
    }

    [Theory]
    [InlineData("/custom-preview/assets/missing.js")]
    [InlineData("/custom-preview/missing.css")]
    [InlineData("/_content/Elsa.ReactStudio.Host/wrong-version/assets/missing.js")]
    [InlineData("/studio-preview")]
    [InlineData("/")]
    public async Task Missing_assets_and_unmapped_paths_remain_not_found(string path)
    {
        using var client = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
    }

    [Fact]
    public async Task Config_endpoint_reads_the_file_again_and_prevents_caching()
    {
        using var client = _factory.CreateClient();
        var first = await client.GetAsync("/custom-preview/studio-config");
        Assert.True(first.Headers.CacheControl!.NoStore);
        Assert.Contains("Endpoint fixture", await first.Content.ReadAsStringAsync());
        await File.WriteAllTextAsync(_configuration, """{"title":"Changed at runtime","servers":[{"url":"/elsa/api"}]}""");
        Assert.Contains("Changed at runtime", await client.GetStringAsync("/custom-preview/studio-config"));
    }

    [Fact]
    public async Task Browser_navigation_to_a_resource_name_with_dots_uses_the_shell()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Accept.ParseAdd("text/html");
        var response = await client.GetAsync("/custom-preview/secrets/Database.Password");
        response.EnsureSuccessStatusCode();
        Assert.Equal("text/html", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Invalid_configuration_does_not_echo_document_values()
    {
        await File.WriteAllTextAsync(_configuration, """{"private-marker":"do-not-echo","servers":[]} """);
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/custom-preview/studio-config");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.DoesNotContain("do-not-echo", await response.Content.ReadAsStringAsync());
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        File.Delete(_configuration);
    }
}
