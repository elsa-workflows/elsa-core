namespace Elsa.Framework.Shells;

public interface IElsaApplication
{
    /// The ID of application shell.
    string ApplicationShellId { get; }
    
    ShellBlueprint ApplicationShell { get; }
    IReadOnlyDictionary<string, ShellBlueprint> ShellBlueprints { get; }
}