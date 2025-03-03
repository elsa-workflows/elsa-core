using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Microsoft.VisualBasic;

namespace Elsa.CLI.Services;

/// <summary>
/// Helper class to find commands in PATH for autocomplete.
/// </summary>
public class CommandFinder
{
    private readonly ILogger<CommandFinder> _logger;
    private readonly Lazy<IEnumerable<string>> _availableCommands;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandFinder"/> class.
    /// </summary>
    public CommandFinder(ILogger<CommandFinder> logger)
    {
        _logger = logger;
        _availableCommands = new Lazy<IEnumerable<string>>(GetCommandsFromPath);
    }

    /// <summary>
    /// Gets all available commands from the PATH environment variable.
    /// </summary>
    public IEnumerable<string> GetAvailableCommands() => _availableCommands.Value;

    private IEnumerable<string> GetCommandsFromPath()
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrEmpty(pathVariable))
        {
            _logger.LogWarning("PATH environment variable is empty or not available");
            return [];
        }

        var pathDirectories = pathVariable.Split(Path.PathSeparator);
        HashSet<string> uniqueCommands = new(StringComparer.OrdinalIgnoreCase);

        foreach (var directory in pathDirectories)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory);

                    foreach (var file in files)
                    {
                        var fileName = Path.GetFileName(file);

                        if (IsExecutable(fileName))
                        {
                            var commandName = Path.GetFileNameWithoutExtension(fileName);
                            uniqueCommands.Add(commandName);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Continue with next directory
            }
        }

        _logger.LogInformation("Found {Count} commands in PATH", uniqueCommands.Count);

        return uniqueCommands.OrderBy(cmd => cmd);
    }

    private static bool IsExecutable(string fileName)
    {
        List<string> executableExtensions = [];

        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.Win32NT or PlatformID.Win32S or PlatformID.Win32Windows or PlatformID.WinCE:
                executableExtensions = [".exe", ".cmd", ".bat", ".ps1", ".vbs", ""];
                break;
            case PlatformID.Unix or PlatformID.MacOSX:
                executableExtensions = ["", ".sh"];
                break;
        }

        var extension = Path.GetExtension(fileName).ToLower();
        return executableExtensions.Contains(extension);
    }
}