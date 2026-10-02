using Elsa.Studio.Contracts;
using Elsa.Studio.DomInterop.Contracts;
using Elsa.Studio.Security.Client;
using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Elsa.Studio.Security.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Security.Components;

/// <summary>
/// Creates or edits one user. Rendered only after <see cref="UserAdministrationAccessBoundary"/> established that the
/// caller may view users; create, update and delete stay independently gated by <see cref="Access"/>.
/// </summary>
public partial class UserEditorSurface : IAsyncDisposable
{
    public const string RoleAssignmentForbiddenMessage =
        "Core refused to assign one or more of the selected roles. You can only assign roles whose permissions your own sign-in already holds. Remove the roles you cannot delegate and try again.";

    private readonly List<RoleSummary> _roleOptions = [];
    private readonly HashSet<string> _originalRoles = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _lifetime = new();
    private IReadOnlyCollection<string> _selectedRoles = [];
    private MudForm? _form;
    private bool _loading = true;
    private bool _saving;
    private bool _deleting;
    private bool _canViewRoles;
    private bool _rolesChanged;
    private bool _hasLoadedParameters;
    private bool _disposed;
    private string? _loadError;
    private string? _saveError;
    private string? _deleteError;
    private string? _name;
    private string? _tenantId;
    private string? _password;
    private string? _passwordConfirmation;
    private string? _loadedId;
    private long _loadVersion;

    [Parameter] public string? Id { get; set; }
    [Parameter, EditorRequired] public UserAdministrationAccess Access { get; set; } = UserAdministrationAccess.Unavailable;
    [Inject] private IBackendApiClientProvider ApiClientProvider { get; set; } = default!;
    [Inject] private IRoleAdministrationAccessService RoleAccessService { get; set; } = default!;
    [Inject] private IClipboard Clipboard { get; set; } = default!;
    [Inject] private IDialogService DialogService { get; set; } = default!;
    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    protected bool IsNew => string.IsNullOrWhiteSpace(Id);
    protected bool IsReadOnly => !IsNew && !Access.CanUpdate;
    protected bool CanOpen => IsNew ? Access.CanCreate : Access.CanView;
    protected bool CanSave => !IsReadOnly && !_saving && !_deleting;
    protected bool CanDelete => !IsNew && Access.CanDelete;
    protected string Title => IsNew ? "Create user" : _name ?? "User";
    protected string SaveLabel => IsNew ? "Create user" : "Save changes";
    protected string PasswordHeading => IsNew ? "Password" : "Change password";
    protected string PasswordLabel => IsNew ? "Password (optional)" : "New password (optional)";
    protected string ScopeLabel => string.IsNullOrWhiteSpace(_tenantId) ? "Host" : _tenantId;
    protected string CredentialGuidance => IsNew
        ? "Leave blank to let Elsa generate a password. A generated password is shown once after the account is created and cannot be retrieved later."
        : "Leave both fields blank to keep the current password. Passwords are never shown again after saving.";
    protected IReadOnlyList<BreadcrumbItem> Breadcrumbs =>
        [new("Users", href: "security/users"), new(Title, href: null)];

    protected override async Task OnParametersSetAsync()
    {
        if (_hasLoadedParameters && string.Equals(_loadedId, Id, StringComparison.Ordinal))
            return;

        _hasLoadedParameters = true;
        _loadedId = Id;
        await LoadAsync();
    }

