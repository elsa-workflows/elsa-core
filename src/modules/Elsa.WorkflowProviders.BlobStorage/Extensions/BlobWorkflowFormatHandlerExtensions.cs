using Elsa.WorkflowProviders.BlobStorage.Contracts;

namespace Elsa.Extensions;

public static class BlobWorkflowFormatHandlerExtensions
{
    public static bool SupportsExtension(this IBlobWorkflowFormatHandler handler, string extension) => handler.SupportedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
}