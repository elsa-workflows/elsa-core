using JetBrains.Annotations;

namespace Elsa.Bpmn.Interchange.Endpoints.Bpmn.Export;

/// <summary>Identifies the workflow definition to export as BPMN 2.0 XML.</summary>
[PublicAPI]
public sealed class Request
{
    /// <summary>The workflow definition id to export.</summary>
    public string DefinitionId { get; set; } = string.Empty;

    /// <summary>The version to export, e.g. <c>Latest</c> or <c>Published</c>. Defaults to <c>Latest</c>.</summary>
    public string? VersionOptions { get; set; }
}
