namespace Elsa.CLI.Options;

/// <summary>
/// Options to configure the CLI wrapper with.
/// </summary>
public class CliOptions
{
    /// <summary>
    /// The default working directory when none is specified in the activity.
    /// </summary>
    public string? DefaultWorkingDirectory { get; set; }

    /// <summary>
    /// The default timeout for command execution.
    /// </summary>
    public TimeSpan? DefaultTimeout { get; set; }

    /// <summary>
    /// Default environment variables to include with all commands.
    /// </summary>
    public IDictionary<string, string>? DefaultEnvironmentVariables { get; set; }

    /// <summary>
    /// Whether to allow the commands to be executed with specified credentials.
    /// </summary>
    public bool AllowRunAs { get; set; } = false;

    /// <summary>
    /// List of allowed commands. If empty, all commands are allowed.
    /// </summary>
    public ICollection<string>? AllowedCommands { get; set; }

    /// <summary>
    /// List of denied commands. These commands will not be allowed even if AllowedCommands is empty.
    /// </summary>
    public ICollection<string>? DeniedCommands { get; set; }
}