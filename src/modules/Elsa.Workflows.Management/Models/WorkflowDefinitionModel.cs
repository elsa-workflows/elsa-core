using Elsa.Workflows.Models;
using JetBrains.Annotations;

namespace Elsa.Workflows.Management.Models;

/// <summary>
/// Represents a serializable workflow definition.
/// </summary>
[PublicAPI]
public class WorkflowDefinitionModel
{
    /// <summary>
    /// Represents a serializable workflow definition.
    /// </summary>
    public WorkflowDefinitionModel() { }

    /// <summary>
    /// Represents a serializable workflow definition.
    /// </summary>
    public WorkflowDefinitionModel(string id,
        string definitionId,
        string? tenantId,
        string? name,
        string? description,
        DateTimeOffset createdAt,
        int version,
        Version? toolVersion,
        ICollection<VariableDefinition>? variables,
        ICollection<InputDefinition>? inputs,
        ICollection<OutputDefinition>? outputs,
        ICollection<string>? outcomes,
        IDictionary<string, object>? customProperties,
        bool isReadonly,
        bool isSystem,
        bool isLatest,
        bool isPublished,
        WorkflowOptions? options,
        bool? usableAsActivity,
        IActivity? root)
    {
        Id = id;
        DefinitionId = definitionId;
        TenantId = tenantId;
        Name = name;
        Description = description;
        CreatedAt = createdAt;
        Version = version;
        ToolVersion = toolVersion;
        Variables = variables;
        Inputs = inputs;
        Outputs = outputs;
        Outcomes = outcomes;
        CustomProperties = customProperties;
        IsReadonly = isReadonly;
        IsSystem = isSystem;
        IsLatest = isLatest;
        IsPublished = isPublished;
        Options = options;
        UsableAsActivity = usableAsActivity;
        Root = root;
    }

    public string Id { get; set; }
    public string DefinitionId { get; set; }
    public string? TenantId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public int Version { get; set; }
    public Version? ToolVersion { get; set; }
    public ICollection<VariableDefinition>? Variables { get; set; }
    public ICollection<InputDefinition>? Inputs { get; set; }
    public ICollection<OutputDefinition>? Outputs { get; set; }
    public ICollection<string>? Outcomes { get; set; }
    public IDictionary<string, object>? CustomProperties { get; set; }
    public bool IsReadonly { get; set; }
    public bool IsSystem { get; set; }
    public bool IsLatest { get; set; }
    public bool IsPublished { get; set; }
    public WorkflowOptions? Options { get; set; }

    [Obsolete("Use Options.UsableAsActivity instead")]
    public bool? UsableAsActivity { get; set; }

    public IActivity? Root { get; set; }
}