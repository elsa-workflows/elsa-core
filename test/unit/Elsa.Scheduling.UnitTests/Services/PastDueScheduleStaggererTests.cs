using Elsa.Scheduling.Options;
using Elsa.Scheduling.Services;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace Elsa.Scheduling.UnitTests.Services;

public class PastDueScheduleStaggererTests
{
    [Fact]
    public void GetDelay_DoesNotExceedConfiguredWindow()
    {
        var staggerer = new PastDueScheduleStaggerer(OptionsFactory.Create(new SchedulingOptions
        {
            MinimumPastDueScheduleDelay = TimeSpan.FromSeconds(1),
            PastDueScheduleStaggerInterval = TimeSpan.FromMilliseconds(900),
            PastDueScheduleStaggerWindow = TimeSpan.FromSeconds(5)
        }));

        var delays = Enumerable.Range(0, 16).Select(_ => staggerer.GetDelay(TimeSpan.Zero)).ToList();

        Assert.All(delays, delay => Assert.InRange(delay, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5)));
    }
}
