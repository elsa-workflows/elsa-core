using System.Net;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.Secrets.Menu;
using Xunit;

namespace Elsa.Studio.Secrets.Tests;

public class SecretsMenuTests
{
    [Theory]
    [InlineData("Elsa.Secrets")]
    [InlineData("Elsa.Secrets.ShellFeatures.Secrets")]
    public async Task CanonicalAndLegacyHostsExposeSecrets(string enabledFeature)
    {
        var provider = new TestFeatureProvider([enabledFeature]);
        var item = Assert.Single(await new SecretsMenu(provider).GetMenuItemsAsync());

        Assert.Equal("security/secrets", item.Href);
        Assert.Equal("Secrets", item.Text);
    }

    [Fact]
    public async Task BothHostNamesStillProduceOneMenuItem()
    {
        var provider = new TestFeatureProvider(["Elsa.Secrets", "Elsa.Secrets.ShellFeatures.Secrets"]);

        Assert.Single(await new SecretsMenu(provider).GetMenuItemsAsync());
    }

    [Theory]
    [InlineData("")]
    [InlineData("Other.Secrets")]
    [InlineData("Elsa.Secrets.Persistence")]
    [InlineData("elsa.secrets")]
    public async Task AbsentOrSimilarFeaturesDoNotExposeSecrets(string enabledFeature)
    {
        var provider = new TestFeatureProvider([enabledFeature]);

        Assert.Empty(await new SecretsMenu(provider).GetMenuItemsAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task FailedFeatureDiscoveryDoesNotExposeSecrets(HttpStatusCode status)
    {
        var provider = new TestFeatureProvider([], new HttpRequestException("Unavailable", null, status));

        Assert.Empty(await new SecretsMenu(provider).GetMenuItemsAsync());
    }

    [Fact]
    public async Task CancellationTokenIsForwardedToDiscovery()
    {
        using var source = new CancellationTokenSource();
        var provider = new TestFeatureProvider(["Elsa.Secrets.ShellFeatures.Secrets"]);

        Assert.Single(await new SecretsMenu(provider).GetMenuItemsAsync(source.Token));
        Assert.NotEmpty(provider.Tokens);
        Assert.All(provider.Tokens, token => Assert.Equal(source.Token, token));
    }

    private sealed class TestFeatureProvider(string[] enabledFeatures, Exception? error = null) : IRemoteFeatureProvider
    {
        public List<CancellationToken> Tokens { get; } = [];

        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default)
        {
            Tokens.Add(cancellationToken);
            return error is null
                ? Task.FromResult(enabledFeatures.Contains(featureName, StringComparer.Ordinal))
                : Task.FromException<bool>(error);
        }

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Menu feature queries use IsEnabledAsync.");
    }
}
