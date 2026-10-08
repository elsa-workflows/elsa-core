using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Elsa.Studio.Login;
using Elsa.Studio.Login.Contracts;
using Elsa.Studio.Login.Models;
using Elsa.Studio.Login.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.BlazorServer;

public class LegacyOidcAuthorizationStateTests
{
    [Fact]
    public async Task MatchingState_ExchangesCodeOnceAndReturnsToOriginalLocalPath()
    {
        var (service, browser, navigation, handler, tokens) = CreateService();
        await service.RedirectToAuthorizationServer();
        var state = QueryHelpers.ParseQuery(new Uri(navigation.LastNavigation!).Query)["state"].ToString();

        await service.ReceiveAuthorizationCode("code", state, CancellationToken.None);

        Assert.Equal(1, handler.RequestCount);
        Assert.Equal("access", tokens.Values[TokenNames.AccessToken]);
        Assert.Equal("/workflows?view=mine", navigation.LastNavigation);
        Assert.Empty(browser.Storage);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReceiveAuthorizationCode("code", state, CancellationToken.None));
        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("wrong-state")]
    public async Task MissingOrMismatchedState_NeverExchangesCode(string? state)
    {
        var (service, browser, _, handler, tokens) = CreateService();
        await service.RedirectToAuthorizationServer();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReceiveAuthorizationCode("code", state, CancellationToken.None));

        Assert.Equal(0, handler.RequestCount);
        Assert.Empty(tokens.Values);
        Assert.Empty(browser.Storage);
    }

    [Fact]
    public async Task MissingCode_ConsumesStateWithoutTokenExchange()
    {
        var (service, browser, navigation, handler, _) = CreateService();
        await service.RedirectToAuthorizationServer();
        var state = QueryHelpers.ParseQuery(new Uri(navigation.LastNavigation!).Query)["state"].ToString();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReceiveAuthorizationCode(string.Empty, state, CancellationToken.None));

        Assert.Equal(0, handler.RequestCount);
        Assert.Empty(browser.Storage);
    }

    [Fact]
    public async Task ExpiredState_NeverExchangesCode()
    {
        var (service, browser, navigation, handler, _) = CreateService();
        await service.RedirectToAuthorizationServer();
        var state = QueryHelpers.ParseQuery(new Uri(navigation.LastNavigation!).Query)["state"].ToString();
        var stored = browser.Storage.Single();
        var pending = JsonNode.Parse(stored.Value)!.AsObject();
        pending["CreatedAt"] = JsonValue.Create(DateTimeOffset.UtcNow.AddMinutes(-11));
        browser.Storage[stored.Key] = pending.ToJsonString();

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReceiveAuthorizationCode("code", state, CancellationToken.None));

        Assert.Equal(0, handler.RequestCount);
        Assert.Empty(browser.Storage);
    }

    [Fact]
    public async Task UntrustedStoredReturnPath_IsNotUsedAsNavigationTarget()
    {
        var (service, browser, navigation, _, _) = CreateService();
        await service.RedirectToAuthorizationServer();
        var state = QueryHelpers.ParseQuery(new Uri(navigation.LastNavigation!).Query)["state"].ToString();
        var stored = browser.Storage.Single();
        var pending = JsonNode.Parse(stored.Value)!.AsObject();
        pending["ReturnPath"] = "//other.example";
        browser.Storage[stored.Key] = pending.ToJsonString();

        await service.ReceiveAuthorizationCode("code", state, CancellationToken.None);

        Assert.Equal("/", navigation.LastNavigation);
    }

    private static (OpenIdConnectAuthorizationService Service, TestJsRuntime Browser, TestNavigationManager Navigation, CountingHandler Handler, TestJwtAccessor Tokens) CreateService()
    {
        var browser = new TestJsRuntime();
        var navigation = new TestNavigationManager();
        var handler = new CountingHandler();
        var tokens = new TestJwtAccessor();
        var configuration = Microsoft.Extensions.Options.Options.Create(new OpenIdConnectConfiguration
        {
            AuthEndpoint = "https://identity.example/authorize",
            TokenEndpoint = "https://identity.example/token",
            EndSessionEndpoint = "https://identity.example/logout",
            ClientId = "studio"
        });
        var service = new OpenIdConnectAuthorizationService(tokens, configuration, navigation, new HttpClient(handler), new UnusedPkceStateService(), browser, NullLogger<OpenIdConnectAuthorizationService>.Instance);
        return (service, browser, navigation, handler, tokens);
    }

    private sealed class TestJsRuntime : IJSRuntime
    {
        public Dictionary<string, string> Storage { get; } = new();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            var key = (string)args![0]!;
            switch (identifier)
            {
                case "sessionStorage.setItem":
                    Storage[key] = (string)args[1]!;
                    return ValueTask.FromResult(default(TValue)!);
                case "sessionStorage.getItem":
                    Storage.TryGetValue(key, out var value);
                    return ValueTask.FromResult((TValue)(object?)value!);
                case "sessionStorage.removeItem":
                    Storage.Remove(key);
                    return ValueTask.FromResult(default(TValue)!);
                default:
                    throw new NotSupportedException(identifier);
            }
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public string? LastNavigation { get; private set; }

        public TestNavigationManager() => Initialize("https://studio.example/", "https://studio.example/workflows?view=mine");

        protected override void NavigateToCore(string uri, NavigationOptions options) => LastNavigation = uri;
    }

    private sealed class CountingHandler : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            Assert.Equal(HttpMethod.Post, request.Method);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"access\",\"refresh_token\":\"refresh\",\"id_token\":\"id\"}", Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TestJwtAccessor : IJwtAccessor
    {
        public Dictionary<string, string> Values { get; } = new();

        public ValueTask<string?> ReadTokenAsync(string name) => ValueTask.FromResult(Values.GetValueOrDefault(name));

        public ValueTask WriteTokenAsync(string name, string token)
        {
            Values[name] = token;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class UnusedPkceStateService : IOpenIdConnectPkceStateService
    {
        public Task<(string CodeChallenge, string Method)> GeneratePkceCodeChallenge() => throw new NotSupportedException();

        public Task<string> GetPkceCodeVerifier() => throw new NotSupportedException();
    }
}
