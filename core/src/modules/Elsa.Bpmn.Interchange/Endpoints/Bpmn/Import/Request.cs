using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Import;

/// <summary>The form fields accompanying the uploaded <c>.bpmn</c> file.</summary>
[PublicAPI]
public sealed class Request
{
    /// <summary>The workflow definition to update, or empty to import as a new one.</summary>
    public string? DefinitionId { get; set; }

    /// <summary>The workflow definition's display name, defaulting to the process's own BPMN name or id.</summary>
    public string? Name { get; set; }

    /// <summary>The process to import when the document declares more than one.</summary>
    public string? ProcessId { get; set; }
}
