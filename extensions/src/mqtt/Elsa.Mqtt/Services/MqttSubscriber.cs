using System.Collections.Concurrent;
using Elsa.Mediator.Contracts;
using Elsa.Mqtt.Models;
using Elsa.Mqtt.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Protocol;

namespace Elsa.Mqtt.Services;

/// <summary>
/// Manages the MQTT subscriber client for a single named connection.
/// Maintains bindings between workflow triggers/bookmarks and MQTT topic filters,
/// and dispatches a <see cref="MqttMessageReceivedNotification"/> for each received message.
/// </summary>
public class MqttSubscriber : IDisposable
{
    private readonly string _connectionName;
    private readonly IMqttClient _mqttClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MqttSubscriber> _logger;
    private readonly SemaphoreSlim _subscriptionSemaphore = new(1, 1);
    private readonly ConcurrentDictionary<string, MqttTriggerBinding> _triggerBindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, MqttBookmarkBinding> _bookmarkBindings = new(StringComparer.OrdinalIgnoreCase);
    private readonly int _maxReconnectAttempts;
    private readonly TimeSpan _reconnectBaseDelay;
    private HashSet<string> _subscribedTopics = [];
    private int _reconnectAttempts;
    private bool _disposed;

    public MqttSubscriber(
        string connectionName,
        IMqttClient mqttClient,
        IServiceScopeFactory scopeFactory,
        ILogger<MqttSubscriber> logger,
        int maxReconnectAttempts,
        TimeSpan reconnectBaseDelay)
    {
        _connectionName = connectionName;
        _mqttClient = mqttClient;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _maxReconnectAttempts = maxReconnectAttempts;
        _reconnectBaseDelay = reconnectBaseDelay;

        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceivedAsync;
        _mqttClient.DisconnectedAsync += OnDisconnectedAsync;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _mqttClient.ApplicationMessageReceivedAsync -= OnApplicationMessageReceivedAsync;
                _mqttClient.DisconnectedAsync -= OnDisconnectedAsync;
                _mqttClient.Dispose();
            }

            _disposed = true;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The name of the MQTT connection this subscriber is bound to.
    /// </summary>
    public string ConnectionName => _connectionName;

    /// <summary>
    /// Active trigger bindings, keyed by trigger ID.
    /// </summary>
    public IDictionary<string, MqttTriggerBinding> TriggerBindings => _triggerBindings;

    /// <summary>
    /// Active bookmark bindings, keyed by bookmark ID.
    /// </summary>
    public IDictionary<string, MqttBookmarkBinding> BookmarkBindings => _bookmarkBindings;

    /// <summary>
    /// Binds a trigger and subscribes to any new topics it requires.
    /// </summary>
    public async Task BindTriggerAsync(MqttTriggerBinding binding)
    {
        _triggerBindings[binding.TriggerId] = binding;

        await UpdateTopicSubscriptionsAsync();
    }

    /// <summary>
    /// Removes matching triggers and unsubscribes from topics no longer needed.
    /// </summary>
    public async Task UnbindTriggersAsync(IEnumerable<string> triggerIds)
    {
        foreach (var id in triggerIds)
        {
            _triggerBindings.TryRemove(id, out _);
        }

        await UpdateTopicSubscriptionsAsync();
    }

    /// <summary>
    /// Binds a bookmark and subscribes to any new topics it requires.
    /// </summary>
    public async Task BindBookmarkAsync(MqttBookmarkBinding binding)
    {
        _bookmarkBindings[binding.BookmarkId] = binding;

        await UpdateTopicSubscriptionsAsync();
    }

    /// <summary>
    /// Removes matching bookmarks and unsubscribes from topics no longer needed.
    /// </summary>
    public async Task UnbindBookmarksAsync(IEnumerable<string> bookmarkIds)
    {
        foreach (var id in bookmarkIds)
        {
            _bookmarkBindings.TryRemove(id, out _);
        }

        await UpdateTopicSubscriptionsAsync();
    }

    private async Task UpdateTopicSubscriptionsAsync(bool resubscribeAll = false)
    {
        await _subscriptionSemaphore.WaitAsync();
        try
        {
            if (resubscribeAll)
            {
                _subscribedTopics = [];
            }

            var requiredTopics = _triggerBindings.Values
                .SelectMany(b => b.Stimulus.Topics)
                .Concat(_bookmarkBindings.Values.SelectMany(b => b.Stimulus.Topics))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var toSubscribe = requiredTopics.Except(_subscribedTopics, StringComparer.OrdinalIgnoreCase).ToList();
            var toUnsubscribe = _subscribedTopics.Except(requiredTopics, StringComparer.OrdinalIgnoreCase).ToList();

            if (toSubscribe.Count > 0)
            {
                var builder = new MqttClientSubscribeOptionsBuilder();

                foreach (var topic in toSubscribe)
                {
                    builder.WithTopicFilter(topic, MqttQualityOfServiceLevel.AtLeastOnce);
                }

                await _mqttClient.SubscribeAsync(builder.Build());

                _logger.LogInformation("Subscribed to MQTT topics on connection '{Connection}': {Topics}",
                    _connectionName, string.Join(", ", toSubscribe));
            }

            if (toUnsubscribe.Count > 0)
            {
                var builder = new MqttClientUnsubscribeOptionsBuilder();

                foreach (var topic in toUnsubscribe)
                {
                    builder.WithTopicFilter(topic);
                }

                await _mqttClient.UnsubscribeAsync(builder.Build());

                _logger.LogInformation("Unsubscribed from MQTT topics on connection '{Connection}': {Topics}",
                    _connectionName, string.Join(", ", toUnsubscribe));
            }

            _subscribedTopics = requiredTopics;
        }
        finally
        {
            _subscriptionSemaphore.Release();
        }
    }

    private async Task OnDisconnectedAsync(MqttClientDisconnectedEventArgs args)
    {
        if (!args.ClientWasConnected || _disposed)
        {
            return;
        }

        while (true)
        {
            var attempt = Interlocked.Increment(ref _reconnectAttempts);

            if (_maxReconnectAttempts > 0 && attempt > _maxReconnectAttempts)
            {
                _logger.LogError(
                    "MQTT connection '{Connection}' could not be re-established after {MaxAttempts} attempt(s). Giving up.",
                    _connectionName, _maxReconnectAttempts);
                return;
            }

            // Exponential back-off: base * 2^(attempt-1), capped at 5 minutes.
            var delaySeconds = Math.Min(_reconnectBaseDelay.TotalSeconds * Math.Pow(2, attempt - 1), 300);
            _logger.LogWarning(
                "MQTT connection '{Connection}' was lost (attempt {Attempt}). Reconnecting in {Delay:F0}s.",
                _connectionName, attempt, delaySeconds);

            await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

            if (_disposed)
            {
                return;
            }

            try
            {
                await _mqttClient.ReconnectAsync();

                Interlocked.Exchange(ref _reconnectAttempts, 0);
                _logger.LogInformation(
                    "Reconnected to MQTT broker on connection '{Connection}'. Restoring topic subscriptions.",
                    _connectionName);

                await UpdateTopicSubscriptionsAsync(resubscribeAll: true);

                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to reconnect to MQTT broker on connection '{Connection}' (attempt {Attempt}).",
                    _connectionName, attempt);
            }
        }
    }

    private async Task OnApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
    {
        var topic = args.ApplicationMessage.Topic;
        var payload = args.ApplicationMessage.ConvertPayloadToString() ?? string.Empty;
        var transportMessage = new MqttMessage(topic, payload);
        var notification = new MqttMessageReceivedNotification(this, transportMessage);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.SendAsync(notification);
    }
}
