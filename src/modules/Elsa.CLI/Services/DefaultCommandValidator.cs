using Elsa.CLI.Contracts;
using Elsa.CLI.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.CLI.Services;

/// <summary>
/// Default implementation of <see cref="ICommandValidator"/>.
/// </summary>
public class DefaultCommandValidator : ICommandValidator
{
    private readonly CliOptions _options;
    private readonly ILogger<DefaultCommandValidator> _logger;

    /// <summary>
    /// Constructor.
    /// </summary>
    public DefaultCommandValidator(
        IOptions<CliOptions> options,
        ILogger<DefaultCommandValidator> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsCommandAllowed(string command)
    {
        // If there are denied commands and this command is in the list, deny it
        if (_options.DeniedCommands?.Any() == true &&
            _options.DeniedCommands.Any(c => string.Equals(c, command, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Command {Command} is explicitly denied", command);
            return false;
        }

        // If there are allowed commands and this command is not in the list, deny it
        if (_options.AllowedCommands?.Any() == true &&
            !_options.AllowedCommands.Any(c => string.Equals(c, command, StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Command {Command} is not in the allowed list", command);
            return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool IsRunAsAllowed() => _options.AllowRunAs;
}