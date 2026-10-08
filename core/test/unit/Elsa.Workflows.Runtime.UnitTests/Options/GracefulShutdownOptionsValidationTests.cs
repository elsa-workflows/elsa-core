using Elsa.Workflows.Runtime.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.Workflows.Runtime.UnitTests.Options;

public class GracefulShutdownOptionsValidationTests
{
    public static TheoryData<Action<GracefulShutdownOptions>, string> InvalidConfigurations => new()
    {
        { options => options.DrainDeadline = TimeSpan.Zero, nameof(GracefulShutdownOptions.DrainDeadline) },
        { options => options.IngressPauseTimeout = TimeSpan.Zero, nameof(GracefulShutdownOptions.IngressPauseTimeout) },
        { options => options.StimulusQueueMaxDepthWhilePaused = 0, nameof(GracefulShutdownOptions.StimulusQueueMaxDepthWhilePaused) },
        { options => options.MaxForceCancelledInstanceIdsReported = 0, nameof(GracefulShutdownOptions.MaxForceCancelledInstanceIdsReported) },
    };

    [Fact]
    public void AllowsUnlimitedPausedStimulusQueue()
    {
        using var serviceProvider = CreateServiceProvider(options => options.StimulusQueueMaxDepthWhilePaused = null);

        var options = serviceProvider.GetRequiredService<IOptions<GracefulShutdownOptions>>().Value;

        Assert.Null(options.StimulusQueueMaxDepthWhilePaused);
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void RejectsInvalidConfiguration(Action<GracefulShutdownOptions> configure, string expectedFailure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var exception = Assert.Throws<OptionsValidationException>(() => _ = serviceProvider.GetRequiredService<IOptions<GracefulShutdownOptions>>().Value);

        Assert.Contains(exception.Failures, failure => failure.Contains(expectedFailure));
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void RejectsInvalidConfigurationDuringStartupValidation(Action<GracefulShutdownOptions> configure, string expectedFailure)
    {
        using var serviceProvider = CreateServiceProvider(configure);

        var startupValidator = serviceProvider.GetRequiredService<IStartupValidator>();
        var exception = Assert.Throws<OptionsValidationException>(startupValidator.Validate);

        Assert.Contains(exception.Failures, failure => failure.Contains(expectedFailure));
    }

    private static ServiceProvider CreateServiceProvider(Action<GracefulShutdownOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddGracefulShutdownOptions(configure);
        return services.BuildServiceProvider();
    }
}

