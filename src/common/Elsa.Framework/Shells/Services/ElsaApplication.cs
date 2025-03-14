using System.Collections.ObjectModel;

namespace Elsa.Framework.Shells.Services;

public class ElsaApplication : IElsaApplication
{
    private readonly IDictionary<string, ShellBlueprint> _shellBlueprints;

    public ElsaApplication(string applicationShellId, IEnumerable<ShellBlueprint> shellBlueprints)
    {
        _shellBlueprints = shellBlueprints.ToDictionary(x => x.Id);
        ApplicationShellId = applicationShellId;
        ApplicationShell = _shellBlueprints[applicationShellId];
    }

    public string ApplicationShellId { get; }
    public ShellBlueprint ApplicationShell { get; }
    public IReadOnlyDictionary<string, ShellBlueprint> ShellBlueprints => new ReadOnlyDictionary<string, ShellBlueprint>(_shellBlueprints);
}