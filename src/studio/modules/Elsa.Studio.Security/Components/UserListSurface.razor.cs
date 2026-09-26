using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Security.Components;

/// <summary>
/// Lists the users visible in the current tenant scope. Rendered only after
/// <see cref="UserAdministrationAccessBoundary"/> established that the caller may view users.
/// </summary>
public partial class UserListSurface : IAsyncDisposable
{
    protected const int PreviewLimit = 3;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<string> _deletingIds = new(StringComparer.Ordinal);
    private readonly List<UserSummary> _users = [];
    private IdentityApiErrorInfo? _loadError;
    private IdentityApiErrorInfo? _actionError;
    private bool _loading = true;
    private bool _disposed;
    private string _search = string.Empty;

    [Parameter, EditorRequired] public UserAdministrationAccess Access { get; set; } = UserAdministrationAccess.Unavailable;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private IClipboard Clipboard { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected IReadOnlyList<UserSummary> FilteredUsers => string.IsNullOrWhiteSpace(_search)
        ? _users
        : _users.Where(MatchesSearch).ToList();

    protected string ResultSummary
    {
        get
        {
            var count = FilteredUsers.Count;
            var label = count == 1 ? "user" : "users";
            return string.IsNullOrWhiteSpace(_search)
                ? $"{count} {label} · all loaded"
                : $"{count} {label} · matching search";
        }
    }

    /// <summary>Describes the tenant scope of the loaded list without pretending to know more than Core returned.</summary>
    protected string ScopeSummary
    {
        get
        {
            var scopes = _users.Select(x => Scope(x.TenantId)).Distinct(StringComparer.Ordinal).ToList();
            return scopes.Count switch
            {
                0 => "Current tenant scope",
                1 => scopes[0] == "Host" ? "Host scope" : $"Tenant {scopes[0]}",
                _ => "Mixed tenant scopes"
            };
        }
    }

    protected override Task OnInitializedAsync() => LoadAsync(_lifetime.Token);

    protected async Task ReloadAsync()
    {
        if (!_disposed && !_loading)
            await LoadAsync(_lifetime.Token);
    }

    protected Task OnSearchChanged(string? value)
    {
        _search = value ?? string.Empty;
        return InvokeAsync(StateHasChanged);
    }

    protected void OpenUser(TableRowClickEventArgs<UserSummary> args)
    {
        if (args.Item != null)
            NavigationManager.NavigateTo(UserUrl(args.Item.Id));
    }

    protected async Task DeleteAsync(UserSummary user)
    {
        if (_disposed || !Access.CanDelete || _deletingIds.Contains(user.Id))
            return;

        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete user?",
            $"Delete {user.Name}? This cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");
        if (confirmed != true || !_deletingIds.Add(user.Id))
            return;

        _actionError = null;
        StateHasChanged();
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IUsersApi>(_lifetime.Token);
            await api.DeleteAsync(user.Id, _lifetime.Token);
            _users.Remove(user);
            Snackbar.Add("User deleted.", Severity.Success);
        }
        catch (Exception exception) when (!_lifetime.IsCancellationRequested)
        {
            // Failures after disposal are dropped by the filter; everything else is mapped to a safe message.
            _actionError = IdentityApiErrorMapper.Describe(exception, IdentityApiErrorMapper.UserSubject);
        }
        finally
        {
            _deletingIds.Remove(user.Id);
        }
    }

    protected async Task CopyUserIdAsync(string userId)
    {
        await Clipboard.CopyText(userId);
        Snackbar.Add("User ID copied.", Severity.Success);
    }

    protected static IEnumerable<string> Preview(ICollection<string> values) =>
        values.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).Take(PreviewLimit);

    protected static string Scope(string? tenantId) => string.IsNullOrWhiteSpace(tenantId) ? "Host" : tenantId;
    protected static string UserUrl(string id) => $"security/users/{Uri.EscapeDataString(id)}";
    protected bool IsDeleting(string id) => _deletingIds.Contains(id);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        _loading = true;
        _loadError = null;
        await InvokeAsync(StateHasChanged);
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IUsersApi>(cancellationToken);
            var response = await api.ListAsync(cancellationToken);
            _users.Clear();
            _users.AddRange(response.Users.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            _loadError = IdentityApiErrorMapper.Describe(exception, IdentityApiErrorMapper.UserSubject);
        }
        finally
        {
            if (!_disposed)
            {
                _loading = false;
                await InvokeAsync(StateHasChanged);
            }
        }
    }

    private bool MatchesSearch(UserSummary user)
    {
        var search = _search.Trim();
        return user.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
               || user.Id.Contains(search, StringComparison.OrdinalIgnoreCase)
               || user.Roles.Any(x => x.Contains(search, StringComparison.OrdinalIgnoreCase))
               || (user.TenantId?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await _lifetime.CancelAsync();
        _lifetime.Dispose();
    }
}
