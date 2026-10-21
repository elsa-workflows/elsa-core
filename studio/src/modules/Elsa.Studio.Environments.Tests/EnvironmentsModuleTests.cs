using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Api.Client.Resources.Identity.Responses;
using Elsa.Studio.Authentication.ElsaIdentity;
using Elsa.Studio.Authentication.ElsaIdentity.Contracts;
using Elsa.Studio.Authentication.ElsaIdentity.HttpMessageHandlers;
using Elsa.Studio.Authentication.ElsaIdentity.Services;
using Elsa.Studio.Contracts;
using Elsa.Studio.Environments;
using Elsa.Studio.Environments.Contracts;
using Elsa.Studio.Environments.Extensions;
using Elsa.Studio.Environments.Models;
using Elsa.Studio.Environments.Services;
using Elsa.Studio.Extensions;
using Elsa.Studio.Models;
using Elsa.Studio.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Elsa.Studio.Environments.Tests;

/// <summary>
/// Regression coverage for elsa-workflows/elsa-studio#1039: Environments must switch backends by
/// updating <see cref="IRemoteBackendAccessor"/> and reusing <see cref="DefaultBackendApiClientProvider"/>,
/// not by replacing it with a private ServiceProvider factory.
/// </summary>
/// <remarks>
/// Also covers #1062: Blazor Server host startup must not call the environments client.
/// The list loads once from <see cref="IFeatureService.InitializeFeaturesAsync"/>.
/// </remarks>
public class EnvironmentsModuleTests : IDisposable
{
    private readonly RecordingHandler _primaryHandler = new();
    private readonly ServiceProvider _serviceProvider;

    public EnvironmentsModuleTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddCoreInternal();
        services.AddSingleton<IRemoteFeatureProvider, EmptyRemoteFeatureProvider>();

        var backendApiConfig = new BackendApiConfig
        {
            ConfigureBackendOptions = options => options.Url = new Uri("https://backend.example/"),
            ConfigureHttpClientBuilder = options => options.AuthenticationHandler = typeof(StampingAuthenticationHandler)
        };

        services.AddRemoteBackend(backendApiConfig);
        services.AddEnvironmentsModule(backendApiConfig);
        services.AddHttpClient(nameof(IEnvironmentsClient)).ConfigurePrimaryHttpMessageHandler(() => _primaryHandler);

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider.Dispose();

    [Fact]
    public void AddEnvironmentsModule_KeepsDefaultBackendApiClientProvider()
    {
        var provider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();
        var accessor = _serviceProvider.GetRequiredService<IRemoteBackendAccessor>();

        Assert.IsType<DefaultBackendApiClientProvider>(provider);
        Assert.IsType<EnvironmentRemoteBackendAccessor>(accessor);
        Assert.Null(typeof(EnvironmentRemoteBackendAccessor).Assembly.GetType(
            "Elsa.Studio.Environments.Services.EnvironmentBackendApiClientProvider"));
    }

    [Fact]
    public void BackendContextKey_ChangesWhenSameUrlEnvironmentIsSelected()
    {
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        var sameUrl = new Uri("https://shared.example/");
        environments.SetEnvironments(
        [
            new ServerEnvironment { Name = "Tenant A", Url = sameUrl },
            new ServerEnvironment { Name = "Tenant B", Url = sameUrl }
        ], "Tenant A");
        var apiClientProvider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();
        var initialContextKey = apiClientProvider.ContextKey;

        environments.SetCurrentEnvironment("Tenant B");

        Assert.Equal(sameUrl, apiClientProvider.Url);
        Assert.NotEqual(initialContextKey, apiClientProvider.ContextKey);
    }

    [Fact]
    public void BackendContextKey_ChangesWhenCircuitPermissionRefreshSignalIsRaised()
    {
        var provider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();
        var backendContextKey = _serviceProvider.GetRequiredService<IRemoteBackendAccessor>().ContextKey;
        var initialContextKey = provider.ContextKey;

        _serviceProvider.GetRequiredService<IPermissionRefreshSignal>().Raise();

        Assert.StartsWith($"{backendContextKey}|auth:", initialContextKey, StringComparison.Ordinal);
        Assert.NotEqual(initialContextKey, provider.ContextKey);
    }

