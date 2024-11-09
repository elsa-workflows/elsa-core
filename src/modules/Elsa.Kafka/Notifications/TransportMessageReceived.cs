using Elsa.Mediator.Contracts;

namespace Elsa.Kafka.Notifications;

public record TransportMessageReceived(Consumer Consumer, KafkaTransportMessage TransportMessage) : INotification;