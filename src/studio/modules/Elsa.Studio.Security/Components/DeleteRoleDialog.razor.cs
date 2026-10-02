using Elsa.Studio.Security.Contracts;
using Elsa.Studio.Security.Models;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace Elsa.Studio.Security.Components;

/// <summary>
/// Shared, version-aware role deletion dialog used by both the role list and editor.
/// </summary>
public partial class DeleteRoleDialog : ComponentBase, IAsyncDisposable
{
    private CancellationTokenSource? _lifetime;
    private DeleteRoleDialogViewState _state = DeleteRoleDialogViewState.Loading;
    private RoleDeletionImpactResponse? _impact;
    private string? _errorMessage;
    private string? _operationCode;
    private string? _previousDependencyVersion;
    private IReadOnlyCollection<string> _changedOwnerIds = [];
    private bool _confirmEditableReferences;
    private bool _confirmEmptyDefaultRoles;
    private bool _confirmBestEffort;
    private bool _isSubmitting;
    private bool _closed;
    private readonly HashSet<string> _selectedReferenceKeys = new(StringComparer.Ordinal);
    private string? _replacementDefaultRoleId;

    [Parameter, EditorRequired]
    public string RoleId { get; set; } = string.Empty;

    [Parameter, EditorRequired]
    public string RoleName { get; set; } = string.Empty;

    /// <summary>Delete capability resolved by the existing role-administration boundary.</summary>
    [Parameter]
    public RoleAdministrationAccess Access { get; set; } = RoleAdministrationAccess.Unavailable;

    [Parameter]
    public IReadOnlyCollection<RoleSummary> ReplacementRoles { get; set; } = [];

    [CascadingParameter]
    private IMudDialogInstance MudDialog { get; set; } = null!;

    [Inject]
    private IRoleDeletionService DeletionService { get; set; } = null!;

    private IReadOnlyCollection<RoleDeletionDependencyResponse> ConfigurationReferences =>
        _impact?.ConfigurationReferences ?? [];

    private IReadOnlyCollection<RoleDeletionDependencyResponse> EditableReferences =>
        _impact?.EditableReferences ?? [];

    private int ConfigurationReferenceCount => ConfigurationReferences.Count;
    private int EditableReferenceCount => EditableReferences.Count;
    private string DependencyVersion => _impact?.DependencyVersion ?? string.Empty;
    private string ErrorMessage => _errorMessage ?? "Role deletion is unavailable right now. Try again in a moment.";
    private string CloseLabel => _state is DeleteRoleDialogViewState.SafeConfirmation or DeleteRoleDialogViewState.ConfigurationBlocked or DeleteRoleDialogViewState.Incomplete
        ? "Close"
        : "Cancel";

    private bool HasFinalDefaultWarning =>
        ConfigurationReferences.Any(x => x.RemovesLastDefaultRole) ||
        (EditableReferences.Count > 0
            ? EditableReferences.Any(x => IsReferenceSelected(x) && x.RemovesLastDefaultRole) ||
              !EditableReferences.Any(x => x.RemovesLastDefaultRole) && HasFinalDefaultWarningCode
            : HasFinalDefaultWarningCode);

    private bool HasFinalDefaultWarningCode =>
        (_impact?.Warnings ?? []).Contains("removes_last_default_role", StringComparer.Ordinal);

    private bool RequiresBestEffortConfirmation =>
        string.Equals(_impact?.ExecutionMode, "bestEffort", StringComparison.OrdinalIgnoreCase);

    private bool CanSubmitRemediation =>
        _impact is not null &&
        !string.IsNullOrWhiteSpace(_impact.DependencyVersion) &&
        (!EditableReferences.Any() || (HasSelectedEditableReferences && _confirmEditableReferences)) &&
        (!HasFinalDefaultWarning || (!string.IsNullOrWhiteSpace(_replacementDefaultRoleId) && _confirmEmptyDefaultRoles)) &&
        (!RequiresBestEffortConfirmation || _confirmBestEffort);

    private bool HasSelectedEditableReferences => EditableReferences.Any(IsReferenceSelected);

    protected override async Task OnInitializedAsync()
    {
        _lifetime = new CancellationTokenSource();

        if (!Access.CanDelete)
        {
            _state = DeleteRoleDialogViewState.Forbidden;
            _errorMessage = "You are not allowed to delete this role.";
            return;
        }

        await InspectAsync(_lifetime.Token);
    }

