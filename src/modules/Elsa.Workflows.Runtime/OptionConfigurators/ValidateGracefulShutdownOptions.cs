using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Validates <see cref="GracefulShutdownOptions"/>.
/// </summary>
public class ValidateGracefulShutdownOptions : IValidateOptions<GracefulShutdownOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, GracefulShutdownOptions options)
    {
        var failures = new List<string>();

        if (options.DrainDeadline <= TimeSpan.Zero)
            failures.Add($"{nameof(GracefulShutdownOptions.DrainDeadline)} must be greater than zero.");

        if (options.IngressPauseTimeout <= TimeSpan.Zero)
            failures.Add($"{nameof(GracefulShutdownOptions.IngressPauseTimeout)} must be greater than zero.");

        if (options.StimulusQueueMaxDepthWhilePaused is <= 0)
            failures.Add($"{nameof(GracefulShutdownOptions.StimulusQueueMaxDepthWhilePaused)} must be null (unlimited) or greater than zero.");

        if (options.MaxForceCancelledInstanceIdsReported <= 0)
            failures.Add($"{nameof(GracefulShutdownOptions.MaxForceCancelledInstanceIdsReported)} must be greater than zero.");

        return failures.Count > 0 ? ValidateOptionsResult.Fail(failures) : ValidateOptionsResult.Success;
    }
}

