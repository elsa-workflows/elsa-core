namespace Elsa.CLI.Contracts;

/// <summary>
/// Validates if a command is allowed to be executed.
/// </summary>
public interface ICommandValidator
{
    /// <summary>
    /// Validates if the specified command is allowed to be executed.
    /// </summary>
    /// <param name="command">The command to validate.</param>
    /// <returns>True if the command is allowed; otherwise false.</returns>
    bool IsCommandAllowed(string command);

    /// <summary>
    /// Validates if running with the specified credentials is allowed.
    /// </summary>
    /// <returns>True if running with credentials is allowed; otherwise false.</returns>
    bool IsRunAsAllowed();
}