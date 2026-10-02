using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

#pragma warning disable SKEXP0001
#pragma warning disable SKEXP0010
#pragma warning disable SKEXP0110

namespace Elsa.Agents;

/// <inheritdoc />
public class AgentFactory(
    ISkillDiscoverer skillDiscoverer,
    IServiceProvider serviceProvider,
    IOptions<AgentsOptions> options,
    ILogger<AgentFactory> logger) : IAgentFactory
{
    /// <inheritdoc />
    public ChatCompletionAgent CreateAgent(AgentConfig agentConfig)
    {
        var kernel = CreateKernel(agentConfig);
        
        return new()
        {
            Name = agentConfig.Name,
            Description = agentConfig.Description,
            Instructions = agentConfig.PromptTemplate,
            Kernel = kernel
        };
    }

    /// <summary>
    /// Creates a Kernel configured for the specified agent.
    /// </summary>
    private Kernel CreateKernel(AgentConfig agentConfig)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
        builder.Services.AddSingleton(agentConfig);

        ApplyAgentConfig(builder, agentConfig);
        ApplyServiceDescriptors(builder, options.Value.ServiceDescriptors);

        return builder.Build();
    }

    private void ApplyServiceDescriptors(IKernelBuilder builder, ICollection<ServiceDescriptor> serviceDescriptors)
    {
        foreach (var descriptor in serviceDescriptors) 
            descriptor.ConfigureKernel(builder);
    }

    private void ApplyAgentConfig(IKernelBuilder builder, AgentConfig agentConfig)
    {
        AddSkills(builder, agentConfig);
    }
    
    private void AddSkills(IKernelBuilder builder, AgentConfig agent)
    {
        var skills = skillDiscoverer.DiscoverSkills().ToDictionary(x => x.Name);
        foreach (var skillName in agent.Skills)
        {
            if (!skills.TryGetValue(skillName, out var skillDescriptor))
            {
                logger.LogWarning($"Skill {skillName} not found");
                continue;
            }

            var clrType = skillDescriptor.ClrType;
            var skillInstance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, clrType);
            builder.Plugins.AddFromObject(skillInstance, skillName);
        }
    }
}
