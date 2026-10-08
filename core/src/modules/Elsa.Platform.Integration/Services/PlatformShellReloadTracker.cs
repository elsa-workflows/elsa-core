namespace Elsa.Platform.Integration.Services;

public sealed class PlatformShellReloadTracker
{
    private readonly HashSet<string> _shellIds = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<string> ShellIds => _shellIds;

    public void MarkForReload(string shellId)
    {
        if (!string.IsNullOrWhiteSpace(shellId))
            _shellIds.Add(shellId);
    }
}
