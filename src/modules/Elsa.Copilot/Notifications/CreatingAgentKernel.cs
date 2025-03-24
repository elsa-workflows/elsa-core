using Elsa.Copilot.Contracts;
using Elsa.Mediator.Contracts;
using Microsoft.SemanticKernel;

namespace Elsa.Copilot.Notifications;

public record CreatingAgentKernel(IAgentProducer Producer, IKernelBuilder KernelBuilder) : INotification;