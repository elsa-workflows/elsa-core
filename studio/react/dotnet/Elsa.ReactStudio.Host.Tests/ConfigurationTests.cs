using System.Text.Json.Nodes;
using Elsa.ReactStudio.Host;
using Xunit;

namespace Elsa.ReactStudio.Host.Tests;

public sealed class ConfigurationTests : IDisposable
{
    private readonly string _root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
    private readonly Dictionary<string, string> _environment = new();

    public ConfigurationTests()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(System.IO.Path.Combine(_root, "studio.config.json"),
            """{"title":"File title","servers":[{"url":"/elsa/api","tenantId":"tenant-a"}],"features":{"labels":{"options":{"colour":"blue"}}}}""");
    }

    private Task<JsonObject> Load() => StudioConfiguration.LoadAsync(_root, "studio.config.json",
        name => _environment.GetValueOrDefault(name), CancellationToken.None);

    [Fact]
    public async Task Configuration_file_changes_are_visible_without_rebuilding()
    {
        Assert.Equal("File title", (await Load())["title"]!.GetValue<string>());
        await File.WriteAllTextAsync(System.IO.Path.Combine(_root, "studio.config.json"), """{"title":"Changed","servers":[{"url":"/api"}]}""");
        Assert.Equal("Changed", (await Load())["title"]!.GetValue<string>());
    }

    [Fact]
    public async Task Inline_document_overrides_the_file_and_environment_overrides_inline()
    {
        _environment["STUDIO_CONFIG"] = """{"title":"Inline","servers":[{"url":"/inline"}]}""";
        _environment["STUDIO_TITLE"] = "Environment";
        _environment["STUDIO_SERVER_URL"] = "/environment";
        var config = await Load();
        Assert.Equal("Environment", config["title"]!.GetValue<string>());
        Assert.Equal("/environment", config["servers"]![0]!["url"]!.GetValue<string>());
    }

    [Fact]
    public async Task Environment_overlays_preserve_other_server_and_feature_options()
    {
        _environment["STUDIO_SERVER_URL"] = "/replacement";
        _environment["STUDIO_SERVER_NAME"] = "Runtime";
        _environment["STUDIO_FEATURES"] = "labels, secrets, ";
        var config = await Load();
        Assert.Equal("tenant-a", config["servers"]![0]!["tenantId"]!.GetValue<string>());
        Assert.Equal("Runtime", config["servers"]![0]!["name"]!.GetValue<string>());
        Assert.Equal("blue", config["features"]!["labels"]!["options"]!["colour"]!.GetValue<string>());
        Assert.True(config["features"]!["secrets"]!["enabled"]!.GetValue<bool>());
    }

    [Fact]
    public async Task Auth_overrides_preserve_configured_fields()
    {
        _environment["STUDIO_AUTH_PROVIDER"] = "oidc";
        _environment["STUDIO_OIDC_AUTHORITY"] = "https://identity.example.invalid";
        _environment["STUDIO_OIDC_CLIENT_ID"] = "studio";
        _environment["STUDIO_OIDC_SCOPE"] = "openid custom";
        _environment["STUDIO_API_KEY"] = "public-browser-key";
        var auth = (await Load())["auth"]!;
        Assert.Equal("oidc", auth["provider"]!.GetValue<string>());
        Assert.Equal("studio", auth["oidc"]!["clientId"]!.GetValue<string>());
        Assert.Equal("openid custom", auth["oidc"]!["scope"]!.GetValue<string>());
        Assert.Equal("public-browser-key", auth["apiKey"]!["key"]!.GetValue<string>());
    }

    [Fact]
    public async Task Explicit_file_path_is_resolved_from_content_root()
    {
        File.WriteAllText(System.IO.Path.Combine(_root, "alternate.json"), """{"servers":[{"url":"/alternate"}]}""");
        _environment["STUDIO_CONFIG_PATH"] = "alternate.json";
        Assert.Equal("/alternate", (await Load())["servers"]![0]!["url"]!.GetValue<string>());
    }

    [Fact]
    public async Task Missing_file_can_be_configured_using_environment_only()
    {
        File.Delete(System.IO.Path.Combine(_root, "studio.config.json"));
        _environment["STUDIO_SERVER_URL"] = "/api";
        Assert.Equal("/api", (await Load())["servers"]![0]!["url"]!.GetValue<string>());
    }

    [Theory]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"servers\":[]}")]
    [InlineData("{\"servers\":[{\"url\":\"\"}]}")]
    public async Task Unusable_server_configuration_is_rejected(string document)
    {
        _environment["STUDIO_CONFIG"] = document;
        await Assert.ThrowsAsync<InvalidDataException>(Load);
    }

    public void Dispose() => Directory.Delete(_root, true);
}
