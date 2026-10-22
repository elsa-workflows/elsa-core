using System.Text.Json;
using Elsa.Workflows.Management.Models;

namespace Elsa.Workflows.Management.UnitTests.Services;

internal static class CompensationDraftTestData
{
    public static string GetDataString(WorkflowValidationError error, string propertyName)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(error.Data));
        return document.RootElement.GetProperty(propertyName).GetString()!;
    }

    public static string[] GetDataArray(WorkflowValidationError error, string propertyName)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(error.Data));
        return document.RootElement.GetProperty(propertyName).EnumerateArray().Select(item => item.GetString()!).ToArray();
    }
}
