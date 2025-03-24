using Elsa.Common;
using Elsa.Copilot.Contracts;
using Elsa.Copilot.Features;

namespace Elsa.Copilot.Tasks;

public class LoadAgentsStartupTask(IAgentTemplateLoader loader, IAgentRegistry registry) : IStartupTask
{
    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var agents = await loader.LoadAgentsAsync(cancellationToken);
        foreach (var agent in agents)
            registry.Register(agent);
    }
}