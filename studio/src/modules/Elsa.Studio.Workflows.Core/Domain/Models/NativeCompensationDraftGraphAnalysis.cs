namespace Elsa.Studio.Workflows.Domain.Models;

/// <summary>
/// Result of contextual native compensation graph classification.
/// </summary>
public sealed record NativeCompensationDraftGraphAnalysis(
    bool IsComplete,
    bool ContainsCompensationScope,
    bool ContainsUnsupportedScope,
    bool HasPotentialScopeMarker)
{
    public static NativeCompensationDraftGraphAnalysis Ordinary { get; } = new(true, false, false, false);

    public bool RequiresNativeProtection => ContainsCompensationScope || !IsComplete && HasPotentialScopeMarker;

    public bool IsSafeNativeDraft => IsComplete && ContainsCompensationScope && !ContainsUnsupportedScope;

    public bool MustRejectProtectedWrite => RequiresNativeProtection && (!IsComplete || ContainsUnsupportedScope);

    public static NativeCompensationDraftGraphAnalysis Indeterminate { get; } = new(false, false, false, true);
}
