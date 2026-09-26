using System.Net;
using Bunit;
using Elsa.Api.Client.Resources.Features.Models;
using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.ExternalAuthentication.Client;
using Elsa.Studio.ExternalAuthentication.Components.IdentityLinks;
using Elsa.Studio.ExternalAuthentication.Menu;
using Elsa.Studio.ExternalAuthentication.Models;
using Elsa.Studio.ExternalAuthentication.Services;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;
using Xunit;
using IdentityLinksPage = Elsa.Studio.ExternalAuthentication.Pages.IdentityLinks.Index;

namespace Elsa.Studio.ExternalAuthentication.Tests.Links;

public sealed class ExternalIdentityLinksTests : BunitContext, IAsyncLifetime
{
    private readonly LinksApi _links = new();
    private readonly ConnectionsApi _connections = new();
    private readonly Clipboard _clipboard = new();
    private readonly IRenderedComponent<MudDialogProvider> _dialogProvider;
    private readonly IRenderedComponent<MudPopoverProvider> _popoverProvider;

    public ExternalIdentityLinksTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddMudServices();
        Services.AddSingleton(TimeProvider.System);
        Services.AddSingleton<IBackendApiClientProvider>(new ApiProvider(_links, _connections));
        Services.AddSingleton<IClipboard>(_clipboard);
        Services.AddSingleton<IExternalAuthenticationPermissionService>(
            new PermissionService(ExternalAuthenticationPermissions.ManageLinks));
        _popoverProvider = Render<MudPopoverProvider>();
        _dialogProvider = Render<MudDialogProvider>();
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;
    async Task IAsyncLifetime.DisposeAsync() => await base.DisposeAsync();

