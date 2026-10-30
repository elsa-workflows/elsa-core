using Elsa.Studio.Contracts;

namespace Elsa.Studio.Services;

/// <inheritdoc />
public sealed class PermissionRefreshSignal : IPermissionRefreshSignal
{
    /// <inheritdoc />
    public event Action? Raised;

    /// <inheritdoc />
    public void Raise() => Raised?.Invoke();
}
