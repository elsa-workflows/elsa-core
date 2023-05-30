using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Management.Models;

public class WorkflowDefinitionState
{
    public WorkflowDefinitionState()
    {
    }

    public WorkflowDefinitionState(
        WorkflowOptions? options,
        ICollection<Variable> variables,
        ICollection<InputDefinition> inputs,
        ICollection<OutputDefinition> outputs,
        ICollection<string> outcomes,
        IDictionary<string, object> customProperties
    )
    {
        Options = options;
        Variables = variables;
        Inputs = inputs;
        Outputs = outputs;
        Outcomes = outcomes;
        CustomProperties = customProperties;
    }

    public WorkflowOptions? Options { get; set; }
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
    public ICollection<InputDefinition> Inputs { get; set; } = new List<InputDefinition>();
    public ICollection<OutputDefinition> Outputs { get; set; } = new List<OutputDefinition>();
    public ICollection<string> Outcomes { get; set; } = new List<string>();
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();
}