    [Fact]
    public async Task LinkPageIsListOnlyUntilCreateLinkIsOpened()
    {
        _links.ListResults.Enqueue(new(
        [
            new ExternalIdentityLink(
                "link-1",
                "user-1",
                "contoso",
                "https://login.contoso.example",
                "00u…cdef",
                DateTimeOffset.UtcNow,
                null)
        ], null));
        _links.Users = [new("user-1", "workflow-admin")];
        _connections.Result = new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "connection-1", Key = "contoso", DisplayName = "Contoso" }]
        };

        var cut = Render<IdentityLinksPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("workflow-admin", cut.Markup);
            Assert.Contains("User ID", cut.Markup);
            Assert.Contains("Contoso", cut.Markup);
            Assert.Contains("https://login.contoso.example", cut.Markup);
            Assert.Contains("00u…cdef", cut.Markup);
            Assert.Contains("Never signed in", cut.Markup);
            Assert.DoesNotContain("type=\"password\"", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains(cut.FindAll("button"), button => button.TextContent.Trim() == "Create link");
            Assert.DoesNotContain("Create a prelink", cut.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("never returned", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var actions = Assert.Single(cut.FindComponents<MudMenu>());
            Assert.Equal("Actions for workflow-admin via Contoso", actions.Instance.AriaLabel);
            Assert.Contains("aria-label=\"Edit identity link for workflow-admin via Contoso\"", cut.Markup, StringComparison.Ordinal);
            var row = cut.Find("tbody tr");
            Assert.Equal("workflow-admin", row.QuerySelector("td[data-label='User']")?.TextContent.Trim());
            Assert.Equal("user-1", row.QuerySelector("td[data-label='User ID'] .identity-id-value")?.TextContent.Trim());
            Assert.DoesNotContain("user-1", row.QuerySelector("td[data-label='User']")?.TextContent);
        });

        await cut.InvokeAsync(() => cut.Find("button[aria-label='Copy user ID user-1']").Click());
        Assert.Equal("user-1", _clipboard.LastCopiedText);

        cut.Find("button[aria-label='Actions for workflow-admin via Contoso']").Click();
        _popoverProvider.WaitForAssertion(() =>
        {
            Assert.Contains("Edit", _popoverProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("Unlink", _popoverProvider.Markup, StringComparison.Ordinal);
        });
        Assert.DoesNotContain("Edit external identity link", _dialogProvider.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public void ClickingALinkRowOpensTheEditDialog()
    {
        var cut = RenderPageWithOneLink();

        cut.Find("tbody tr").Click();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Contains("Edit external identity link", _dialogProvider.Markup, StringComparison.Ordinal);
            Assert.Contains("https://login.contoso.example", _dialogProvider.Markup, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void LinkListUsesServerCursorAndTenantScopedFilters()
    {
        _links.Users = [];
        _connections.Result = new();
        _links.ListResults.Enqueue(new([], "cursor-2"));
        _links.ListResults.Enqueue(new([], null));

        var cut = Render<IdentityLinksPage>();
        cut.WaitForAssertion(() => Assert.Contains("Next page", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Contains("Next page", StringComparison.Ordinal)).Click();

        cut.WaitForAssertion(() => Assert.Equal([null, "cursor-2"], _links.Cursors));
    }

    [Fact]
    public void LinkPageLoadsEveryEffectiveConnectionPage()
    {
        _links.Users = [];
        _links.ListResults.Enqueue(new(
        [
            new ExternalIdentityLink("link-1", "user-1", "contoso", "https://login.contoso.example", null, DateTimeOffset.UtcNow, null),
            new ExternalIdentityLink("link-2", "user-2", "github", "https://github.com", null, DateTimeOffset.UtcNow, null)
        ], null));
        _connections.Results.Enqueue(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Key = "contoso", DisplayName = "Contoso" }],
            NextCursor = "connections-2"
        });
        _connections.Results.Enqueue(new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Key = "github", DisplayName = "GitHub" }]
        });

        var cut = Render<IdentityLinksPage>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Contoso", cut.Markup);
            Assert.Contains("GitHub", cut.Markup);
            Assert.Equal([null, "connections-2"], _connections.Cursors);
        });
    }

    [Fact]
    public void CreateAndEditUseTheSharedDialogAndEditReplacesTheLink()
    {
        var link = new ExternalIdentityLink(
            "link-1",
            "user-1",
            "contoso",
            "https://login.contoso.example",
            "00u…cdef",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
        _links.ListResults.Enqueue(new([link], null));
        _links.ListResults.Enqueue(new([], null));
        _links.Users = [new("user-1", "workflow-admin")];
        _connections.Result = new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "connection-1", Key = "contoso", DisplayName = "Contoso", Validity = "valid" }]
        };

        var cut = Render<IdentityLinksPage>();
        cut.WaitForAssertion(() => Assert.Contains("workflow-admin", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Create link").Click();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Contains("Create external identity link", _dialogProvider.Markup);
            Assert.Contains("External subject", _dialogProvider.Markup);
            Assert.Contains("type=\"password\"", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
            var dialog = _dialogProvider.FindComponent<MudDialogContainer>();
            var options = ((IMudDialogInstance)dialog.Instance).Options;
            Assert.True(options.CloseButton is true);
            Assert.True(options.CloseOnEscapeKey is true);
            Assert.NotEqual(false, options.BackdropClick);
        });
        _dialogProvider.Find("button[aria-label='Show external subject']").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("type=\"text\"", _dialogProvider.Find("input[autocomplete=off]").OuterHtml, StringComparison.OrdinalIgnoreCase));
        _dialogProvider.Find("button[aria-label='Hide external subject']").Click();
        _dialogProvider.Find("input[type=password]").Change("must-not-survive-close");
        _dialogProvider.FindAll("button").Single(button => button.TextContent.Trim() == "Cancel").Click();

        OpenEditDialog(cut);
        Assert.Contains("resets its sign-in history", _dialogProvider.Markup);
        Assert.Contains("https://login.contoso.example", _dialogProvider.Markup);
        Assert.Equal(string.Empty, _dialogProvider.Find("input[type=password]").GetAttribute("value") ?? string.Empty);
        _dialogProvider.Find("input[type=password]").Change("replacement-subject");
        _dialogProvider.Find("form").Submit();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(_links.ReplaceRequest);
            Assert.Equal("link-1", _links.ReplacedLinkId);
            Assert.Equal("user-1", _links.ReplaceRequest.UserId);
            Assert.Equal("contoso", _links.ReplaceRequest.ConnectionKey);
            Assert.Equal("https://login.contoso.example", _links.ReplaceRequest.Issuer);
            Assert.Equal("replacement-subject", _links.ReplaceRequest.Subject);
        });
    }

    [Fact]
    public async Task CreateSubmitsThroughTheSharedFormAndDisablesSaveWhilePending()
    {
        _links.ListResults.Enqueue(new([], null));
        _links.ListResults.Enqueue(new([], null));
        _links.Users = [new("user-1", "workflow-admin")];
        _connections.Result = new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Key = "contoso", DisplayName = "Contoso", Validity = "valid" }]
        };
        _links.PrelinkCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);

        var cut = Render<IdentityLinksPage>();
        cut.WaitForAssertion(() => Assert.Contains("Create link", cut.Markup));
        cut.FindAll("button").Single(button => button.TextContent.Trim() == "Create link").Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Create external identity link", _dialogProvider.Markup));

        var selects = _dialogProvider.FindComponents<MudSelect<string>>();
        await _dialogProvider.InvokeAsync(() => selects[0].Instance.ValueChanged.InvokeAsync("user-1"));
        await _dialogProvider.InvokeAsync(() => selects[1].Instance.ValueChanged.InvokeAsync("contoso"));
        _dialogProvider.Find("input[type=url]").Change("https://login.contoso.example");
        _dialogProvider.Find("input[type=password]").Change("subject-1");
        _dialogProvider.Find("form").Submit();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.NotNull(_links.PrelinkRequest);
            var save = _dialogProvider.FindAll("button").Single(button => button.TextContent.Trim() == "Create link");
            Assert.True(save.HasAttribute("disabled"));
            var dialog = _dialogProvider.FindComponent<MudDialogContainer>();
            var options = ((IMudDialogInstance)dialog.Instance).Options;
            Assert.False(options.CloseButton);
            Assert.False(options.CloseOnEscapeKey);
            Assert.False(options.BackdropClick);
        });

        _links.PrelinkCompletion.SetResult(new ExternalIdentityLink(
            "link-1",
            "user-1",
            "contoso",
            "https://login.contoso.example",
            "subject…hint",
            DateTimeOffset.UtcNow,
            null));

        _dialogProvider.WaitForAssertion(() => Assert.DoesNotContain("Create external identity link", _dialogProvider.Markup));
        Assert.Equal("user-1", _links.PrelinkRequest!.UserId);
        Assert.Equal("contoso", _links.PrelinkRequest.ConnectionKey);
        Assert.Equal("subject-1", _links.PrelinkRequest.Subject);
    }

    [Fact]
    public async Task ReplacementConflictStaysInTheDialog()
    {
        var cut = RenderPageWithOneLink();
        _links.ReplaceException = await CreateApiExceptionAsync(HttpStatusCode.Conflict, """{"error":"conflict"}""");

        OpenEditDialog(cut);
        _dialogProvider.Find("input[type=password]").Change("conflicting-subject");
        _dialogProvider.Find("form").Submit();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Contains("already exists", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("Edit external identity link", _dialogProvider.Markup);
        });
    }

    [Theory]
    [InlineData("""{"error":"not_found","message":"The requested resource was not found."}""", true)]
    [InlineData("", false)]
    public async Task ReplacementDistinguishesAStaleLinkFromAnUnsupportedBackend(string responseBody, bool closes)
    {
        var cut = RenderPageWithOneLink(enqueueReload: closes);
        _links.ReplaceException = await CreateApiExceptionAsync(HttpStatusCode.NotFound, responseBody);

        OpenEditDialog(cut);
        _dialogProvider.Find("input[type=password]").Change("replacement-subject");
        _dialogProvider.Find("form").Submit();

        if (closes)
        {
            _dialogProvider.WaitForAssertion(() => Assert.DoesNotContain("Edit external identity link", _dialogProvider.Markup));
            cut.WaitForAssertion(() => Assert.Equal(3, _links.Cursors.Count));
        }
        else
        {
            _dialogProvider.WaitForAssertion(() =>
            {
                Assert.Contains("does not support editing", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
                Assert.Contains("Edit external identity link", _dialogProvider.Markup);
            });
        }
    }

    [Fact]
    public async Task StructuredNotFoundStaysOpenWhenTheOriginalLinkStillExists()
    {
        var cut = RenderPageWithOneLink();
        var original = _links.ListedLinks.Single();
        _links.ListResults.Enqueue(new([original], null));
        _links.ReplaceException = await CreateApiExceptionAsync(
            HttpStatusCode.NotFound,
            """{"error":"not_found","message":"The requested resource was not found."}""");

        OpenEditDialog(cut);
        _dialogProvider.Find("input[type=password]").Change("replacement-subject");
        _dialogProvider.Find("form").Submit();

        _dialogProvider.WaitForAssertion(() =>
        {
            Assert.Contains("no longer available", _dialogProvider.Markup, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("replacement-subject", _dialogProvider.Find("input[type=password]").GetAttribute("value"));
        });
    }

    [Fact]
    public async Task SecurityMenuShowsLinksOnlyWithTheDedicatedPermission()
    {
        var menu = new ExternalAuthenticationSecurityMenuContributor(
            new FeatureProvider(),
            new PermissionService(ExternalAuthenticationPermissions.ManageLinks));

        var item = Assert.Single(await menu.GetMenuItemsAsync());

        Assert.Equal("security/external-authentication/identity-links", item.Href);
        Assert.DoesNotContain("connections", item.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SecurityMenuUsesTheConnectionRouteAvailableAtThisBoundary()
    {
        var menu = new ExternalAuthenticationSecurityMenuContributor(
            new FeatureProvider(),
            new PermissionService(ExternalAuthenticationPermissions.Read));

        var item = Assert.Single(await menu.GetMenuItemsAsync());

        Assert.Equal("security/external-authentication/connections", item.Href);
        Assert.Equal("Identity provider connections", item.Text);
    }

    [Fact]
    public void PrelinkValidationRequiresAnHttpsIssuerAndNeverNeedsRoleOrPermissionData()
    {
        var request = new PrelinkExternalIdentityRequest
        {
            UserId = "user-1",
            ConnectionKey = "contoso",
            Issuer = "http://insecure.example",
            Subject = "subject-1"
        };

        var errors = IdentityLinkUiState.Validate(request);

        Assert.Single(errors);
        Assert.Contains("HTTPS", errors.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(errors, error => error.Contains("role", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(errors, error => error.Contains("permission", StringComparison.OrdinalIgnoreCase));
    }

    private IRenderedComponent<IdentityLinksPage> RenderPageWithOneLink(bool enqueueReload = false)
    {
        _links.ListResults.Enqueue(new(
        [
            new ExternalIdentityLink(
                "link-1",
                "user-1",
                "contoso",
                "https://login.contoso.example",
                "00u…cdef",
                DateTimeOffset.UtcNow,
                null)
        ], null));
        if (enqueueReload)
        {
            _links.ListResults.Enqueue(new([], null));
            _links.ListResults.Enqueue(new([], null));
        }
        _links.Users = [new("user-1", "workflow-admin")];
        _connections.Result = new ListConnectionsResponse
        {
            Items = [new ConnectionSummary { Id = "connection-1", Key = "contoso", DisplayName = "Contoso", Validity = "valid" }]
        };
        var cut = Render<IdentityLinksPage>();
        cut.WaitForAssertion(() => Assert.Contains("workflow-admin", cut.Markup));
        return cut;
    }

    private void OpenEditDialog(IRenderedComponent<IdentityLinksPage> cut)
    {
        cut.Find("button[aria-label^='Actions for ']").Click();
        var edit = _popoverProvider.WaitForElements(".mud-menu-item")
            .Single(item => item.TextContent.Contains("Edit", StringComparison.Ordinal));
        edit.Click();
        _dialogProvider.WaitForAssertion(() => Assert.Contains("Edit external identity link", _dialogProvider.Markup));
    }

    private static async Task<Refit.ApiException> CreateApiExceptionAsync(HttpStatusCode statusCode, string content)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://elsa.example.test/external-authentication/identity-links/link-1/replace");
        using var response = new HttpResponseMessage(statusCode)
        {
            RequestMessage = request,
            Content = new StringContent(content)
        };
        return await Refit.ApiException.Create(request, HttpMethod.Post, response, new Refit.RefitSettings());
    }

    private sealed class ApiProvider(LinksApi links, ConnectionsApi connections) : IBackendApiClientProvider
    {
        public Uri Url { get; } = new("https://elsa.example.test/elsa/api/");

        public ValueTask<T> GetApiAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            object api = typeof(T) == typeof(IExternalIdentityLinksApi) ? links :
                typeof(T) == typeof(IExternalAuthenticationConnectionsApi) ? connections :
                throw new NotSupportedException(typeof(T).FullName);
            return ValueTask.FromResult((T)api);
        }
    }

    private sealed class LinksApi : IExternalIdentityLinksApi
    {
        public Queue<ListExternalIdentityLinksResponse> ListResults { get; } = new();
        public IReadOnlyCollection<IdentityLinkUser> Users { get; set; } = [];
        public List<string?> Cursors { get; } = [];
        public IReadOnlyCollection<ExternalIdentityLink> ListedLinks { get; private set; } = [];

        public ReplaceExternalIdentityLinkRequest? ReplaceRequest { get; private set; }
        public PrelinkExternalIdentityRequest? PrelinkRequest { get; private set; }
        public TaskCompletionSource<ExternalIdentityLink>? PrelinkCompletion { get; set; }
        public string? ReplacedLinkId { get; private set; }
        public Exception? ReplaceException { get; set; }

        public Task<ListExternalIdentityLinksResponse> ListAsync(string? userId = null, string? connectionKey = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            Cursors.Add(cursor);
            var response = ListResults.Dequeue();
            ListedLinks = response.Items;
            return Task.FromResult(response);
        }

        public Task<FindIdentityLinkUsersResponse> FindUsersAsync(string? search = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default) =>
            Task.FromResult(new FindIdentityLinkUsersResponse(Users, null));

        public Task<ExternalIdentityLink> PrelinkAsync(PrelinkExternalIdentityRequest request, CancellationToken cancellationToken = default)
        {
            PrelinkRequest = new PrelinkExternalIdentityRequest
            {
                UserId = request.UserId,
                ConnectionKey = request.ConnectionKey,
                Issuer = request.Issuer,
                Subject = request.Subject
            };
            return PrelinkCompletion?.Task ?? Task.FromResult(new ExternalIdentityLink(
                "link-1",
                request.UserId,
                request.ConnectionKey,
                request.Issuer,
                "subject…hint",
                DateTimeOffset.UtcNow,
                null));
        }

        public Task<ExternalIdentityLink> ReplaceAsync(string linkId, ReplaceExternalIdentityLinkRequest request, CancellationToken cancellationToken = default)
        {
            if (ReplaceException is not null)
                return Task.FromException<ExternalIdentityLink>(ReplaceException);

            ReplacedLinkId = linkId;
            ReplaceRequest = new ReplaceExternalIdentityLinkRequest
            {
                UserId = request.UserId,
                ConnectionKey = request.ConnectionKey,
                Issuer = request.Issuer,
                Subject = request.Subject
            };
            return Task.FromResult(new ExternalIdentityLink(
                "link-2",
                request.UserId,
                request.ConnectionKey,
                request.Issuer,
                "replacement…hint",
                DateTimeOffset.UtcNow,
                null));
        }

        public Task UnlinkAsync(string linkId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class ConnectionsApi : IExternalAuthenticationConnectionsApi
    {
        public ListConnectionsResponse Result { get; set; } = new();
        public Queue<ListConnectionsResponse> Results { get; } = new();
        public List<string?> Cursors { get; } = [];

        public Task<ListConnectionsResponse> ListAsync(string? search = null, string? source = null, string? scope = null, string? adapterType = null, bool? enabled = null, bool? valid = null, bool? shadowed = null, bool? archived = null, string? cursor = null, int pageSize = 25, CancellationToken cancellationToken = default)
        {
            Cursors.Add(cursor);
            return Task.FromResult(Results.TryDequeue(out var response) ? response : Result);
        }

        public Task<ConnectionDetail> GetAsync(string connectionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ExternalAuthenticationRuntimeDescriptor> GetRuntimeAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<AdapterDescriptor>> GetAdaptersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<PermissionGrantSourceDescriptor>> GetPermissionSourcesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<UnlinkedIdentityPolicyDescriptor>> GetPoliciesAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<ExternalUserMatcherDescriptor>> GetUserMatchersAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ManagedSecretResolverCatalog> GetManagedSecretResolversAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ICollection<PermissionDescriptor>> GetPermissionsAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> CreateAsync(ConnectionMutation request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> UpdateAsync(string connectionId, ConnectionMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task EnableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task DisableAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task ArchiveAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RestoreAsync(string connectionId, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionValidationResult> ValidateAsync(string connectionId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ConnectionDetail> ReplaceManagedSecretAsync(string connectionId, string fieldName, ManagedSecretMutation request, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task RemoveSecretBindingAsync(string connectionId, string fieldName, string ifMatch, CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class PermissionService(params string[] permissions) : IExternalAuthenticationPermissionService
    {
        private readonly IReadOnlySet<string> _permissions = permissions.ToHashSet(StringComparer.Ordinal);
        public ValueTask<bool> HasAsync(string permission, CancellationToken cancellationToken = default) => ValueTask.FromResult(_permissions.Contains(permission));
        public ValueTask<IReadOnlySet<string>> ListAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(_permissions);
    }

    private sealed class Clipboard : IClipboard
    {
        public string? LastCopiedText { get; private set; }

        public Task CopyText(string text, CancellationToken cancellationToken = default)
        {
            LastCopiedText = text;
            return Task.CompletedTask;
        }
    }

    private sealed class FeatureProvider : IRemoteFeatureProvider
    {
        public Task<bool> IsEnabledAsync(string featureName, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IEnumerable<FeatureDescriptor>> ListAsync(CancellationToken cancellationToken = default) => Task.FromResult<IEnumerable<FeatureDescriptor>>([]);
    }
}
