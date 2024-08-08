using System.ComponentModel;
using JetBrains.Annotations;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TextToImage;

#pragma warning disable SKEXP0001

namespace Elsa.Agents.Plugins;

[Description("Generates an image from text")]
[UsedImplicitly]
public class ImageGenerator
{
    [KernelFunction("generate_image_from_text")]
    [Description("Generates an image from text")]
    [return: Description("The URL to the generated image")]
    public async Task<string> GenerateImage(
        Kernel kernel,
        [Description("The text to generate an image from")]
        string description,
        [Description("The width of the image to generate. When not specified, a default size will be used.")]
        int width = 1024,
        [Description("The height of the image to generate. When not specified, a default size will be used.")]
        int height = 1024)
    {
        var dallE = kernel.GetRequiredService<ITextToImageService>();
        var imageUrl = await dallE.GenerateImageAsync(description.Trim(), width, height);
        return imageUrl;
    }
}