    private async Task InspectAsync(CancellationToken cancellationToken)
    {
        if (_closed)
            return;

        _state = DeleteRoleDialogViewState.Loading;
        _impact = null;
        _errorMessage = null;
        ResetConfirmations();

        try
        {
            var result = await DeletionService.InspectAsync(RoleId, Access, cancellationToken);
            if (_closed || cancellationToken.IsCancellationRequested)
                return;

            ApplyInspectionResult(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Disposal and an explicit cancellation close the dialog without presenting a stale error.
        }
        catch
        {
            _state = DeleteRoleDialogViewState.Error;
            _errorMessage = "Deletion impact could not be loaded. Try again in a moment.";
        }
    }

    private void ApplyInspectionResult(RoleDeletionInspectionResult result)
    {
        _impact = result.Impact;
        _errorMessage = result.Message;
        _operationCode = result.Code;
        ResetSelectedReferences();
        _state = result.Outcome switch
        {
            RoleDeletionInspectionOutcome.Safe => DeleteRoleDialogViewState.SafeConfirmation,
            RoleDeletionInspectionOutcome.Blocked => DeleteRoleDialogViewState.ConfigurationBlocked,
            RoleDeletionInspectionOutcome.RemediationRequired => DeleteRoleDialogViewState.Remediation,
            RoleDeletionInspectionOutcome.ConfirmationRequired => DeleteRoleDialogViewState.ConfirmationRequired,
            RoleDeletionInspectionOutcome.Forbidden => DeleteRoleDialogViewState.Forbidden,
            RoleDeletionInspectionOutcome.NotFound => DeleteRoleDialogViewState.NotFound,
            _ => DeleteRoleDialogViewState.Error
        };
    }

    private async Task ExecuteAsync()
    {
        if (_closed || _isSubmitting || _impact is null)
            return;

        if (_state is not (DeleteRoleDialogViewState.SafeConfirmation or DeleteRoleDialogViewState.Remediation or DeleteRoleDialogViewState.ConfirmationRequired))
            return;

        if (_state is DeleteRoleDialogViewState.Remediation or DeleteRoleDialogViewState.ConfirmationRequired)
        {
            if (!CanSubmitRemediation)
                return;
        }

        _isSubmitting = true;

        try
        {
            var cancellationToken = _lifetime?.Token ?? CancellationToken.None;
            var result = _state == DeleteRoleDialogViewState.SafeConfirmation
                ? await DeletionService.DeleteAsync(RoleId, Access, cancellationToken)
                : await DeletionService.RemediateAndDeleteAsync(RoleId, Access, new RoleDeletionConfirmation
                {
                    ExpectedDependencyVersion = _impact.DependencyVersion,
                    ConfirmRemoveFromEditableJitPolicies = _confirmEditableReferences,
                    ConfirmEmptyDefaultRoles = _confirmEmptyDefaultRoles,
                    ConfirmBestEffort = _confirmBestEffort,
                    SelectedReferences = EditableReferences
                        .Where(IsReferenceSelected)
                        .Select(ToSelection)
                        .ToArray(),
                    ReplacementDefaultRoleId = _replacementDefaultRoleId
                }, cancellationToken);

            if (_closed || cancellationToken.IsCancellationRequested)
                return;

            ApplyOperationResult(result);
        }
        catch (OperationCanceledException) when (_lifetime?.IsCancellationRequested == true)
        {
            // Disposal and cancellation are intentionally silent.
        }
        catch
        {
            _state = DeleteRoleDialogViewState.Error;
            _errorMessage = "The deletion request failed. Try again in a moment.";
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void ApplyOperationResult(RoleDeletionOperationResult result)
    {
        var previousImpact = _impact;
        if (result.Impact is not null)
        {
            _impact = result.Impact;
            ResetSelectedReferences();
        }

        _errorMessage = result.Message;
        _changedOwnerIds = result.ChangedOwnerIds;

        switch (result.Outcome)
        {
            case RoleDeletionOperationOutcome.Deleted:
                CloseWithResult(DeleteRoleDialogResult.Deleted(RoleId, result.ChangedOwnerIds));
                break;
            case RoleDeletionOperationOutcome.Blocked:
                _state = DeleteRoleDialogViewState.ConfigurationBlocked;
                break;
            case RoleDeletionOperationOutcome.DependencyConflict:
                _operationCode = result.Code;
                _previousDependencyVersion = previousImpact?.DependencyVersion;
                _state = DeleteRoleDialogViewState.DependencyConflict;
                ResetConfirmations();
                break;
            case RoleDeletionOperationOutcome.ConfirmationRequired:
                _operationCode = result.Code;
                _state = DeleteRoleDialogViewState.ConfirmationRequired;
                ResetConfirmations();
                break;
            case RoleDeletionOperationOutcome.Incomplete:
                _operationCode = result.Code;
                _state = DeleteRoleDialogViewState.Incomplete;
                ResetConfirmations();
                break;
            case RoleDeletionOperationOutcome.Forbidden:
                _state = DeleteRoleDialogViewState.Forbidden;
                break;
            case RoleDeletionOperationOutcome.NotFound:
                _state = DeleteRoleDialogViewState.NotFound;
                break;
            default:
                _state = DeleteRoleDialogViewState.Error;
                _errorMessage ??= "The deletion request failed. Try again in a moment.";
                break;
        }
    }

    private void ReviewRefreshedImpact()
    {
        if (_impact is null)
        {
            _state = DeleteRoleDialogViewState.Error;
            _errorMessage = "The refreshed deletion impact was unavailable. Inspect again.";
            return;
        }

        _previousDependencyVersion = null;
        ApplyInspectionResult(new RoleDeletionInspectionResult
        {
            Outcome = ClassifyImpact(_impact),
            Impact = _impact
        });
    }

    private Task InspectAgain() => InspectAsync(_lifetime?.Token ?? CancellationToken.None);

    private static RoleDeletionInspectionOutcome ClassifyImpact(RoleDeletionImpactResponse impact)
    {
        if (impact.ConfigurationReferences.Count != 0)
            return RoleDeletionInspectionOutcome.Blocked;
        if (impact.EditableReferences.Count != 0)
            return impact.CanRemediate
                ? RoleDeletionInspectionOutcome.RemediationRequired
                : RoleDeletionInspectionOutcome.Blocked;
        if (impact.Warnings.Count != 0)
            return RoleDeletionInspectionOutcome.ConfirmationRequired;
        return impact.CanDelete
            ? RoleDeletionInspectionOutcome.Safe
            : RoleDeletionInspectionOutcome.Blocked;
    }

    private void ResetConfirmations()
    {
        _confirmEditableReferences = false;
        _confirmEmptyDefaultRoles = false;
        _confirmBestEffort = false;
    }

    private void ResetSelectedReferences()
    {
        _selectedReferenceKeys.Clear();
        foreach (var reference in EditableReferences)
            _selectedReferenceKeys.Add(ReferenceKey(reference));

        _replacementDefaultRoleId = null;
    }

    private bool IsReferenceSelected(RoleDeletionDependencyResponse reference) =>
        _selectedReferenceKeys.Contains(ReferenceKey(reference));

    private void SetReferenceSelection(RoleDeletionDependencyResponse reference, bool selected)
    {
        var key = ReferenceKey(reference);
        if (selected)
            _selectedReferenceKeys.Add(key);
        else
            _selectedReferenceKeys.Remove(key);
    }

    private void SetReplacementDefaultRole(string? roleId) => _replacementDefaultRoleId = roleId;

    private static string ReferenceKey(RoleDeletionDependencyResponse reference) =>
        $"{reference.Source}\u001F{reference.OwnerId}";

    private static RoleDeletionReferenceSelection ToSelection(RoleDeletionDependencyResponse reference) => new()
    {
        Source = reference.Source,
        OwnerId = reference.OwnerId
    };

    private void Cancel()
    {
        if (_isSubmitting || _closed)
            return;

        CloseWithResult(_state == DeleteRoleDialogViewState.Incomplete
            ? DeleteRoleDialogResult.Retained(RoleId, _operationCode, _changedOwnerIds)
            : DeleteRoleDialogResult.Cancelled(RoleId));
    }

    private void CloseWithResult(DeleteRoleDialogResult result)
    {
        if (_closed)
            return;

        _closed = true;
        MudDialog?.Close(DialogResult.Ok(result));
    }

    private static string GetReferenceName(RoleDeletionDependencyResponse reference) =>
        string.IsNullOrWhiteSpace(reference.OwnerKey) ? reference.OwnerId : reference.OwnerKey;

    private static string GetReferencePath(RoleDeletionDependencyResponse reference) =>
        string.IsNullOrWhiteSpace(reference.ConfigurationPath) ? GetReferenceName(reference) : reference.ConfigurationPath;

    private static string GetPolicyBranch(RoleDeletionDependencyResponse reference) =>
        string.IsNullOrWhiteSpace(reference.PolicyBranch) ? string.Empty : $"· {reference.PolicyBranch}";

    private static string FormatOwnerIds(IEnumerable<string> ownerIds)
    {
        var owners = ownerIds.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
        return owners.Length == 0 ? "None" : string.Join(", ", owners);
    }

    private string FormatRemainingOwners() =>
        FormatOwnerIds(ConfigurationReferences.Concat(EditableReferences).Select(GetReferenceName));

    public ValueTask DisposeAsync()
    {
        _closed = true;
        _lifetime?.Cancel();
        _lifetime?.Dispose();
        _lifetime = null;
        return ValueTask.CompletedTask;
    }
}
