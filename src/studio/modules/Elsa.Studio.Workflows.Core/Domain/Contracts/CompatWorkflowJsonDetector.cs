using Elsa.Studio.Workflows.Domain.Services;

namespace Elsa.Studio.Workflows.Domain.Contracts;

/// <summary>
/// A service that detects whether a JSON string is a workflow definition,
/// first using a schema, then using a simple heuristic.
/// </summary>
public class CompatWorkflowJsonDetector : IWorkflowJsonDetector
{
    /// <inheritdoc />
    public bool IsWorkflowSchema(string json)
    {
        var schemaDetector = new SchemaWorkflowJsonDetector();
        
        if (schemaDetector.IsWorkflowSchema(json))
            return true;
        
        var simpleDetector = new SimpleWorkflowJsonDetector();
        return simpleDetector.IsWorkflowSchema(json);
    }
}