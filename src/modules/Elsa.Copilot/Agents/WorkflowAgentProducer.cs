using Elsa.Copilot.Notifications;
using Elsa.Mediator.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace Elsa.Copilot.Contracts;

public class WorkflowAgentProducer(IMediator mediator, ILoggerFactory loggerFactory) : IAgentProducer
{
    public string Name => "Workflow";

    public async Task<Agent> ProduceAsync(CancellationToken cancellationToken = default)
    {
        var kernelBuilder = Kernel.CreateBuilder();
        var notification = new CreatingAgentKernel(this, kernelBuilder);
        await mediator.SendAsync(notification, cancellationToken);
        var kernel = kernelBuilder.Build();
    
        var templateConfig = new PromptTemplateConfig(yaml);
        var agent = new ChatCompletionAgent(templateConfig, new KernelPromptTemplateFactory(loggerFactory))
        {
            Kernel = kernel,
            LoggerFactory = loggerFactory
        };

        return agent;
    }
}