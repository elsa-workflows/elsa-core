using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

#pragma warning disable SKEXP0001

#pragma warning disable SKEXP0010

namespace Elsa.Agents;

public class KernelFactory(IPluginsDiscoverer pluginsDiscoverer, ILoggerFactory loggerFactory, IServiceProvider serviceProvider, ILogger<KernelFactory> logger)
{
    public Kernel CreateKernel(KernelConfig kernelConfig, string agentName)
    {
        var agent = kernelConfig.Agents[agentName];
        return CreateKernel(kernelConfig, agent);
    }

    public Kernel CreateKernel(KernelConfig kernelConfig, AgentConfig agentConfig)
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
        builder.Services.AddSingleton(agentConfig);

        ApplyAgentConfig(builder, kernelConfig, agentConfig);

        return builder.Build();
    }

    private void ApplyAgentConfig(IKernelBuilder builder, KernelConfig kernelConfig, AgentConfig agentConfig)
    {
        foreach (string serviceName in agentConfig.Services)
        {
            if (!kernelConfig.Services.TryGetValue(serviceName, out var serviceConfig))
            {
                logger.LogWarning($"Service {serviceName} not found");
                continue;
            }

            AddService(builder, kernelConfig, serviceConfig);
        }

        AddPlugins(builder, agentConfig);
        AddAgents(builder, kernelConfig, agentConfig);
    }

    private void AddService(IKernelBuilder builder, KernelConfig kernelConfig, ServiceConfig serviceConfig)
    {
        switch (serviceConfig.Type)
        {
            case "OpenAIChatCompletion":
                {
                    var modelId = (string)serviceConfig.Settings["ModelId"];
                    var apiKey = GetApiKey(kernelConfig, serviceConfig);
                    builder.AddOpenAIChatCompletion(modelId, apiKey);
                    break;
                }
            case "OpenAITextToImage":
                {
                    var modelId = (string)serviceConfig.Settings["ModelId"];
                    var apiKey = GetApiKey(kernelConfig, serviceConfig);
                    builder.AddOpenAITextToImage(apiKey, modelId: modelId);
                    break;
                }
            default:
                logger.LogWarning($"Unknown service type {serviceConfig.Type}");
                break;
        }
    }

    private string GetApiKey(KernelConfig kernelConfig, ServiceConfig service)
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
        var plugins = pluginsDiscoverer.GetPluginDescriptors().ToDictionary(x => x.Name);
        foreach (var pluginName in agent.Plugins)
        {
            if (!plugins.TryGetValue(pluginName, out var pluginDescriptor))
            {
                logger.LogWarning($"Plugin {pluginName} not found");
                continue;
            }

            var pluginType = pluginDescriptor.PluginType;
            var pluginInstance = ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, pluginType);
            builder.Plugins.AddFromObject(pluginInstance, pluginName);
        }
    }

    private void AddAgents(IKernelBuilder builder, KernelConfig kernelConfig, AgentConfig agent)
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