namespace Elsa.Framework.Shells;

public interface IShellFactory
{
    /// <summary>
    /// Creates a new Shell object from the blueprint.
    /// </summary>
    /// <returns>A newly created <see cref="Shell"/> object.</returns>
    Shell CreateShell(ShellBlueprint blueprint);
}