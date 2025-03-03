using CliWrap;
using CliWrap.Buffered;
using Elsa.CLI.Contracts;
using Microsoft.Extensions.Logging;

namespace Elsa.CLI.Services;

/// <summary>
/// Default implementation of <see cref="ICommandRunner"/>.
/// </summary>
public class CliWrapCommandRunner : ICommandRunner
{
    private readonly ILogger<CliWrapCommandRunner> _logger;
    private readonly ICommandValidator _validator;

    /// <summary>
    /// Constructor.
    /// </summary>
    public CliWrapCommandRunner(
        ILogger<CliWrapCommandRunner> logger,
        ICommandValidator validator)
    {
        _logger = logger;
        _validator = validator;
    }

    /// <inheritdoc />
    public async Task<BufferedCommandResult> ExecuteCommandAsync(Command command, CancellationToken cancellationToken)
    {
        var commandName = command.TargetFilePath;

        if (!_validator.IsCommandAllowed(commandName))
            throw new InvalidOperationException($"The command '{commandName}' is not allowed to be executed.");

        if (!_validator.IsRunAsAllowed())
            throw new InvalidOperationException("Running commands with alternate credentials is not allowed.");

        _logger.LogInformation("Executing command: {Command}", commandName);

        try
        {
            var result = await command.ExecuteBufferedAsync(cancellationToken);

            _logger.LogInformation("Command {Command} completed with exit code {ExitCode}", commandName, result.ExitCode);

            if (!string.IsNullOrEmpty(result.StandardError))
                _logger.LogDebug("Command {Command} stderr: {Error}", commandName, result.StandardError);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command {Command}", commandName);
            throw;
        }
    }
}