    [Fact]
    public async Task AuthenticatedWriteIsStoppedWhenTokenAcquisitionChangesIdentity()
    {
        using var harness = AuthenticatedRequestHarness.Create();
        using var invoker = harness.CreateInvoker();
        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/features");
        Assert.True(harness.Binding.SupportsAuthenticatedRequestContextBinding);
        var originalContextKey = harness.BackendProvider.ContextKey;
        using var binding = harness.Binding.BindRequestContext(originalContextKey);
        harness.TokenAccessor.Token = harness.CreateToken("user-a", DateTimeOffset.UtcNow.AddMinutes(-10));
        harness.RefreshTokenService.RefreshedToken = harness.CreateToken("user-b", DateTimeOffset.UtcNow.AddHours(1));

        await Assert.ThrowsAsync<BackendRequestContextChangedException>(() =>
            SendAndDisposeAsync(invoker, HttpMethod.Post, "https://backend.example/workflow-definitions"));

        Assert.NotEqual(originalContextKey, harness.BackendProvider.ContextKey);
        Assert.Equal(1, harness.InnerHandler.RequestCount);
        Assert.True(harness.Binding.SupportsAuthenticatedRequestContextBinding);
    }

    [Fact]
    public async Task AuthenticatedWriteIsStoppedWhenStillValidStoredTokenChangesIdentity()
    {
        using var harness = AuthenticatedRequestHarness.Create();
        using var invoker = harness.CreateInvoker();
        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/features");
        var originalContextKey = harness.BackendProvider.ContextKey;
        using var binding = harness.Binding.BindRequestContext(originalContextKey);
        harness.TokenAccessor.Token = harness.CreateToken("user-b", harness.TokenExpiresAt);
        using var writeRequest = new HttpRequestMessage(HttpMethod.Post, "https://backend.example/workflow-definitions");

        await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => invoker.SendAsync(writeRequest, CancellationToken.None));

