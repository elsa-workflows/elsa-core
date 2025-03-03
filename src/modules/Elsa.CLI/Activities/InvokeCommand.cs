using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using CliWrap;
using CliWrap.Buffered;
using Elsa.CLI.Contracts;
using Elsa.CLI.Options;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.CLI.Activities;

/// <summary>
/// Activity that invokes a command line process using CliWrap.
/// </summary>
[Activity(
    Category = "CLI",
    DisplayName = "Invoke Command",
    Description = "Invokes a command line process using CliWrap"
)]
[PublicAPI]
public class InvokeCommand : Activity
{
    private readonly ILogger<InvokeCommand> _logger;
    private readonly ICommandRunner _commandRunner;
    private readonly CliOptions? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="InvokeCommand"/> class.
    /// </summary>
    public InvokeCommand(
        ILogger<InvokeCommand> logger,
        ICommandRunner commandRunner,
        IOptions<CliOptions> options,
        [CallerFilePath] string? source = default,
        [CallerLineNumber] int? line = default) : base(source, line)
    {
        _logger = logger;
        _commandRunner = commandRunner;
        _options = options.Value;
    }

    /// <summary>
    /// The command to execute. This can be a CliWrap Command object or a string representing the command name.
    /// </summary>
    [Input(
        Description = "The command to execute. Can be passed directly or as a string of the executable name.",
        UIHint = InputUIHints.DropDown
    )]
    public Input<object> Command { get; set; } = default!;

    /// <summary>
    /// The arguments to pass to the command. This can be an array of strings or a single string.
    /// </summary>
    [Input(
        Description = "The arguments to pass to the command. Can be an array of strings or a single string."
    )]
    public Input<object> Arguments { get; set; } = default!;

    /// <summary>
    /// The working directory to execute the command in.
    /// </summary>
    [Input(
        Description = "The working directory to execute the command in."
    )]
    public Input<string> WorkingDirectory { get; set; } = default!;

    /// <summary>
    /// Environment variables to set for the command.
    /// </summary>
    [Input(
        Description = "Environment variables to set for the command as key-value pairs."
    )]
    public Input<IDictionary<string, string>> EnvironmentVariables { get; set; } = default!;

    /// <summary>
    /// Credentials to run the command under. Format: Domain, Username, Password
    /// </summary>
    [Input(
        Description = "Credentials to run the command under. Format: Domain, Username, Password"
    )]
    public Input<(string Domain, string Username, string Password)?> Credentials { get; set; } = default!;

    /// <summary>
    /// The expected exit code for a successful command execution. If null, exit code validation is disabled.
    /// </summary>
    [Input(
        Description = "The expected exit code for a successful command execution. If blank, exit code validation is disabled."
    )]
    public Input<int?> SuccessfulExitCode { get; set; } = default!;

    /// <summary>
    /// A string or regex pattern that should be present in the standard output for a successful command execution. If null, output validation is disabled.
    /// </summary>
    [Input(
        Description = "A string or regex pattern that should be present in the standard output for a successful command execution. If blank, output validation is disabled."
    )]
    public Input<string> SuccessfulOutputText { get; set; } = default!;

    /// <summary>
    /// The timeout for the command execution. If null, no timeout is applied.
    /// </summary>
    [Input(
        Description = "The timeout for the command execution. If blank, no timeout is applied."
    )]
    public Input<TimeSpan?> Timeout { get; set; } = default!;

    /// <summary>
    /// The CliWrap command instance that was used to invoke the CLI command.
    /// </summary>
    [Output(Description = "The CliWrap command instance that was used to invoke the CLI command.")]
    public Output<Command> CommandOutput { get; set; } = default!;

    /// <summary>
    /// Indicates whether the command execution was successful based on the configured validation criteria.
    /// </summary>
    [Output(Description = "Indicates whether the command execution was successful based on the configured validation criteria.")]
    public Output<bool> IsSuccess { get; set; } = default!;

    /// <summary>
    /// The standard output from the command execution.
    /// </summary>
    [Output(Description = "The standard output from the command execution.")]
    public Output<string> StandardOutput { get; set; } = default!;

    /// <summary>
    /// The standard error from the command execution.
    /// </summary>
    [Output(Description = "The standard error from the command execution.")]
    public Output<string> StandardError { get; set; } = default!;

    /// <summary>
    /// The exit code from the command execution.
    /// </summary>
    [Output(Description = "The exit code from the command execution.")]
    public Output<int> ExitCode { get; set; } = default!;

    /// <summary>
    /// The start time of the command execution.
    /// </summary>
    [Output(Description = "The start time of the command execution.")]
    public Output<DateTimeOffset> StartTime { get; set; } = default!;

    /// <summary>
    /// The exit time of the command execution.
    /// </summary>
    [Output(Description = "The exit time of the command execution.")]
    public Output<DateTimeOffset> ExitTime { get; set; } = default!;

    /// <summary>
    /// The runtime of the command execution.
    /// </summary>
    [Output(Description = "The runtime of the command execution.")]
    public Output<TimeSpan> RunTime { get; set; } = default!;

    /// <summary>
    /// The activity to execute when an error occurs while trying to send the email.
    /// </summary>
    [Port] public IActivity? Error { get; set; }

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var cmd = BuildCommand(context);

