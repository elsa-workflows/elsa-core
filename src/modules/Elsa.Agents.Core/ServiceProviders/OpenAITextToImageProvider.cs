using Microsoft.SemanticKernel;
#pragma warning disable SKEXP0010

namespace Elsa.Agents;

public class OpenAITextToImageProvider : IAgentServiceProvider
{
    public string Name => "OpenAITextToImage";

    public void ConfigureKernel(KernelBuilderContext context)
    {
        var modelId = (string)context.ServiceConfig.Settings["ModelId"];
        var apiKey = context.GetApiKey();
        context.KernelBuilder.AddOpenAITextToImage(apiKey, modelId: modelId);
    }
}