using Microsoft.AspNetCore.Http;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn;

/// <summary>Reads an uploaded <c>.bpmn</c> file as text, shared by every endpoint that accepts one.</summary>
internal static class BpmnUploadedFileReader
{
    public static async Task<string> ReadTextAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var streamReader = new StreamReader(stream);
        return await streamReader.ReadToEndAsync(cancellationToken);
    }
}
