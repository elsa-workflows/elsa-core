using Bpmn.Model;
using Elsa.Bpmn.Hosting;
using Elsa.Scheduling;
using Elsa.Scheduling.Bookmarks;
using Elsa.Workflows;

namespace Elsa.Bpmn.UnitTests;

/// <summary>
/// Resolving a trigger-started process's start event from the stimulus hash of the stored trigger that fired. Message
/// and interval-timer starts are covered end to end in <c>BpmnTriggeredStartTests</c>; these cover what no runtime
/// path there exercises.
/// </summary>
public class BpmnStartEventResolverTests
{
    private static readonly IStimulusHasher Hasher = new DescribingStimulusHasher();

    [Fact(DisplayName = "A cron start is matched by hashing the cron payload it registers")]
    public async Task ResolveByStimulusHash_CronStart_MatchesTheStartThatRegisteredIt()
    {
        var hash = Hasher.Hash(SchedulingStimulusNames.Cron, new CronTriggerPayload("0 0 * * * ?"));

        var signal = await BpmnStartEventResolver.ResolveByStimulusHashAsync(TwoCronStarts(), hash, Hasher, NoStoredTimer);

        Assert.Equal("hourlyStart", signal?.StartElementId);
    }

    [Fact(DisplayName = "A hash none of the start events registered resolves to nothing, so the kind-based fallback decides")]
    public async Task ResolveByStimulusHash_UnknownHash_ResolvesToNothing()
    {
        var signal = await BpmnStartEventResolver.ResolveByStimulusHashAsync(TwoCronStarts(), "unknown", Hasher, NoStoredTimer);

        Assert.Null(signal);
    }

    private static ValueTask<TimeSpan?> NoStoredTimer() => throw new InvalidOperationException("A process without interval timer starts never reads the stored trigger.");

    private static BpmnProcessDefinition TwoCronStarts() =>
        new BpmnProcessBuilder("process")
            .StartEvent("dailyStart", null, Cron("0 0 0 * * ?"))
            .EndEvent("dailyEnd")
            .StartEvent("hourlyStart", null, Cron("0 0 * * * ?"))
            .EndEvent("hourlyEnd")
            .ConnectSequence("dailyStart", "dailyEnd")
            .ConnectSequence("hourlyStart", "hourlyEnd")
            .Build();

    private static BpmnEventDefinition Cron(string expression) =>
        new(BpmnEventDefinitionTypes.Timer, new Dictionary<string, string>(StringComparer.Ordinal) { [BpmnEventDefinitionProperties.Cron] = expression });

    // A stand-in that is equal for equal stimuli, which is all the resolver relies on.
    private sealed class DescribingStimulusHasher : IStimulusHasher
    {
        public string Hash(string stimulusName, object? payload = null, string? activityInstanceId = null) => $"{stimulusName}|{payload}|{activityInstanceId}";
    }
}
