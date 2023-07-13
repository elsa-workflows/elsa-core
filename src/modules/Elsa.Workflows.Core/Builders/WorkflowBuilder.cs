using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;
using Elsa.Workflows.Core.Models;

namespace Elsa.Workflows.Core.Builders;

/// <inheritdoc />
public class WorkflowBuilder : IWorkflowBuilder
{
    private readonly IActivityVisitor _activityVisitor;
    private readonly IIdentityGraphService _identityGraphService;
    private readonly IActivityRegistry _activityRegistry;

    /// <summary>
    /// Constructor.
    /// </summary>
    public WorkflowBuilder(IActivityVisitor activityVisitor, IIdentityGraphService identityGraphService, IActivityRegistry activityRegistry)
    {
        _activityVisitor = activityVisitor;
        _identityGraphService = identityGraphService;
        _activityRegistry = activityRegistry;
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
    public bool IsReadonly { get; set; }

    /// <inheritdoc />
    public IActivity? Root { get; set; }

    /// <inheritdoc />
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();

    /// <inheritdoc />
    public ICollection<InputDefinition> Inputs { get; set; } = new List<InputDefinition>();

    /// <inheritdoc />
    public ICollection<OutputDefinition> Outputs { get; set; } = new List<OutputDefinition>();

    /// <inheritdoc />
    public ICollection<string> Outcomes { get; set; } = new List<string>();

    /// <inheritdoc />
    public Variable? Result { get; set; }

    /// <inheritdoc />
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

    /// <inheritdoc />
    public WorkflowOptions WorkflowOptions { get; } = new();

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
        var variable = new Variable<T>
        {
            Name = name,
            Value = value
        };

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
    public async Task<Workflow> BuildWorkflowAsync(CancellationToken cancellationToken = default)
    {
        var definitionId = DefinitionId ?? Guid.NewGuid().ToString("N");
        var id = Id ?? Guid.NewGuid().ToString("N");
        var root = Root ?? new Sequence();
        var identity = new WorkflowIdentity(definitionId, Version, id);
        var publication = WorkflowPublication.LatestAndPublished;
        var workflowMetadata = new WorkflowMetadata(Name, Description);
        var workflow = new Workflow(identity, publication, workflowMetadata, WorkflowOptions, root, Variables, Inputs, Outputs, Outcomes, CustomProperties, IsReadonly);

        // If a Result variable is defined, install it into the workflow so we can capture the output into it.
        if (Result != null)
        {
            workflow.ResultVariable = Result;
            workflow.Result = new Output<object>(Result);
        }

        var graph = await _activityVisitor.VisitAsync(workflow, cancellationToken);
        var nodes = graph.Flatten().ToList();

        // Register all activity types first. The identity graph service will need to know about all activity types.
        var distinctActivityTypes = nodes.Select(x => x.Activity.GetType()).Distinct().ToList();
        await _activityRegistry.RegisterAsync(distinctActivityTypes, cancellationToken);

        // Assign identities to all activities.
        _identityGraphService.AssignIdentities(nodes);

        return workflow;
    }

    /// <inheritdoc />
    public async Task<Workflow> BuildWorkflowAsync(IWorkflow workflowDefinition, CancellationToken cancellationToken = default)
    {
        DefinitionId = workflowDefinition.GetType().Name;
        await workflowDefinition.BuildAsync(this, cancellationToken);
        return await BuildWorkflowAsync(cancellationToken);
    }
}