        Assert.NotEqual(originalContextKey, harness.BackendProvider.ContextKey);
        Assert.Null(writeRequest.Headers.Authorization);
        Assert.Equal(1, harness.InnerHandler.RequestCount);
    }

    [Fact]
    public async Task ConcurrentBoundWriteIsStoppedWhileIdentityChangeSignalIsBlocked()
    {
        var signalRaised = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseSignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = AuthenticatedRequestHarness.Create(signal => signal.Raised += () =>
        {
            signalRaised.TrySetResult();
            if (!releaseSignal.Task.Wait(TimeSpan.FromSeconds(5)))
                throw new TimeoutException("The test did not release the blocked permission refresh signal.");
        });
        using var invoker = harness.CreateInvoker();

        // Qualify the actual ElsaIdentity transport and observe identity A before binding.
        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/features");
        var originalContextKey = harness.BackendProvider.ContextKey;
        using var binding = harness.Binding.BindRequestContext(originalContextKey);
        harness.TokenAccessor.Token = harness.CreateToken("user-b", harness.TokenExpiresAt);

        using var firstWrite = new HttpRequestMessage(HttpMethod.Post, "https://backend.example/workflow-definitions");
        var firstWriteTask = Task.Run(() => invoker.SendAsync(firstWrite, CancellationToken.None));
        Exception? firstWriteError = null;

        try
        {
            await signalRaised.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.NotEqual(originalContextKey, harness.BackendProvider.ContextKey);

            // The first call has stored B's fingerprint but has not reached the provider's
            // signal subscriber. A second B observation must already see the invalidation.
            using var secondWrite = new HttpRequestMessage(HttpMethod.Post, "https://backend.example/workflow-definitions");
            await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => invoker.SendAsync(secondWrite, CancellationToken.None));

            Assert.Null(secondWrite.Headers.Authorization);
            Assert.Equal(1, harness.InnerHandler.RequestCount);
        }
        finally
        {
            releaseSignal.TrySetResult();
            try
            {
                await firstWriteTask.WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (Exception exception)
            {
                firstWriteError = exception;
            }
        }

        Assert.IsType<BackendRequestContextChangedException>(firstWriteError);
    }

    [Fact]
    public void BoundValidationRejectsAReplacedAndRestoredTransportSource()
    {
        using var harness = AuthenticatedRequestHarness.Create();
        var sourceA = new TestEpochSource();
        var sourceB = new TestEpochSource();
        var initialContextKey = harness.BackendProvider.ContextKey;

        harness.BackendProvider.RegisterAuthenticatedRequestTransport(sourceA);
        var sourceAContextKey = harness.BackendProvider.ContextKey;
        Assert.Equal(initialContextKey, sourceAContextKey);
        using var binding = harness.Binding.BindRequestContext(sourceAContextKey);

        harness.BackendProvider.RegisterAuthenticatedRequestTransport(sourceB);
        var sourceBContextKey = harness.BackendProvider.ContextKey;
        Assert.NotEqual(sourceAContextKey, sourceBContextKey);

        harness.BackendProvider.RegisterAuthenticatedRequestTransport(sourceA);
        var restoredSourceAContextKey = harness.BackendProvider.ContextKey;
        Assert.NotEqual(sourceAContextKey, restoredSourceAContextKey);
        Assert.NotEqual(sourceBContextKey, restoredSourceAContextKey);

        using (harness.Binding.BindRequestContext(restoredSourceAContextKey))
        {
            Assert.Throws<BackendRequestContextChangedException>(() =>
                harness.BackendProvider.ValidateBoundRequestContext(sourceB));
            harness.BackendProvider.ValidateBoundRequestContext(sourceA);
        }

        harness.BackendProvider.RegisterAuthenticatedRequestTransport(null);
        var contextWithoutTransport = harness.BackendProvider.ContextKey;
        Assert.NotEqual(restoredSourceAContextKey, contextWithoutTransport);
        Assert.False(harness.BackendProvider.SupportsAuthenticatedRequestContextBinding);
        harness.BackendProvider.RegisterAuthenticatedRequestTransport(sourceA);
        Assert.NotEqual(contextWithoutTransport, harness.BackendProvider.ContextKey);
        Assert.True(harness.BackendProvider.SupportsAuthenticatedRequestContextBinding);
    }

    [Fact]
    public async Task RequestRejectsTokenSourceWhenGlobalTransportIsRestoredBeforeValidation()
    {
        var signal = new PermissionRefreshSignal();
        var blazorServiceAccessor = new BlazorServiceAccessor();
        using var clientServices = new ServiceCollection().BuildServiceProvider();
        using var backendProvider = new DefaultBackendApiClientProvider(
            new FixedRemoteBackendAccessor(),
            blazorServiceAccessor,
            clientServices,
            null,
            signal);
        using var tokenProvider = new RestoringTokenProvider(backendProvider, new TestEpochSource(), signal);
        using var services = new ServiceCollection()
            .AddSingleton<ITokenProvider>(tokenProvider)
            .AddSingleton<IBackendApiClientProvider>(backendProvider)
            .BuildServiceProvider();
        blazorServiceAccessor.Services = services;
        var sourceA = new TestEpochSource();
        backendProvider.RegisterAuthenticatedRequestTransport(sourceA);
        using var binding = backendProvider.BindRequestContext(backendProvider.ContextKey);
        var innerHandler = new RecordingHandler();
        using var handler = new ElsaIdentityAuthenticatingApiHttpMessageHandler(blazorServiceAccessor)
        {
            InnerHandler = innerHandler
        };
        using var invoker = new HttpMessageInvoker(handler);

        await Assert.ThrowsAsync<BackendRequestContextChangedException>(() =>
            SendAndDisposeAsync(invoker, HttpMethod.Post, "https://backend.example/workflow-definitions"));

        Assert.Equal(0, innerHandler.RequestCount);
        Assert.True(backendProvider.SupportsAuthenticatedRequestContextBinding);
    }

    [Theory]
    [InlineData("tenant-b", "stamp-a")]
    [InlineData("tenant-a", "stamp-b")]
    public async Task AuthenticatedWriteIsStoppedWhenNonSubjectSecurityClaimChanges(string tenantId, string securityStamp)
    {
        using var harness = AuthenticatedRequestHarness.Create();
        using var invoker = harness.CreateInvoker();
        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/features");
        var originalContextKey = harness.BackendProvider.ContextKey;
        using var binding = harness.Binding.BindRequestContext(originalContextKey);
        harness.TokenAccessor.Token = harness.CreateToken("user-a", harness.TokenExpiresAt, new Dictionary<string, object?>
        {
            ["tenant_id"] = tenantId,
            ["security_stamp"] = securityStamp
        });
        using var writeRequest = new HttpRequestMessage(HttpMethod.Post, "https://backend.example/workflow-definitions");

        await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => invoker.SendAsync(writeRequest, CancellationToken.None));

        Assert.NotEqual(originalContextKey, harness.BackendProvider.ContextKey);
        Assert.Null(writeRequest.Headers.Authorization);
        Assert.Equal(1, harness.InnerHandler.RequestCount);
    }

    [Fact]
    public async Task AuthenticatedWriteAllowsRefreshThatOnlyChangesLifecycleClaims()
    {
        using var harness = AuthenticatedRequestHarness.Create();
        using var invoker = harness.CreateInvoker();
        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/features");
        var originalContextKey = harness.BackendProvider.ContextKey;
        using var binding = harness.Binding.BindRequestContext(originalContextKey);
        var lifecycleClaims = new Dictionary<string, object?>
        {
            ["iat"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["nbf"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            ["jti"] = Guid.NewGuid().ToString("N")
        };
        harness.TokenAccessor.Token = harness.CreateToken("user-a", DateTimeOffset.UtcNow.AddMinutes(-10), lifecycleClaims);
        harness.RefreshTokenService.RefreshedToken = harness.CreateToken("user-a", DateTimeOffset.UtcNow.AddHours(1), lifecycleClaims);

        await SendAndDisposeAsync(invoker, HttpMethod.Post, "https://backend.example/workflow-definitions");

        Assert.Equal(originalContextKey, harness.BackendProvider.ContextKey);
        Assert.Equal(2, harness.InnerHandler.RequestCount);
    }

    [Fact]
    public async Task MissingTokenStopsBoundWriteAndPreservesOrdinaryUnauthenticatedRequest()
    {
        using var harness = AuthenticatedRequestHarness.Create();
        using var invoker = harness.CreateInvoker();
        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/features");
        using (harness.Binding.BindRequestContext(harness.BackendProvider.ContextKey))
        {
            harness.TokenAccessor.Token = null;
            await Assert.ThrowsAsync<BackendRequestContextChangedException>(() =>
                SendAndDisposeAsync(invoker, HttpMethod.Post, "https://backend.example/workflow-definitions"));
        }

        Assert.Equal(1, harness.InnerHandler.RequestCount);
        using var ordinaryRequest = new HttpRequestMessage(HttpMethod.Get, "https://backend.example/ordinary");
        await invoker.SendAsync(ordinaryRequest, CancellationToken.None);

        Assert.Equal(2, harness.InnerHandler.RequestCount);
        Assert.Null(ordinaryRequest.Headers.Authorization);
    }

    [Fact]
    public async Task UnparseableTokenStopsBoundWriteWithoutChangingOrdinaryTokenHandling()
    {
        using var harness = AuthenticatedRequestHarness.Create();
        using var invoker = harness.CreateInvoker();
        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/features");
        using var writeRequest = new HttpRequestMessage(HttpMethod.Post, "https://backend.example/workflow-definitions");
        using (harness.Binding.BindRequestContext(harness.BackendProvider.ContextKey))
        {
            harness.TokenAccessor.Token = "invalid-token";
            await Assert.ThrowsAsync<BackendRequestContextChangedException>(() => invoker.SendAsync(writeRequest, CancellationToken.None));
        }

        Assert.Equal(1, harness.InnerHandler.RequestCount);
        Assert.Null(writeRequest.Headers.Authorization);
        using var ordinaryRequest = new HttpRequestMessage(HttpMethod.Get, "https://backend.example/ordinary");
        await invoker.SendAsync(ordinaryRequest, CancellationToken.None);

        Assert.Equal(2, harness.InnerHandler.RequestCount);
        Assert.Equal("Bearer", ordinaryRequest.Headers.Authorization?.Scheme);
    }

    [Fact]
    public async Task AuthenticatedWriteUsesStableIdentityAndNestedBindingsRestore()
    {
        using var harness = AuthenticatedRequestHarness.Create();
        using var invoker = harness.CreateInvoker();
        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/features");
        var contextKey = harness.BackendProvider.ContextKey;
        using (harness.Binding.BindRequestContext(contextKey))
        {
            Assert.True(harness.Binding.HasActiveRequestContextBinding);
            using (harness.Binding.BindRequestContext(contextKey))
                await SendAndDisposeAsync(invoker, HttpMethod.Post, "https://backend.example/workflow-definitions");
            Assert.True(harness.Binding.HasActiveRequestContextBinding);
        }

        Assert.False(harness.Binding.HasActiveRequestContextBinding);
        Assert.Equal(2, harness.InnerHandler.RequestCount);
    }

    [Fact]
    public async Task LegacyUnboundAuthenticatedRequestRemainsCompatible()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ITokenProvider>(new StaticTokenProvider());
        services.AddSingleton<IBlazorServiceAccessor, BlazorServiceAccessor>();
        using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IBlazorServiceAccessor>();
        accessor.Services = provider;
        var innerHandler = new RecordingHandler();
        using var handler = new ElsaIdentityAuthenticatingApiHttpMessageHandler(accessor) { InnerHandler = innerHandler };
        using var invoker = new HttpMessageInvoker(handler);

        await SendAndDisposeAsync(invoker, HttpMethod.Get, "https://backend.example/ordinary");

        Assert.Equal(1, innerHandler.RequestCount);
        Assert.Equal("Bearer", innerHandler.LastRequest!.Headers.Authorization?.Scheme);
    }

    private static async Task SendAndDisposeAsync(HttpMessageInvoker invoker, HttpMethod method, string url)
    {
        using var request = new HttpRequestMessage(method, url);
        using var response = await invoker.SendAsync(request, CancellationToken.None);
    }

    private sealed class AuthenticatedRequestHarness : IDisposable
    {
        private readonly ServiceProvider _services;
        private readonly BlazorServiceAccessor _blazorServiceAccessor;

        private AuthenticatedRequestHarness(
            ServiceProvider services,
            BlazorServiceAccessor blazorServiceAccessor,
            DefaultBackendApiClientProvider backendProvider,
            MutableJwtAccessor tokenAccessor,
            RefreshTokenService refreshTokenService,
            DateTimeOffset tokenExpiresAt)
        {
            _services = services;
            _blazorServiceAccessor = blazorServiceAccessor;
            BackendProvider = backendProvider;
            Binding = backendProvider;
            TokenAccessor = tokenAccessor;
            RefreshTokenService = refreshTokenService;
            TokenExpiresAt = tokenExpiresAt;
            InnerHandler = new RecordingHandler();
        }

        public DefaultBackendApiClientProvider BackendProvider { get; }
        public IBackendRequestContextBinding Binding { get; }
        public MutableJwtAccessor TokenAccessor { get; }
        public RefreshTokenService RefreshTokenService { get; }
        public RecordingHandler InnerHandler { get; }
        public DateTimeOffset TokenExpiresAt { get; }

        public static AuthenticatedRequestHarness Create(Action<IPermissionRefreshSignal>? registerBeforeBackendProvider = null)
        {
            var tokenExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
            var initialToken = CreateTokenPayload("user-a", tokenExpiresAt, new Dictionary<string, object?>
            {
                ["tenant_id"] = "tenant-a",
                ["security_stamp"] = "stamp-a"
            });
            var tokenAccessor = new MutableJwtAccessor(initialToken);
            var refreshTokenService = new RefreshTokenService(tokenAccessor);
            var signal = new PermissionRefreshSignal();
            registerBeforeBackendProvider?.Invoke(signal);
            var blazorServiceAccessor = new BlazorServiceAccessor();
            var services = new ServiceCollection();
            services.AddSingleton<IPermissionRefreshSignal>(signal);
            services.AddSingleton<IRemoteBackendAccessor>(new FixedRemoteBackendAccessor());
            services.AddSingleton<IBlazorServiceAccessor>(blazorServiceAccessor);
            services.AddSingleton<ITokenProvider>(new JwtTokenProvider(
                tokenAccessor, new JwtParser(), new ImmediateSingleFlightCoordinator(), refreshTokenService, [signal]));
            services.AddSingleton<IBackendApiClientProvider>(serviceProvider => new DefaultBackendApiClientProvider(
                serviceProvider.GetRequiredService<IRemoteBackendAccessor>(),
                serviceProvider.GetRequiredService<IBlazorServiceAccessor>(),
                serviceProvider,
                null,
                signal));
            var provider = services.BuildServiceProvider();
            blazorServiceAccessor.Services = provider;

            return new AuthenticatedRequestHarness(
                provider,
                blazorServiceAccessor,
                (DefaultBackendApiClientProvider)provider.GetRequiredService<IBackendApiClientProvider>(),
                tokenAccessor,
                refreshTokenService,
                tokenExpiresAt);
        }

        public HttpMessageInvoker CreateInvoker() => new(new ElsaIdentityAuthenticatingApiHttpMessageHandler(_blazorServiceAccessor)
        {
            InnerHandler = InnerHandler
        });

        public string CreateToken(string subject, DateTimeOffset expiresAt, IReadOnlyDictionary<string, object?>? additionalClaims = null) =>
            CreateTokenPayload(subject, expiresAt, additionalClaims);

        public void Dispose()
        {
            _blazorServiceAccessor.Services = null!;
            _services.Dispose();
        }

        private static string CreateTokenPayload(string subject, DateTimeOffset expiresAt, IReadOnlyDictionary<string, object?>? additionalClaims)
        {
            var claims = new Dictionary<string, object?>
            {
                ["sub"] = subject,
                ["permissions"] = new[] { "workflow.write" },
                ["exp"] = expiresAt.ToUnixTimeSeconds(),
                ["tenant_id"] = "tenant-a",
                ["security_stamp"] = "stamp-a"
            };
            if (additionalClaims != null)
            {
                foreach (var (type, value) in additionalClaims)
                    claims[type] = value;
            }

            var payload = JsonSerializer.Serialize(claims);
            var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
            return $"header.{encoded}.signature";
        }
    }

    private sealed class FixedRemoteBackendAccessor : IRemoteBackendAccessor
    {
        public RemoteBackend RemoteBackend { get; } = new(new Uri("https://backend.example/"));
        public string ContextKey => $"tenant-A|{RemoteBackend.Url}";
    }

    private sealed class MutableJwtAccessor(string? token) : IJwtAccessor
    {
        public string? Token { get; set; } = token;

        public ValueTask<string?> ReadTokenAsync(string name) => ValueTask.FromResult(Token);

        public ValueTask WriteTokenAsync(string name, string token)
        {
            Token = token;
            return ValueTask.CompletedTask;
        }

        public ValueTask ClearTokenAsync(string name)
        {
            Token = null;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class RefreshTokenService(MutableJwtAccessor jwtAccessor) : IRefreshTokenService
    {
        public string RefreshedToken { get; set; } = string.Empty;

        public async Task<LoginResponse> RefreshTokenAsync(CancellationToken cancellationToken)
        {
            await jwtAccessor.WriteTokenAsync(TokenNames.AccessToken, RefreshedToken);
            return new LoginResponse(true, RefreshedToken, "refresh-token");
        }
    }

    private sealed class ImmediateSingleFlightCoordinator : ISingleFlightCoordinator
    {
        public Task<T> RunAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) => action(cancellationToken);
    }

    private sealed class StaticTokenProvider : ITokenProvider
    {
        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>("ordinary-test-token");
    }

    private sealed class TestEpochSource : IBackendRequestContextEpochSource
    {
        public long IdentityGeneration { get; set; }

        public bool UsesPermissionRefreshSignal(IPermissionRefreshSignal signal) => true;
    }

    private sealed class RestoringTokenProvider(
        DefaultBackendApiClientProvider backendProvider,
        IBackendRequestContextEpochSource restoredSource,
        IPermissionRefreshSignal permissionRefreshSignal) : ITokenProvider, IBackendRequestContextEpochSource, IDisposable
    {
        private IDisposable? _restoredBinding;

        public long IdentityGeneration => 0;

        public bool UsesPermissionRefreshSignal(IPermissionRefreshSignal signal) => ReferenceEquals(permissionRefreshSignal, signal);

        public Task<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            backendProvider.RegisterAuthenticatedRequestTransport(restoredSource);
            _restoredBinding = backendProvider.BindRequestContext(backendProvider.ContextKey);
            return Task.FromResult<string?>("test-token");
        }

        public void Dispose() => Interlocked.Exchange(ref _restoredBinding, null)?.Dispose();
    }

    [Fact]
    public void AddEnvironmentsModule_DoesNotRegisterStartupTask()
    {
        Assert.Empty(_serviceProvider.GetServices<IStartupTask>());
    }

    [Fact]
    public async Task BlazorServerStartupPath_DoesNotCallEnvironmentsClient()
    {
        // Arrange: same path as RunStartupTasksHostedService during Host.StartAsync.
        var runner = _serviceProvider.GetRequiredService<IStartupTaskRunner>();

        // Act
        await runner.RunStartupTasksAsync();

        // Assert
        Assert.Null(_primaryHandler.LastRequest);
        Assert.Equal(0, _primaryHandler.RequestCount);
        Assert.Empty(_serviceProvider.GetRequiredService<IEnvironmentService>().Environments);
    }

    [Fact]
    public async Task FeatureInitialize_OnlyAddsThePicker()
    {
        var feature = _serviceProvider.GetServices<IFeature>().OfType<Feature>().Single();

        await feature.InitializeAsync();

        Assert.Equal(0, _primaryHandler.RequestCount);
        Assert.Empty(_serviceProvider.GetRequiredService<IEnvironmentService>().Environments);
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task InitializeFeatures_LoadsEnvironmentsOnce()
    {
        // Arrange
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" },
                { "name": "Prod", "url": "https://prod.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        var features = _serviceProvider.GetRequiredService<IFeatureService>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await features.InitializeFeaturesAsync();
        await features.InitializeFeaturesAsync();

        // Assert
        Assert.Equal(1, _primaryHandler.RequestCount);
        Assert.Equal(["Dev", "Prod"], environments.Environments.Select(environment => environment.Name));
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
        Assert.Equal(new Uri("https://dev.example/"), environments.CurrentEnvironment?.Url);
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task EnvironmentLoader_EnsureLoadedAsync_LoadsOnce()
    {
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Staging", "url": "https://staging.example/" }
              ],
              "defaultEnvironmentName": "Staging"
            }
            """;

        var loader = _serviceProvider.GetRequiredService<EnvironmentLoader>();

        await loader.EnsureLoadedAsync();
        await loader.EnsureLoadedAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Equal(1, _primaryHandler.RequestCount);
        Assert.Equal("Staging", environments.CurrentEnvironment?.Name);
        Assert.Contains(environments.Environments, environment => environment.Name == "Staging");
    }

    [Fact]
    public async Task EnvironmentLoader_FailedLoad_IsNotCachedAndRetries()
    {
        // Arrange
        _primaryHandler.ThrowOnSend = new InvalidOperationException("The environments API is unreachable.");
        var loader = _serviceProvider.GetRequiredService<EnvironmentLoader>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.Empty(environments.Environments);
        var failedRequests = _primaryHandler.RequestCount;
        Assert.True(failedRequests >= 1);

        // Arrange: backend recovers
        _primaryHandler.ThrowOnSend = null;
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.True(_primaryHandler.RequestCount > failedRequests);
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
    }

    [Fact]
    public async Task EnvironmentLoader_HttpClientTimeout_DoesNotThrowAndRetries()
    {
        // Arrange: HttpClient.Timeout surfaces as TaskCanceledException while the caller token is still open.
        _primaryHandler.ThrowOnSend = new TaskCanceledException(
            "The request was canceled due to the configured HttpClient.Timeout.");
        var loader = _serviceProvider.GetRequiredService<EnvironmentLoader>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.Empty(environments.Environments);
        var timedOutRequests = _primaryHandler.RequestCount;
        Assert.True(timedOutRequests >= 1);

        // Arrange: backend recovers
        _primaryHandler.ThrowOnSend = null;
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.True(_primaryHandler.RequestCount > timedOutRequests);
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
    }

    [Fact]
    public async Task EnvironmentLoader_CallerCancellation_PropagatesAndIsNotCached()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var loader = _serviceProvider.GetRequiredService<EnvironmentLoader>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act / Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => loader.EnsureLoadedAsync(cts.Token));
        Assert.Empty(environments.Environments);

        // Arrange: a later uncancelled call must still load.
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Dev", "url": "https://dev.example/" }
              ],
              "defaultEnvironmentName": "Dev"
            }
            """;

        // Act
        await loader.EnsureLoadedAsync();

        // Assert
        Assert.True(_primaryHandler.RequestCount >= 1);
        Assert.Equal("Dev", environments.CurrentEnvironment?.Name);
    }

    [Fact]
    public async Task InitializeFeatures_FillsEnvironmentsForThePicker()
    {
        _primaryHandler.ResponseContent = """
            {
              "environments": [
                { "name": "Staging", "url": "https://staging.example/" }
              ],
              "defaultEnvironmentName": "Staging"
            }
            """;

        await _serviceProvider.GetRequiredService<IFeatureService>().InitializeFeaturesAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Equal("Staging", environments.CurrentEnvironment?.Name);
        Assert.Contains(environments.Environments, environment => environment.Name == "Staging");
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task InitializeFeatures_FailedLoad_MakesOneAttempt()
    {
        // Arrange
        _primaryHandler.ThrowOnSend = new InvalidOperationException("The environments API is unreachable.");
        var features = _serviceProvider.GetRequiredService<IFeatureService>();
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();

        // Act
        await features.InitializeFeaturesAsync();

        // Assert
        Assert.Equal(1, _primaryHandler.RequestCount);
        Assert.Empty(environments.Environments);
        Assert.Null(environments.CurrentEnvironment);
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task InitializeFeatures_DoesNotThrowWhenEnvironmentsApiIsMissing()
    {
        _primaryHandler.StatusCode = HttpStatusCode.NotFound;

        await _serviceProvider.GetRequiredService<IFeatureService>().InitializeFeaturesAsync();

        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        Assert.Empty(environments.Environments);
        Assert.NotEmpty(_serviceProvider.GetRequiredService<IAppBarService>().AppBarElements);
    }

    [Fact]
    public async Task SwitchingEnvironment_UpdatesAccessorUrlUsedByDefaultProvider()
    {
        var environments = _serviceProvider.GetRequiredService<IEnvironmentService>();
        environments.SetEnvironments([
            new ServerEnvironment { Name = "Dev", Url = new Uri("https://dev.example/") },
            new ServerEnvironment { Name = "Prod", Url = new Uri("https://prod.example/") }
        ], "Dev");

        var accessor = _serviceProvider.GetRequiredService<IRemoteBackendAccessor>();
        var provider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();

        Assert.Equal(new Uri("https://dev.example/"), accessor.RemoteBackend.Url);
        Assert.Equal(new Uri("https://dev.example/"), provider.Url);

        environments.SetCurrentEnvironment("Prod");

        Assert.Equal(new Uri("https://prod.example/"), accessor.RemoteBackend.Url);
        Assert.Equal(new Uri("https://prod.example/"), provider.Url);

        var api = await provider.GetApiAsync<IEnvironmentsClient>();
        await api.ListEnvironmentsAsync();
        Assert.StartsWith("https://prod.example/", _primaryHandler.LastRequest!.RequestUri!.ToString());
    }

    [Fact]
    public async Task EnvironmentsClient_CarriesTheHostsConfiguredAuthenticationHandler()
    {
        _primaryHandler.ResponseContent = """{ "environments": [], "defaultEnvironmentName": null }""";

        var provider = _serviceProvider.GetRequiredService<IBackendApiClientProvider>();
        var api = await provider.GetApiAsync<IEnvironmentsClient>();
        await api.ListEnvironmentsAsync();

        Assert.NotNull(_primaryHandler.LastRequest);
        Assert.Equal("stamped", _primaryHandler.LastRequest!.Headers.GetValues(StampingAuthenticationHandler.HeaderName).Single());
    }

    private sealed class StampingAuthenticationHandler : DelegatingHandler
    {
        public const string HeaderName = "X-Test-Authenticated";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(HeaderName, "stamped");
            return base.SendAsync(request, cancellationToken);
        }
    }

    private sealed class EmptyRemoteFeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }

    internal sealed class RecordingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public int RequestCount { get; private set; }
        public List<HttpRequestMessage> Requests { get; } = [];
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public string ResponseContent { get; set; } = """{ "environments": [], "defaultEnvironmentName": null }""";
        public Exception? ThrowOnSend { get; set; }
        public int RemainingEnvironmentFailures { get; set; }
        public Func<HttpRequestMessage, string>? ResponseContentForRequest { get; set; }

        public int EnvironmentRequestCount =>
            Requests.Count(request => request.RequestUri?.AbsolutePath.Contains("environments", StringComparison.OrdinalIgnoreCase) == true);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LastRequest = request;
            RequestCount++;
            Requests.Add(request);

            if (ThrowOnSend is not null)
                throw ThrowOnSend;

            if (RemainingEnvironmentFailures > 0
                && request.RequestUri?.AbsolutePath.Contains("environments", StringComparison.OrdinalIgnoreCase) == true)
            {
                RemainingEnvironmentFailures--;
                throw new InvalidOperationException("The environments API is unreachable.");
            }

            var content = ResponseContentForRequest?.Invoke(request) ?? ResponseContent;

            return Task.FromResult(new HttpResponseMessage(StatusCode)
            {
                RequestMessage = request,
                Content = new StringContent(content, Encoding.UTF8, new MediaTypeHeaderValue("application/json"))
            });
        }
    }
}
