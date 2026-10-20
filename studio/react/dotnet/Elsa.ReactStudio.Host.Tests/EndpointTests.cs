using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Elsa.ReactStudio.Host.Tests;

public sealed class EndpointTests : IDisposable
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

    public void Dispose()
    {
        _factory.Dispose();
        File.Delete(_configuration);
    }
}
