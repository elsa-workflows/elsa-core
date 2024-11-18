using Elsa.Kafka.Implementations;
using Elsa.Mediator.Contracts;

namespace Elsa.Kafka.Notifications;

public record TransportMessageReceived(Worker Worker, KafkaTransportMessage TransportMessage) : INotification;