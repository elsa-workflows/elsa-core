// using Elsa.Workflows.Core.Contracts;
// using Elsa.Workflows.Core.Models;
// using Elsa.Workflows.Management.Models;
//
// namespace Elsa.Workflows.Api.Models;
//
// internal class WorkflowDefinitionResponse
// {
//     public WorkflowDefinitionResponse(
//         string id,
//         string definitionId,
//         string? name,
//         string? description,
//         DateTimeOffset createdAt,
//         int version,
//         ICollection<VariableDefinition> variables,
//         ICollection<InputDefinition> inputs,
//         ICollection<OutputDefinition> outputs,
//         ICollection<string> outcomes,
//         IDictionary<string, object> customProperties,
//         bool isLatest,
//         bool isPublished,
//         bool? usableAsActivity,
//         IActivity root,
//         WorkflowOptions? options)
//     {
//         Id = id;
//         DefinitionId = definitionId;
//         Name = name;
//         Description = description;
//         CreatedAt = createdAt;
//         Version = version;
//         Variables = variables;
//         Inputs = inputs;
//         Outputs = outputs;
//         Outcomes = outcomes;
//         CustomProperties = customProperties;
//         IsLatest = isLatest;
//         IsPublished = isPublished;
//         UsableAsActivity = usableAsActivity;
//         Root = root;
//         Options = options;
//     }
//
//     public string Id { get; }
//     public string DefinitionId { get; }
//     public string? Name { get; }
//     public string? Description { get; }
//     public DateTimeOffset CreatedAt { get; }
//     public int Version { get; }
//     public ICollection<VariableDefinition> Variables { get; }
//     public ICollection<InputDefinition> Inputs { get; }
//     public ICollection<OutputDefinition> Outputs { get; }
//     public ICollection<string> Outcomes { get; }
//     public IDictionary<string, object> CustomProperties { get; }
//     public bool IsLatest { get; }
//     public bool IsPublished { get; }
//     public bool? UsableAsActivity { get; }
//     public IActivity Root { get; }
//     public WorkflowOptions? Options { get; set; }
// }