using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Kafka;

public record WorkerContext(IServiceScopeFactory ScopeFactory, ConsumerDefinition ConsumerDefinition);