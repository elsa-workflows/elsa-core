using Elsa.Copilot.Contracts;
using Elsa.Copilot.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace Elsa.Copilot.Implementations;

public class AgentTemplateLoader(Kernel kernel, IOptions<CopilotOptions> options, ILoggerFactory loggerFactory, ILogger<AgentTemplateLoader> logger) : IAgentTemplateLoader
{
    private readonly CopilotOptions _options = options.Value;

    public async Task<IEnumerable<KernelAgent>> LoadAgentsAsync(CancellationToken cancellationToken = default)
    {
        var agents = new List<KernelAgent>();

        var agentDirectory = _options.AgentTemplatesPath ?? Path.Combine(AppContext.BaseDirectory, "Agents");
        var templatePaths = Directory.Exists(agentDirectory) ? Directory.GetFiles(agentDirectory, "*.template.yaml") : [];

        foreach (var path in templatePaths)
        {
            try
            {
                var generateStoryYaml = await File.ReadAllTextAsync(path, cancellationToken);
                var templateConfig = KernelFunctionYaml.ToPromptTemplateConfig(generateStoryYaml);
                var agent = new ChatCompletionAgent(templateConfig, new KernelPromptTemplateFactory(loggerFactory))
                {
                    Kernel = kernel
                };
                agents.Add(agent);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to load agent template from {Path}", path);
            }
        }

        return agents;
    }
}