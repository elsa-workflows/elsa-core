using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Definitions.Post;

/// <summary>The new BPMN workflow to create.</summary>
[PublicAPI]
public sealed class Request
{
    /// <summary>The workflow definition's name, which its process also takes. Required.</summary>
    public string? Name { get; set; }
}
