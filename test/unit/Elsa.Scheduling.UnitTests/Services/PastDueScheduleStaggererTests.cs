using Elsa.Scheduling.Options;
using Elsa.Scheduling.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Scheduling.UnitTests.Services;

public class PastDueScheduleStaggererTests
{
    [Test]
    public async Task GetDelay_DoesNotExceedConfiguredWindow()
    {
        var staggerer = new PastDueScheduleStaggerer(OptionsFactory.Create(new SchedulingOptions
        {
            MinimumPastDueScheduleDelay = TimeSpan.FromSeconds(1),
            PastDueScheduleStaggerInterval = TimeSpan.FromMilliseconds(900),
            PastDueScheduleStaggerWindow = TimeSpan.FromSeconds(5)
        }));

        var delays = Enumerable.Range(0, 16).Select(_ => staggerer.GetDelay(TimeSpan.Zero)).ToList();

        foreach (var delay in delays)
        {
            await Assert.That(delay).IsGreaterThanOrEqualTo(TimeSpan.FromSeconds(1));
            await Assert.That(delay).IsLessThanOrEqualTo(TimeSpan.FromSeconds(5));
        }
    }
}
