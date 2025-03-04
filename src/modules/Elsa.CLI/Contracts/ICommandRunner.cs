using CliWrap;
using CliWrap.Buffered;

namespace Elsa.CLI.Contracts;

/// <summary>
/// Runs command line processes.
/// </summary>
public interface ICommandRunner
{
    /// <summary>
    /// Executes the specified command and returns the result.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The result of the command execution.</returns>
    Task<BufferedCommandResult> ExecuteCommandAsync(Command command, CancellationToken cancellationToken);
}