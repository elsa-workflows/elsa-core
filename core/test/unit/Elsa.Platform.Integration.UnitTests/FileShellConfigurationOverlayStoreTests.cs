using System.Text.Json.Nodes;
using Elsa.Platform.Integration.Options;
using Elsa.Platform.Integration.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Elsa.Platform.Integration.UnitTests;

public class FileShellConfigurationOverlayStoreTests : IAsyncLifetime
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"elsa-platform-overlay-{Guid.NewGuid():N}");
    private readonly FileShellConfigurationOverlayStore _store;
    private readonly string _overlayPath;

    public FileShellConfigurationOverlayStoreTests()
    {
        _overlayPath = Path.Combine(_directory, "platform-shell-overrides.json");
        var environment = Substitute.For<IHostEnvironment>();
        environment.ContentRootPath.Returns(_directory);
        _store = new FileShellConfigurationOverlayStore(
            Microsoft.Extensions.Options.Options.Create(new ElsaPlatformIntegrationOptions
            {
                ShellOverlayPath = "platform-shell-overrides.json"
            }),
            environment);
    }

    [Fact]
    public async Task ConfigureFeaturesAsync_WritesEnabledAndDisabledFeatureState()
    {
        var enabled = new Dictionary<string, System.Text.Json.JsonElement>
        {
            ["Http"] = System.Text.Json.JsonDocument.Parse("""{ "baseUrl": "https://example.com" }""").RootElement.Clone()
        };

        var changed = await _store.ConfigureFeaturesAsync("Default", enabled, ["Python"]);

        Assert.True(changed);
        var root = await ReadRootAsync();
        Assert.Equal("https://example.com", root["CShells"]!["Shells"]!["Default"]!["Features"]!["Http"]!["baseUrl"]!.GetValue<string>());
        Assert.Equal("Python", root["Elsa"]!["PlatformIntegration"]!["Shells"]!["Default"]!["DisabledFeatures"]![0]!.GetValue<string>());
    }

    [Fact]
    public async Task ConfigureSettingsAsync_MergesShellConfiguration()
    {
        var settings = System.Text.Json.JsonDocument.Parse("""{ "WebRouting": { "Path": "tenant-a" } }""").RootElement.Clone();

        var changed = await _store.ConfigureSettingsAsync("Default", settings);

        Assert.True(changed);
        var root = await ReadRootAsync();
        Assert.Equal("tenant-a", root["CShells"]!["Shells"]!["Default"]!["Configuration"]!["WebRouting"]!["Path"]!.GetValue<string>());
    }

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_directory);
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);

        return Task.CompletedTask;
    }

    private async Task<JsonNode> ReadRootAsync()
    {
        await using var stream = File.OpenRead(_overlayPath);
        return (await JsonNode.ParseAsync(stream))!;
    }
}
