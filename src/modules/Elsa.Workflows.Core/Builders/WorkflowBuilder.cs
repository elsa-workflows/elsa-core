using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Builders;

/// <inheritdoc />
public class WorkflowBuilder : IWorkflowBuilder
{
    private readonly IIdentityGraphService _identityGraphService;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowBuilder(IIdentityGraphService identityGraphService)
    {
        _identityGraphService = identityGraphService;
        Result = new Variable();
    }

    /// <inheritdoc />
    public string? Id { get; set; }

    /// <inheritdoc />
    public string? DefinitionId { get; set; }

    /// <inheritdoc />
    public int Version { get; set; } = 1;

    /// <inheritdoc />
    public string? Name { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IActivity? Root { get; set; }

    /// <inheritdoc />
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();

    /// <inheritdoc />
    public Variable? Result { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

    /// <inheritdoc />
    public WorkflowOptions WorkflowOptions { get;  } = new(); 
    
    /// <inheritdoc />
    public Variable<T> WithVariable<T>()
    {
        var variable = new Variable<T>();
        Variables.Add(variable);
        return variable;
    }

    /// <inheritdoc />
    public Variable<T> WithVariable<T>(string name, T value)
    {
        var variable = value != null ? new Variable<T>(name, value) : new Variable<T>(name);
        Variables.Add(variable);
        return variable;
    }

    /// <inheritdoc />
    public Variable<T> WithVariable<T>(T value)
    {
        var variable = value != null ? new Variable<T>(value) : new Variable<T>();
        Variables.Add(variable);
        return variable;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithVariable<T>(Variable<T> variable)
    {
        Variables.Add(variable);
        return this;
    }
    
    /// <inheritdoc />
    public IWorkflowBuilder WithVariable(Variable variable)
    {
        Variables.Add(variable);
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithVariables(params Variable[] variables)
    {
        foreach (var variable in variables) Variables.Add(variable);
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithCustomProperty(string name, object value)
    {
        CustomProperties[name] = value;
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithActivationStrategyType<T>() where T : IWorkflowActivationStrategy
    {
        WorkflowOptions.ActivationStrategyType = typeof(T);
        return this;
    }

    /// <inheritdoc />
    public Workflow BuildWorkflow()
    {
        var definitionId = DefinitionId ?? Guid.NewGuid().ToString("N");
        var id = Id ?? Guid.NewGuid().ToString("N");
        var root = Root ?? new Sequence();
        var identity = new WorkflowIdentity(definitionId, Version, id);
        var publication = WorkflowPublication.LatestAndPublished;
        var workflowMetadata = new WorkflowMetadata(Name, Description);
        var workflow = new Workflow(identity, publication, workflowMetadata, WorkflowOptions, root, Variables, CustomProperties);

        // IF a Result variable is defined, install it into the workflow so we can capture the output into it.
        if (Result != null)
        {
            workflow.ResultVariable = Result;
            workflow.Result = new Output(Result);
        }

        _identityGraphService.AssignIdentitiesAsync(workflow);

        return workflow;
    }

    /// <inheritdoc />
    public async Task<Workflow> BuildWorkflowAsync(IWorkflow workflowDefinition, CancellationToken cancellationToken = default)
    {
        DefinitionId = workflowDefinition.GetType().Name;
        await workflowDefinition.BuildAsync(this, cancellationToken);
        return BuildWorkflow();
    }
}