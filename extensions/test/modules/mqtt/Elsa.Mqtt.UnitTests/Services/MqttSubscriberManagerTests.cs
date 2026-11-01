using Elsa.Mqtt.Contracts;
using Elsa.Mqtt.Options;
using Elsa.Mqtt.Services;
using Elsa.Mqtt.Stimuli;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MQTTnet;
using NSubstitute;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace Elsa.Mqtt.UnitTests.Services;

public class MqttSubscriberManagerTests
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<Elsa.Mqtt.Activities.MqttMessageReceived>();

    [Fact]
    public async Task BindBookmarksAsync_CreatesClientOnce_WhenCalledConcurrentlyForSameConnection()
    {
        const int callerCount = 16;
        var factory = new GatedMqttClientFactory(Substitute.For<IMqttClient>());
        var manager = new MqttSubscriberManager(
            MsOptions.Create(new MqttOptions()),
            Substitute.For<IServiceScopeFactory>(),
            NullLoggerFactory.Instance,
            NullLogger<MqttSubscriberManager>.Instance,
            factory);

        var callers = Enumerable.Range(0, callerCount)
            .Select(i => Task.Run(() => manager.BindBookmarksAsync([MakeBookmark($"b{i}", "Default")])))
            .ToList();

        await factory.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(200);
        factory.Gate.SetResult();
        await Task.WhenAll(callers).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(1, factory.CallCount);
    }

    private static StoredBookmark MakeBookmark(string id, string connectionName) => new()
    {
        Id = id,
        Name = ActivityTypeName,
        WorkflowInstanceId = "wf-" + id,
        Payload = new MqttMessageReceivedStimulus { ConnectionName = connectionName, Topics = ["sensors/temp"] }
    };

    private sealed class GatedMqttClientFactory(IMqttClient client) : IMqttClientFactory
    {
        private int _callCount;

        public int CallCount => _callCount;
        public TaskCompletionSource FirstCallStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Gate { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IMqttClient> CreateClientAsync(string? connectionName = null, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            FirstCallStarted.TrySetResult();
            await Gate.Task;
            return client;
        }
    }
}
