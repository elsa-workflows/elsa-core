using Elsa.Scheduling.Quartz;
using Quartz;

namespace Elsa.Scheduling.Quartz.UnitTests.Services;

public class QuartzTriggerKeysTests
{
    [Fact]
    public void GetRetryTriggerKey_UsesTheReservedRetryGroup()
    {
        var original = new TriggerKey("task-1", "tenant-a");

        var retry = QuartzTriggerKeys.GetRetryTriggerKey(original);

        Assert.StartsWith("retry-", retry.Name);
        Assert.Equal(QuartzTriggerKeys.RetryGroup, retry.Group);
    }

    [Fact]
    public void GetRetryTriggerKey_WhenTaskNamesWouldCollide_ProducesDistinctKeys()
    {
        var first = QuartzTriggerKeys.GetRetryTriggerKey(new TriggerKey("task-1", "Default"));
        var second = QuartzTriggerKeys.GetRetryTriggerKey(new TriggerKey("task-1-retry", "Default"));

        Assert.NotEqual(first, second);
        Assert.NotEqual(first, new TriggerKey("task-1-retry", "Default"));
    }

    [Fact]
    public void GetCancellationMarkerJobKey_UsesAStableReservedIdentity()
    {
        var original = new TriggerKey("task-1", "tenant-a");

        var marker = QuartzTriggerKeys.GetCancellationMarkerJobKey(original);

        Assert.StartsWith("cancellation-", marker.Name);
        Assert.Equal(QuartzTriggerKeys.CancellationGroup, marker.Group);
        Assert.Equal(marker, QuartzTriggerKeys.GetCancellationMarkerJobKey(original));
    }

    [Fact]
    public void IsRetryTrigger_OriginalTriggerWithRetrySuffix_ReturnsFalse()
    {
        var trigger = TriggerBuilder.Create().WithIdentity("task-1-retry", "Default").Build();

        Assert.False(QuartzTriggerKeys.IsRetryTrigger(trigger));
    }

    [Fact]
    public void IsRetryTrigger_MarkedTrigger_ReturnsTrue()
    {
        var trigger = TriggerBuilder.Create()
            .WithIdentity("task-1-retry", "Default")
            .UsingJobData(QuartzJobDataKeys.RetryTrigger, bool.TrueString)
            .Build();

        Assert.True(QuartzTriggerKeys.IsRetryTrigger(trigger));
    }

    [Fact]
    public void GetOriginalTriggerKey_UsesPersistedOriginalIdentity()
    {
        var trigger = TriggerBuilder.Create()
            .WithIdentity("retry-123", QuartzTriggerKeys.RetryGroup)
            .UsingJobData(QuartzJobDataKeys.RetryTrigger, bool.TrueString)
            .UsingJobData(QuartzJobDataKeys.RetryOriginalTriggerName, "task-1-retry")
            .UsingJobData(QuartzJobDataKeys.RetryOriginalTriggerGroup, "Default")
            .Build();

        Assert.Equal(new TriggerKey("task-1-retry", "Default"), QuartzTriggerKeys.GetOriginalTriggerKey(trigger));
    }

}
