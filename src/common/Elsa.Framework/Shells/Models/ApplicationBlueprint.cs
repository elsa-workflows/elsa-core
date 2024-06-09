namespace Elsa.Framework.Shells;

public class ApplicationBlueprint
{
    private readonly List<ShellBlueprint> _shellBlueprints = new();

    public ApplicationBlueprint(IEnumerable<ShellBlueprint> shellBlueprints)
    {
        AddShellBlueprints(shellBlueprints);
    }
    
    public IReadOnlyCollection<ShellBlueprint> ShellBlueprints => _shellBlueprints.ToList();
    
    public void AddShellBlueprint(ShellBlueprint shellBlueprint)
    {
        _shellBlueprints.Add(shellBlueprint);
    }
    
    public void AddShellBlueprints(IEnumerable<ShellBlueprint> shellBlueprints)
    {
        _shellBlueprints.AddRange(shellBlueprints);
    }
    
    public void RemoveShellBlueprint(ShellBlueprint shellBlueprint)
    {
        _shellBlueprints.Remove(shellBlueprint);
    }
}