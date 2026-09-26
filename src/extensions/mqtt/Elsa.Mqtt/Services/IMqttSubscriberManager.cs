using Elsa.Workflows.Runtime.Entities;

namespace Elsa.Mqtt.Services;

/// <summary>
/// Manages <see cref="MqttSubscriber"/> instances and keeps their topic subscriptions
/// in sync with the registered workflow triggers and bookmarks.
/// </summary>
internal interface IMqttSubscriberManager
{
    /// <summary>
    /// Reads all stored triggers and bookmarks for <c>MqttMessageReceived</c> and
    /// binds them to the appropriate subscribers, creating subscriber connections as needed.
    /// </summary>
    public Task UpdateSubscriptionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Binds a set of new or updated triggers to their subscribers.
    /// </summary>
    public Task BindTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a set of triggers from their subscribers.
    /// </summary>
    public Task UnbindTriggersAsync(IEnumerable<StoredTrigger> triggers, CancellationToken cancellationToken = default);

    /// <summary>
    /// Binds a set of new or updated bookmarks to their subscribers.
    /// </summary>
    public Task BindBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a set of bookmarks from their subscribers.
    /// </summary>
    public Task UnbindBookmarksAsync(IEnumerable<StoredBookmark> bookmarks, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops all subscriber connections and clears internal state.
    /// </summary>
    public void StopAll();
}
