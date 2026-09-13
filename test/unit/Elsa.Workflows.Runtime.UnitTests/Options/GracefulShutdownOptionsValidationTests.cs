using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.UnitTests.Options;

public class GracefulShutdownOptionsValidationTests
{
    public static IEnumerable<(Action<GracefulShutdownOptions> Configure, string ExpectedFailure)> InvalidConfigurations =>
    [
        (options => options.DrainDeadline = TimeSpan.Zero, nameof(GracefulShutdownOptions.DrainDeadline)),
        (options => options.IngressPauseTimeout = TimeSpan.Zero, nameof(GracefulShutdownOptions.IngressPauseTimeout)),
        (options => options.StimulusQueueMaxDepthWhilePaused = 0, nameof(GracefulShutdownOptions.StimulusQueueMaxDepthWhilePaused)),
        (options => options.MaxForceCancelledInstanceIdsReported = 0, nameof(GracefulShutdownOptions.MaxForceCancelledInstanceIdsReported))
    ];

    [Test]
    public async Task AllowsUnlimitedPausedStimulusQueue()
    {
        using var serviceProvider = CreateServiceProvider(options => options.StimulusQueueMaxDepthWhilePaused = null);

        var options = serviceProvider.GetRequiredService<IOptions<GracefulShutdownOptions>>().Value;

        await Assert.That(options.StimulusQueueMaxDepthWhilePaused).IsNull();
    }

    [Test]
    [MethodDataSource(nameof(InvalidConfigurations))]
    [DisplayName("Rejects invalid configuration for $expectedFailure")]
    public async Task RejectsInvalidConfiguration(Action<GracefulShutdownOptions> configure, string expectedFailure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var exception = Assert.ThrowsExactly<OptionsValidationException>(() => _ = serviceProvider.GetRequiredService<IOptions<GracefulShutdownOptions>>().Value);

        await Assert.That(exception.Failures).Contains(failure => failure.Contains(expectedFailure, StringComparison.CurrentCulture));
    }

    [Test]
    [MethodDataSource(nameof(InvalidConfigurations))]
    [DisplayName("Rejects invalid startup configuration for $expectedFailure")]
    public async Task RejectsInvalidConfigurationDuringStartupValidation(Action<GracefulShutdownOptions> configure, string expectedFailure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        var exception = Assert.ThrowsExactly<OptionsValidationException>(startupValidator.Validate);

        await Assert.That(exception.Failures).Contains(failure => failure.Contains(expectedFailure, StringComparison.CurrentCulture));
    }

    private static ServiceProvider CreateServiceProvider(Action<GracefulShutdownOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddGracefulShutdownOptions(configure);
        return services.BuildServiceProvider();
    }
}