        try
        {
            var cancellationToken = GetCancellationToken(context);
            var result = await _commandRunner.ExecuteCommandAsync(cmd, cancellationToken);

            // Store outputs
            context.Set(CommandOutput, cmd);
            context.Set(IsSuccess, ValidateExecution(context, result));
            context.Set(StandardOutput, result.StandardOutput);
            context.Set(StandardError, result.StandardError);
            context.Set(ExitCode, result.ExitCode);
            context.Set(StartTime, result.StartTime);
            context.Set(ExitTime, result.ExitTime);
            context.Set(RunTime, result.RunTime);

            _logger.LogInformation("Invoked Command: {Command}, Exit Code: {ExitCode}", cmd.ToString(), result.ExitCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command {Command}", cmd.ToString());
            context.Set(StandardError, ex.ToString());
            context.Set(IsSuccess, false);

            if (Error != null)
            {
                await context.ScheduleActivityAsync(Error, OnErrorCompletedAsync);
            }
        }

        await context.CompleteActivityAsync();
    }

    private async ValueTask OnErrorCompletedAsync(ActivityCompletedContext context) =>
        await context.TargetContext.CompleteActivityAsync();

    private Command BuildCommand(ActivityExecutionContext context)
    {
        Command cmd;
        var commandInput = context.Get(Command);

        // Handle different Command input types
        if (commandInput is Command existingCommand)
        {
            cmd = existingCommand;
        }
        else if (commandInput is string commandStr)
        {
            cmd = Cli.Wrap(commandStr);
        }
        else
        {
            throw new ArgumentException("Command must be either a CliWrap Command object or a string.");
        }

        // Validation is handled by the command runner
        cmd.WithValidation(CommandResultValidation.None);

        // Apply arguments
        var arguments = context.Get(Arguments);
        if (arguments != null)
        {
            if (arguments is string[] argsArray)
            {
                cmd = cmd.WithArguments(argsArray);
            }
            else if (arguments is string argsStr)
            {
                cmd = cmd.WithArguments(argsStr);
            }
            else if (arguments is IEnumerable<string> argsEnumerable)
            {
                cmd = cmd.WithArguments(argsEnumerable.ToArray());
            }
        }

        // Apply working directory
        var workingDirectory = context.Get(WorkingDirectory);
        if (!string.IsNullOrEmpty(workingDirectory))
        {
            cmd = cmd.WithWorkingDirectory(workingDirectory);
        }
        else if (!string.IsNullOrEmpty(_options?.DefaultWorkingDirectory))
        {
            cmd = cmd.WithWorkingDirectory(_options.DefaultWorkingDirectory);
        }

        // Merge environment variables
        var environmentVariables = context.Get(EnvironmentVariables);
        var mergedVariables = new Dictionary<string, string?>();

        // First add default environment variables from options
        if (_options?.DefaultEnvironmentVariables is { Count: > 0 })
        {
            foreach (var kvp in _options.DefaultEnvironmentVariables)
            {
                mergedVariables[kvp.Key] = kvp.Value;
            }
        }

        // Then add (and override) with activity-specific environment variables
        if (environmentVariables is { Count: > 0 })
        {
            foreach (var kvp in environmentVariables)
            {
                mergedVariables[kvp.Key] = kvp.Value;
            }
        }

        // Apply the merged environment variables
        if (mergedVariables.Count > 0)
        {
            cmd = cmd.WithEnvironmentVariables(mergedVariables);
        }

        // Apply credentials if specified
        var credentials = context.Get(Credentials);
        if (credentials.HasValue)
        {
            var (domain, username, password) = credentials.Value;
            cmd = cmd.WithCredentials(new Credentials(username, password, domain));
        }

        return cmd;
    }

    private CancellationToken GetCancellationToken(ActivityExecutionContext context)
    {
        var timeout = context.Get(Timeout);

        if (!timeout.HasValue && _options?.DefaultTimeout is null)
        {
            return context.CancellationToken;
        }

        // Create a timeout-based cancellation token source
        var timeoutCts = new CancellationTokenSource();
        if (timeout.HasValue)
        {
            timeoutCts.CancelAfter(timeout.Value);
        }
        else if (_options?.DefaultTimeout is null)
        {
            timeoutCts.CancelAfter(_options!.DefaultTimeout!.Value);
        }

        var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            context.CancellationToken,
            timeoutCts.Token);

        return cancellationTokenSource.Token;
    }

    private bool ValidateExecution(ActivityExecutionContext context, BufferedCommandResult result)
    {
        // If no validation criteria are specified, consider the execution successful
        var exitCodeValid = result.IsSuccess;
        var outputTextValid = result.IsSuccess;

        // Validate exit code if specified
        var successfulExitCode = context.Get(SuccessfulExitCode);
        if (successfulExitCode.HasValue)
        {
            exitCodeValid = result.ExitCode == successfulExitCode.Value;
        }

        // Validate output text if specified
        var successfulOutputText = context.Get(SuccessfulOutputText);

        if (!string.IsNullOrEmpty(successfulOutputText))
        {
            try
            {
                // Check if it's a regex pattern
                if (successfulOutputText.StartsWith("/") && successfulOutputText.EndsWith("/"))
                {
                    var pattern = successfulOutputText.Substring(1, successfulOutputText.Length - 2);
                    outputTextValid = Regex.IsMatch(result.StandardOutput, pattern);
                }
                else
                {
                    // Simple string contains check
                    outputTextValid = result.StandardOutput.Contains(successfulOutputText);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating output text with pattern: {Pattern}", successfulOutputText);
                outputTextValid = false;
            }
        }

        return exitCodeValid && outputTextValid;
    }
}