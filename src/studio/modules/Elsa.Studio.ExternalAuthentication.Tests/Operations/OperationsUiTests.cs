using Bunit;
using Elsa.Studio.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Components.Operations;
using Elsa.Studio.ExternalAuthentication.Components.Sessions;
using Elsa.Studio.ExternalAuthentication.Models;
using SessionsIndex = Elsa.Studio.ExternalAuthentication.Pages.Sessions.Index;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;

namespace Elsa.Studio.ExternalAuthentication.Tests.Operations;

public sealed class OperationsUiTests : BunitContext, IAsyncLifetime
{
    private readonly OperationsApi _operations = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;
    private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;

    public OperationsUiTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton<IBackendApiClientProvider>(new BackendApiClientProvider(_operations));
        Services.AddSingleton<IExternalAuthenticationPermissionService>(new PermissionService());
        _popoverProvider = Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public void StaleObservation_ExplainsThatAMaterialChangeInvalidatedTheTest()
    {
        var connection = CreateConnection();
        connection.LatestObservation = new ConnectionObservation
        {
            Status = "succeeded",
            Summary = "Provider discovery succeeded.",
            IsStale = true
        };

        var cut = Render<ConnectionOperations>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter()));

        Assert.Contains("stale", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("materially changed", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TestConnection_RecordsOnlyARedactedObservation()
    {
        var connection = CreateConnection();
        var cut = Render<ConnectionOperations>(parameters => parameters
            .Add(component => component.Connection, connection)
            .Add(component => component.Adapter, CreateAdapter()));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Test connection", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Equal(1, _operations.TestCalls));

        Assert.NotNull(connection.LatestObservation);
        Assert.Equal("Provider discovery succeeded.", connection.LatestObservation!.Summary);
        Assert.DoesNotContain("provider-access-token", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FailedTest_ExplainsThatNoObservationWasRecorded()
    {
        _operations.TestException = new InvalidOperationException("backend unavailable");
        var cut = Render<ConnectionOperations>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter()));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Test connection", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("failed before a diagnostic observation was recorded", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Review the redacted server observation", cut.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    [Fact]
    public void PreviewFlow_RequiresExplicitOneTimeResultRetrieval()
    {
        var cut = Render<ConnectionOperations>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter()));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Preview sign-in", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("Open preview sign-in", cut.Markup));
        Assert.Contains("separate tab", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("https://elsa.example.test/external-authentication/previews/preview-handle/authorize", cut.Markup, StringComparison.Ordinal);

        cut.FindAll("button").Single(button => button.TextContent.Contains("Get one-time preview result", StringComparison.Ordinal)).Click();
        cut.WaitForAssertion(() => Assert.Contains("https://issuer.example.test", cut.Markup));

        Assert.Equal("preview-handle", _operations.LastPreviewHandle);
        Assert.Contains("did not create or link a user", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("permission projection", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("projected claims", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FailedPreview_LeavesAnActionableMessageOnTheDiagnosticsTab()
    {
        _operations.PreviewException = new InvalidOperationException("backend unavailable");
        var cut = Render<ConnectionOperations>(parameters => parameters
            .Add(component => component.Connection, CreateConnection())
            .Add(component => component.Adapter, CreateAdapter()));

        cut.FindAll("button").Single(button => button.TextContent.Contains("Preview sign-in", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Preview could not be prepared", cut.Markup, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void FinalLoginPathWarning_ExplainsGuardAndPrivilegedRecoveryOverride()
    {
        var cut = Render<FinalLoginPathWarning>(parameters => parameters.Add(component => component.CanOverride, true));

        Assert.Contains("without a normal sign-in method", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("break-glass", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("confirm a recovery override", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FinalLoginPathWarning_ExplainsWhenRecoveryOverrideIsUnavailable()
    {
        var cut = Render<FinalLoginPathWarning>();

        Assert.Contains("cannot override this protection", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PreviewNavigation_RequiresAnElsaBackendOriginAndOneTimeAuthorizePath()
    {
        var backend = new Uri("https://elsa.example.test/elsa/api/");

        var accepted = ConnectionOperations.TryGetTrustedPreviewNavigation(
            backend,
            "/external-authentication/previews/preview-handle/authorize",
            out var navigationUrl,
            out var handle);
        var rejected = ConnectionOperations.TryGetTrustedPreviewNavigation(
            backend,
            "https://attacker.example/external-authentication/previews/preview-handle/authorize",
            out _,
            out _);

        Assert.True(accepted);
        Assert.Equal("https://elsa.example.test/external-authentication/previews/preview-handle/authorize", navigationUrl);
        Assert.Equal("preview-handle", handle);
        Assert.False(rejected);
    }

    [Fact]
    public void SessionsPage_ListsOnlySafeSessionMetadata()
    {
        var cut = Render<SessionsIndex>();

        cut.WaitForAssertion(() => Assert.Contains("session-1", cut.Markup));

        Assert.Contains("connection-1", cut.Markup);
        Assert.DoesNotContain("provider-access-token", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("external-subject", cut.Markup, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("aria-label=\"View session session-1\"", cut.Markup, StringComparison.Ordinal);

        var status = cut.FindComponents<MudChip<string>>()
            .Single(chip => chip.Markup.Contains("Active", StringComparison.Ordinal));
        Assert.Equal(Color.Success, status.Instance.Color);
        Assert.Equal(Icons.Material.Outlined.CheckCircleOutline, status.Instance.Icon);

        var actions = Assert.Single(cut.FindComponents<MudMenu>());
        Assert.Equal("Actions for session session-1", actions.Instance.AriaLabel);
        cut.Find("button[aria-label='Actions for session session-1']").Click();
        _popoverProvider.WaitForAssertion(() => Assert.Contains("Revoke", _popoverProvider.Markup, StringComparison.Ordinal));
        Assert.Empty(_dialogProvider.FindComponents<ExternalAuthenticationSessionDetailsDialog>());
    }

    [Fact]
    public void SessionsPage_RowClickOpensSafeSessionDetails()
    {
        var cut = Render<SessionsIndex>();
        cut.WaitForAssertion(() => Assert.Contains("session-1", cut.Markup));

        cut.Find("tbody tr").Click();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Single(_dialogProvider.FindComponents<ExternalAuthenticationSessionDetailsDialog>());
            Assert.Contains("Authentication session details", _dialogProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("session-1", _dialogProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("user-1", _dialogProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("tenant-1", _dialogProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("connection-1", _dialogProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("safe session metadata only", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("provider-access-token", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("external-subject", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
        });
    }

    private static ConnectionDetail CreateConnection() => new()
    {
        Id = "connection-1",
        Revision = 7,
        DisplayName = "Contoso",
        Key = "contoso",
        AdapterType = "openid-connect"
    };

    private static AdapterDescriptor CreateAdapter() => new()
    {
        Type = "openid-connect",
        Capabilities = new AdapterCapabilities { SupportsTest = true, SupportsPreview = true }
    };

    private sealed class PermissionService : IExternalAuthenticationPermissionService
    {
        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) => ValueTask.FromResult(true);
        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult<IReadOnlySet<string>>(new HashSet<string>(["*"], StringComparer.Ordinal));
    }

    private sealed class BackendApiClientProvider(OperationsApi api) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/elsa/api/");
        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class => ValueTask.FromResult((T)(object)api);
    }

    private sealed class OperationsApi : IExternalAuthenticationOperationsApi
    {
        public int TestCalls { get; private set; }
        public string? LastPreviewHandle { get; private set; }
        public Exception? TestException { get; set; }
        public Exception? PreviewException { get; set; }

        public Task<ConnectionTestResult> TestAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default)
        {
            TestCalls++;
            if (TestException is not null)
                throw TestException;
            return Task.FromResult(new ConnectionTestResult { Status = "succeeded", Summary = "Provider discovery succeeded.", TestedMaterialRevision = "revision-1" });
        }

        public Task<PreviewInitiation> InitiatePreviewAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default)
        {
            if (PreviewException is not null)
                throw PreviewException;
            return Task.FromResult(new PreviewInitiation { NavigationUrl = "/external-authentication/previews/preview-handle/authorize", ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5) });
        }

        public Task<PreviewResultDocument> GetPreviewResultAsync(string previewHandle, CancellationToken cancellationToken = default)
        {
            LastPreviewHandle = previewHandle;
            return Task.FromResult(new PreviewResultDocument { Issuer = "https://issuer.example.test", MaskedSubject = "sub•••123", PolicyDecision = "would-link" });
        }

        public Task<ListExternalAuthenticationSessionsResponse> ListSessionsAsync(string? userId = null, string? connectionId = null, string? status = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default) =>
            Task.FromResult(new ListExternalAuthenticationSessionsResponse
            {
                Items = [new ExternalAuthenticationSessionSummary
                {
                    Id = "session-1",
                    UserId = "user-1",
                    TenantId = "tenant-1",
                    ConnectionId = "connection-1",
                    StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                    LastRefreshedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(55),
                    Status = "active"
                }]
            });
        public Task RevokeSessionAsync(string sessionId, RevokeExternalAuthenticationSessionRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DisableWithRecoveryOverrideAsync(string connectionId, string ifMatch, bool confirmFinalLoginPathOverride, bool revokeActiveSessions = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