    protected async Task SaveAsync()
    {
        if (!CanSave || _form == null)
            return;

        var operationVersion = _loadVersion;
        var operationId = Id;
        var isNew = string.IsNullOrWhiteSpace(operationId);
        await _form.Validate();
        if (!IsCurrentLoad(operationVersion) || !_form.IsValid)
            return;

        var name = _name!.Trim();
        var password = _password;
        var suppliedPassword = !string.IsNullOrWhiteSpace(password);
        var roles = _selectedRoles.Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        var rolesChanged = _rolesChanged;
        var rolesIncluded = isNew ? roles.Length > 0 : rolesChanged;
        _saving = true;
        _saveError = null;
        StateHasChanged();
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IUsersApi>(_lifetime.Token);
            if (!IsCurrentLoad(operationVersion))
                return;

            if (isNew)
            {
                var created = await api.CreateAsync(new()
                {
                    Name = name,
                    Password = suppliedPassword ? password : null,
                    Roles = roles
                }, _lifetime.Token);
                if (!IsCurrentLoad(operationVersion))
                    return;

                Snackbar.Add("User created.", Severity.Success);
                // A supplied password is never displayed, even if a server were to echo it.
                if (!suppliedPassword && !string.IsNullOrWhiteSpace(created.GeneratedPassword))
                    await ShowGeneratedPasswordAsync(created.GeneratedPassword);
                if (!IsCurrentLoad(operationVersion))
                    return;
                NavigationManager.NavigateTo($"security/users/{Uri.EscapeDataString(created.Id)}");
            }
            else
            {
                await api.UpdateAsync(operationId!, new()
                {
                    Password = suppliedPassword ? password : null,
                    Roles = rolesChanged ? roles : null
                }, _lifetime.Token);
                if (!IsCurrentLoad(operationVersion))
                    return;

                _originalRoles.Clear();
                _originalRoles.UnionWith(roles);
                _rolesChanged = false;
                _password = null;
                _passwordConfirmation = null;
                Snackbar.Add("User updated.", Severity.Success);
            }
        }
        catch (Exception exception) when (!_lifetime.IsCancellationRequested)
        {
            if (IsCurrentLoad(operationVersion))
                _saveError = DescribeSaveFailure(exception, rolesIncluded);
        }
        finally
        {
            if (IsCurrentLoad(operationVersion))
                _saving = false;
        }
    }

    protected async Task DeleteAsync()
    {
        if (!CanDelete || _deleting || _saving)
            return;

        var operationVersion = _loadVersion;
        var operationId = Id!;
        var confirmed = await DialogService.ShowMessageBoxAsync(
            "Delete user?",
            $"Delete {_name}? This cannot be undone.",
            yesText: "Delete",
            cancelText: "Cancel");
        if (confirmed != true || !IsCurrentLoad(operationVersion))
            return;

        _deleting = true;
        _deleteError = null;
        StateHasChanged();
        try
        {
            var api = await ApiClientProvider.GetApiAsync<IUsersApi>(_lifetime.Token);
            if (!IsCurrentLoad(operationVersion))
                return;
            await api.DeleteAsync(operationId, _lifetime.Token);
            if (!IsCurrentLoad(operationVersion))
                return;
            Snackbar.Add("User deleted.", Severity.Success);
            NavigationManager.NavigateTo("security/users");
        }
        catch (Exception exception) when (!_lifetime.IsCancellationRequested)
        {
            if (IsCurrentLoad(operationVersion))
                _deleteError = IdentityApiErrorMapper.Describe(exception, IdentityApiErrorMapper.UserSubject).Message;
        }
        finally
        {
            if (IsCurrentLoad(operationVersion))
                _deleting = false;
        }
    }

    protected async Task CopyUserIdAsync()
    {
        if (IsNew)
            return;

        await Clipboard.CopyText(Id!);
        Snackbar.Add("User ID copied.", Severity.Success);
    }

    protected string? ValidatePasswordConfirmation(string? value)
    {
        if (string.IsNullOrEmpty(_password) && string.IsNullOrEmpty(value))
            return null;
        return string.Equals(_password, value, StringComparison.Ordinal) ? null : "Passwords do not match.";
    }

    protected Task OnSelectedRolesChanged(IEnumerable<string>? roles)
    {
        var selectedRoles = (roles ?? []).ToHashSet(StringComparer.Ordinal);
        _selectedRoles = selectedRoles;
        _rolesChanged = !_originalRoles.SetEquals(selectedRoles);
        return Task.CompletedTask;
    }

    private async Task LoadAsync()
    {
        var loadVersion = ++_loadVersion;
        var requestedId = Id;
        var isNew = string.IsNullOrWhiteSpace(requestedId);
        ResetEditorState();

        if (!CanOpen)
        {
            _loading = false;
            return;
        }

        try
        {
            var roleAccess = await RoleAccessService.GetAsync(_lifetime.Token);
            if (!IsCurrentLoad(loadVersion))
                return;
            _canViewRoles = roleAccess.CanView;

            var usersApi = await ApiClientProvider.GetApiAsync<IUsersApi>(_lifetime.Token);
            if (!IsCurrentLoad(loadVersion))
                return;
            var rolesApi = _canViewRoles ? await ApiClientProvider.GetApiAsync<IRolesApi>(_lifetime.Token) : null;
            if (!IsCurrentLoad(loadVersion))
                return;
            var rolesTask = rolesApi?.ListAsync(_lifetime.Token);

            if (!isNew)
            {
                var users = await usersApi.ListAsync(_lifetime.Token);
                if (!IsCurrentLoad(loadVersion))
                    return;
                var user = users.Users.FirstOrDefault(x => string.Equals(x.Id, requestedId, StringComparison.Ordinal));
                if (user == null)
                {
                    _loadError = "The user was not found in the current tenant scope.";
                    return;
                }

                _name = user.Name;
                _tenantId = user.TenantId;
                _originalRoles.UnionWith(user.Roles);
                _selectedRoles = _originalRoles.ToHashSet(StringComparer.Ordinal);
            }

            if (rolesTask != null)
            {
                var roles = await rolesTask;
                if (!IsCurrentLoad(loadVersion))
                    return;
                _roleOptions.AddRange(roles.Roles.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase));
            }
        }
        catch (Exception exception) when (!_lifetime.IsCancellationRequested)
        {
            // Once the component is disposed nothing may touch state; otherwise every failure maps to a safe message.
            if (IsCurrentLoad(loadVersion))
                _loadError = IdentityApiErrorMapper.Describe(exception, IdentityApiErrorMapper.UserSubject).Message;
        }
        finally
        {
            if (IsCurrentLoad(loadVersion))
                _loading = false;
        }
    }

    private static string DescribeSaveFailure(Exception exception, bool rolesIncluded)
    {
        var error = IdentityApiErrorMapper.Describe(exception, IdentityApiErrorMapper.UserSubject);
        return error.IsAuthorization && rolesIncluded ? RoleAssignmentForbiddenMessage : error.Message;
    }

    private void ResetEditorState()
    {
        _loading = true;
        _saving = false;
        _deleting = false;
        _canViewRoles = false;
        _loadError = null;
        _saveError = null;
        _deleteError = null;
        _name = null;
        _tenantId = null;
        _password = null;
        _passwordConfirmation = null;
        _selectedRoles = [];
        _roleOptions.Clear();
        _originalRoles.Clear();
        _rolesChanged = false;
    }

    private async Task ShowGeneratedPasswordAsync(string password)
    {
        var parameters = new DialogParameters<GeneratedPasswordDialog> { { x => x.Password, password } };
        var options = new DialogOptions
        {
            BackdropClick = false,
            CloseButton = false,
            CloseOnEscapeKey = false,
            FullWidth = true,
            MaxWidth = MaxWidth.Small
        };
        var dialog = await DialogService.ShowAsync<GeneratedPasswordDialog>("Save the generated password", parameters, options);
        await dialog.Result;
    }

    private bool IsCurrentLoad(long version) => !_disposed && version == _loadVersion;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        _loadVersion++;
        await _lifetime.CancelAsync();
        _lifetime.Dispose();
    }
}
