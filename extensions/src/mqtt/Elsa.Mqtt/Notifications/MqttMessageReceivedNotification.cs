using Elsa.Mediator.Contracts;
using Elsa.Mqtt.Models;
using Elsa.Mqtt.Services;

namespace Elsa.Mqtt.Notifications;

/// <summary>
/// Published when an MQTT message is received on a subscribed topic.
/// </summary>
public record MqttMessageReceivedNotification(MqttSubscriber Subscriber, MqttMessage TransportMessage) : INotification;
