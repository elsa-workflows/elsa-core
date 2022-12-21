using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

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
    }

    /// <inheritdoc />
    public string? Id { get; set; }

    /// <inheritdoc />
    public string? DefinitionId { get; set; }

    /// <inheritdoc />
    public int Version { get; private set; } = 1;

    /// <inheritdoc />
    public string? Name { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public IActivity? Root { get; set; }

    /// <inheritdoc />
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();

    /// <inheritdoc />
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Stores <see cref="WorkflowOptions"/> to be applied to the workflow being created.
    /// </summary>
    public WorkflowOptions WorkflowOptions { get; } = new(); 

    /// <inheritdoc />
    public IWorkflowBuilder WithId(string id)
    {
        Id = id;
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithDefinitionId(string definitionId)
    {
        DefinitionId = definitionId;
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithVersion(int version)
    {
        Version = version;
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithRoot(IActivity root)
    {
        Root = root;
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithName(string value)
    {
        Name = value;
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithDescription(string value)
    {
        Description = value;
        return this;
    }

    /// <inheritdoc />
    public Variable<T> WithVariable<T>(string? storageDriverId = default)
    {
        var variable = new Variable<T>()
        {
            StorageDriverId = storageDriverId
        };
        Variables.Add(variable);
        return variable;
    }

    /// <inheritdoc />
    public Variable<T> WithVariable<T>(string name, T value, string? storageDriverId = default)
    {
        var variable = value != null ? new Variable<T>(name, value) : new Variable<T>(name);
        variable.StorageDriverId = storageDriverId;
        Variables.Add(variable);
        return variable;
    }

    /// <inheritdoc />
    public Variable<T> WithVariable<T>(T value, string? storageDriverId = default)
    {
        var variable = value != null ? new Variable<T>(value) : new Variable<T>();
        variable.StorageDriverId = storageDriverId;
        Variables.Add(variable);
        return variable;
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

    public IWorkflowBuilder ConfigureOptions(Action<WorkflowOptions> configure)
    {
        configure(WorkflowOptions);
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithCustomProperty(string name, object value)
    {
        CustomProperties[name] = value;
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

        _identityGraphService.AssignIdentitiesAsync(workflow);

        return workflow;
    }

    /// <inheritdoc />
    public async Task<Workflow> BuildWorkflowAsync(IWorkflow workflowDefinition, CancellationToken cancellationToken = default)
    {
        WithDefinitionId(workflowDefinition.GetType().Name);
        await workflowDefinition.BuildAsync(this, cancellationToken);
        return BuildWorkflow();
    }
}