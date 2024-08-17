using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
#pragma warning disable SKEXP0001

#pragma warning disable SKEXP0010

namespace Elsa.Agents;

public class KernelFactory(KernelConfig kernelConfig, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, ILogger<KernelFactory> logger)
{
    public Kernel CreateKernel(string agentName)
    {
        var agent = kernelConfig.Agents[agentName];
        return CreateKernel(agent);
    }

    public Kernel CreateKernel(AgentConfig agentConfig)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
        builder.Services.AddSingleton(kernelConfig);
        builder.Services.AddSingleton(agentConfig);

        ApplyAgentConfig(builder, agentConfig);

        return builder.Build();
    }

    private void ApplyAgentConfig(IKernelBuilder builder, AgentConfig agentConfig)
    {
        foreach (string serviceName in agentConfig.Services)
        {
            if (!kernelConfig.Services.TryGetValue(serviceName, out var serviceConfig))
            {
                logger.LogWarning($"Service {serviceName} not found");
                continue;
            }

            AddService(builder, serviceConfig);
        }
        
        AddPlugins(builder, agentConfig);
        AddAgents(builder, agentConfig);
    }
    
    private void AddService(IKernelBuilder builder, ServiceConfig serviceConfig)
    {
        switch (serviceConfig.Type)
        {
            case "OpenAIChatCompletion":
                {
                    var modelId = (string)serviceConfig.Settings["ModelId"];
                    var apiKey = GetApiKey(serviceConfig);
                    builder.AddOpenAIChatCompletion(modelId, apiKey);
                    break;
                }
            case "OpenAITextToImage":
                {
                    var modelId = (string)serviceConfig.Settings["ModelId"];
                    var apiKey = GetApiKey(serviceConfig);
                    builder.AddOpenAITextToImage(apiKey, modelId: modelId);
                    break;
                }
            default:
                logger.LogWarning($"Unknown service type {serviceConfig.Type}");
                break;
        }
    }

    private string GetApiKey(ServiceConfig service)
    {
        var settings = service.Settings;
        if (settings.TryGetValue("ApiKey", out var apiKey))
            return (string)apiKey;

        if (settings.TryGetValue("ApiKeyRef", out var apiKeyRef))
            return kernelConfig.ApiKeys[(string)apiKeyRef].Value;

        throw new KeyNotFoundException($"No api key found for service {service.Type}");
    }

    private void AddPlugins(IKernelBuilder builder, AgentConfig agent)
    {
        foreach (var pluginName in agent.Plugins)
        {
            if (!kernelConfig.Plugins.TryGetValue(pluginName, out var plugin))
            {
                logger.LogWarning($"Plugin {pluginName} not found");
                continue;
            }

            var pluginTypeName = plugin.Type;
            var pluginType = Type.GetType(pluginTypeName);

            if (pluginType == null)
            {
                logger.LogWarning($"Plugin type {pluginType} not found");
                continue;
            }

            var pluginInstance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, pluginType);
            builder.Plugins.AddFromObject(pluginInstance, pluginName);
        }
    }

    private void AddAgents(IKernelBuilder builder, AgentConfig agent)
    {
        foreach (var agentName in agent.Agents)
        {
            if (!kernelConfig.Agents.TryGetValue(agentName, out var subAgent))
            { 
                logger.LogWarning($"Agent {agentName} not found");
                continue;
            }
            
            var promptExecutionSettings = subAgent.ToOpenAIPromptExecutionSettings();
            var promptExecutionSettingsDictionary = new Dictionary<string, PromptExecutionSettings>
            {
                [PromptExecutionSettings.DefaultServiceId] = promptExecutionSettings,
            };
            var promptTemplateConfig = new PromptTemplateConfig
            {
                Name = subAgent.FunctionName,
                Description = subAgent.Description,
                Template = subAgent.PromptTemplate,
                ExecutionSettings = promptExecutionSettingsDictionary,
                AllowDangerouslySetContent = true,
                InputVariables = subAgent.InputVariables.Select(x => new InputVariable
                {
                    Name = x.Name,
                    Description = x.Description,
                    IsRequired = true,
                    AllowDangerouslySetContent = true
                }).ToList()
            };
            
            var subAgentFunction = KernelFunctionFactory.CreateFromPrompt(promptTemplateConfig, loggerFactory: loggerFactory);
            var agentPlugin = KernelPluginFactory.CreateFromFunctions(subAgent.Name, subAgent.Description, [subAgentFunction]);
            builder.Plugins.Add(agentPlugin);
        }
    }
    
    
}