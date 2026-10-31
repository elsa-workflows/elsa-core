using Elsa.Mediator.Contracts;

namespace Elsa.ServiceBus.Kafka.Notifications;

public record TransportMessageReceived(IWorker Worker, KafkaTransportMessage TransportMessage) : INotification;