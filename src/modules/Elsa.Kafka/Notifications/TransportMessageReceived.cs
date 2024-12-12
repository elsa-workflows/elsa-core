using Elsa.Mediator.Contracts;

namespace Elsa.Kafka.Notifications;

public record TransportMessageReceived(IWorker Worker, KafkaTransportMessage TransportMessage) : INotification;