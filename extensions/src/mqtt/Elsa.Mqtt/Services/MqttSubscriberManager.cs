using Elsa.Extensions;
using Elsa.Mqtt.Contracts;
using Elsa.Mqtt.Models;
using Elsa.Mqtt.Options;
using Elsa.Mqtt.Stimuli;
using Elsa.Workflows.Helpers;
using Elsa.Workflows.Management;
using Elsa.Workflows.Runtime.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Elsa.Mqtt.Services;

/// <summary>
/// Singleton service that manages one <see cref="MqttSubscriber"/> per named MQTT connection
/// and keeps trigger/bookmark bindings in sync with registered workflows.
/// </summary>
internal class MqttSubscriberManager : IMqttSubscriberManager
{
    private static readonly string ActivityTypeName = ActivityTypeNameHelper.GenerateTypeName<Activities.MqttMessageReceived>();
    
    private readonly MqttOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<MqttSubscriberManager> _logger;
    private readonly IMqttClientFactory _mqttClientFactory;
    private readonly Dictionary<string, MqttSubscriber> _subscribers = new(StringComparer.OrdinalIgnoreCase);

    public MqttSubscriberManager(
        IOptions<MqttOptions> options,
        IServiceScopeFactory scopeFactory,
        ILoggerFactory loggerFactory,
        ILogger<MqttSubscriberManager> logger,
        IMqttClientFactory mqttClientFactory)
    {
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _loggerFactory = loggerFactory;
        _logger = logger;
        _mqttClientFactory = mqttClientFactory;
    }

    /// <inheritdoc/>
    public async Task UpdateSubscriptionsAsync(CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var triggerStore = scope.ServiceProvider.GetRequiredService<Workflows.Runtime.ITriggerStore>();
        var bookmarkStore = scope.ServiceProvider.GetRequiredService<Workflows.Runtime.IBookmarkStore>();

        var triggerFilter = new Workflows.Runtime.Filters.TriggerFilter { Name = ActivityTypeName };
        var bookmarkFilter = new Workflows.Runtime.Filters.BookmarkFilter { Name = ActivityTypeName };

        var triggers = await triggerStore.FindManyAsync(triggerFilter, cancellationToken);
        var bookmarks = await bookmarkStore.FindManyAsync(bookmarkFilter, cancellationToken);

        await BindTriggersAsync(triggers, cancellationToken);
        await BindBookmarksAsync(bookmarks, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task BindTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var relevant = triggers.Where(t => t.Name == ActivityTypeName).ToList();

        if (relevant.Count == 0)
        {
            return;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var workflowDefinitionService = scope.ServiceProvider.GetRequiredService<IWorkflowDefinitionService>();

        foreach (var trigger in relevant)
        {
            var stimulus = trigger.GetPayload<MqttMessageReceivedStimulus>();
            var subscriber = await GetOrCreateSubscriberAsync(stimulus.ConnectionName, cancellationToken);

            if (subscriber == null)
            {
                continue;
            }

            var workflow = await workflowDefinitionService.FindWorkflowAsync(
                definitionVersionId: trigger.WorkflowDefinitionVersionId,
                cancellationToken: cancellationToken);

            if (workflow == null)
            {
                continue;
            }

            await subscriber.BindTriggerAsync(new MqttTriggerBinding(workflow, trigger.Id, trigger.ActivityId, stimulus));
        }
    }

    /// <inheritdoc/>
    public async Task UnbindTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default)
    {
        var relevant = triggers.Where(t => t.Name == ActivityTypeName).ToList();

        foreach (var trigger in relevant)
        {
            var stimulus = trigger.GetPayload<MqttMessageReceivedStimulus>();

            if (_subscribers.TryGetValue(stimulus.ConnectionName, out var subscriber))
            {
                await subscriber.UnbindTriggersAsync([trigger.Id]);
            }
        }
    }

    /// <inheritdoc/>
    public async Task BindBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var relevant = bookmarks.Where(b => b.Name == ActivityTypeName).ToList();

        foreach (var bookmark in relevant)
        {
            var stimulus = bookmark.GetPayload<MqttMessageReceivedStimulus>();
            var subscriber = _subscribers.TryGetValue(stimulus.ConnectionName, out var existing)
                ? existing
                : await GetOrCreateSubscriberAsync(stimulus.ConnectionName, cancellationToken);

            if (subscriber == null)
            {
                continue;
            }

            await subscriber.BindBookmarkAsync(new MqttBookmarkBinding(
                WorkflowInstanceId: bookmark.WorkflowInstanceId,
                CorrelationId: bookmark.CorrelationId,
                BookmarkId: bookmark.Id,
                Stimulus: stimulus));
        }
    }

    /// <inheritdoc/>
    public async Task UnbindBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default)
    {
        var relevant = bookmarks.Where(b => b.Name == ActivityTypeName).ToList();

        foreach (var bookmark in relevant)
        {
            var stimulus = bookmark.GetPayload<MqttMessageReceivedStimulus>();

            if (_subscribers.TryGetValue(stimulus.ConnectionName, out var subscriber))
            {
                await subscriber.UnbindBookmarksAsync([bookmark.Id]);
            }
        }
    }

    /// <inheritdoc/>
    public void StopAll()
    {
        foreach (var subscriber in _subscribers.Values)
        {
            subscriber.Dispose();
        }

        _subscribers.Clear();
    }

    private async Task<MqttSubscriber?> GetOrCreateSubscriberAsync(string connectionName, CancellationToken cancellationToken)
    {
        if (_subscribers.TryGetValue(connectionName, out var existing))
        {
            return existing;
        }

        var mqttClient = await _mqttClientFactory.CreateClientAsync(connectionName, cancellationToken);

        var subscriberLogger = _loggerFactory.CreateLogger<MqttSubscriber>();
        var subscriber = new MqttSubscriber(
            connectionName: connectionName,
            mqttClient: mqttClient,
            scopeFactory: _scopeFactory,
            logger: subscriberLogger,
            maxReconnectAttempts: _options.MaxReconnectAttempts,
            reconnectBaseDelay: _options.ReconnectBaseDelay);

        _subscribers[connectionName] = subscriber;

        return subscriber;
    }
}
