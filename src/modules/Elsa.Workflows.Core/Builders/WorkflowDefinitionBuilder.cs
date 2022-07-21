using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Builders;

public class WorkflowDefinitionBuilder : IWorkflowDefinitionBuilder
{
    private readonly IIdentityGraphService _identityGraphService;

    public WorkflowDefinitionBuilder(IIdentityGraphService identityGraphService)
    {
        _identityGraphService = identityGraphService;
    }
    
    public string? Id { get; private set; }
    public string? DefinitionId { get; private set; }
    public int Version { get; private set; } = 1;
    public IActivity? Root { get; private set; }
    public ICollection<Variable> Variables { get; set; } = new List<Variable>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    public IDictionary<string, object> ApplicationProperties { get; set; } = new Dictionary<string, object>();

    public IWorkflowDefinitionBuilder WithId(string id)
    {
        Id = id;
        return this;
    }

    public IWorkflowDefinitionBuilder WithDefinitionId(string definitionId)
    {
        DefinitionId = definitionId;
        return this;
    }

    public IWorkflowDefinitionBuilder WithVersion(int version)
    {
        Version = version;
        return this;
    }

    public IWorkflowDefinitionBuilder WithRoot(IActivity root)
    {
        Root = root;
        return this;
    }

    public Variable<T> WithVariable<T>(string? storageDriverId = default)
    {
        var variable = new Variable<T>()
        {
            StorageDriverId = storageDriverId
        };
        Variables.Add(variable);
        return variable;
    }

    public Variable<T> WithVariable<T>(string name, T value, string? storageDriverId = default)
    {
        var variable = value != null ? new Variable<T>(name, value) : new Variable<T>(name);
        variable.StorageDriverId = storageDriverId;
        Variables.Add(variable);
        return variable;
    }

    public Variable<T> WithVariable<T>(T value, string? storageDriverId = default)
    {
        var variable = value != null ? new Variable<T>(value) : new Variable<T>();
        variable.StorageDriverId = storageDriverId;
        Variables.Add(variable);
        return variable;
    }

    public IWorkflowDefinitionBuilder WithVariable(Variable variable)
    {
        Variables.Add(variable);
        return this;
    }

    public IWorkflowDefinitionBuilder WithVariables(params Variable[] variables)
    {
        foreach (var variable in variables) Variables.Add(variable);
        return this;
    }

    public IWorkflowDefinitionBuilder WithMetadata(string name, object value)
    {
        Metadata[name] = value;
        return this;
    }

    public IWorkflowDefinitionBuilder WithApplicationProperty(string name, object value)
    {
        ApplicationProperties[name] = value;
        return this;
    }

    public Workflow BuildWorkflow()
    {
        var definitionId = DefinitionId ?? Guid.NewGuid().ToString("N");
        var id = Id ?? Guid.NewGuid().ToString("N");
        var root = Root ?? new Sequence();
        var identity = new WorkflowIdentity(definitionId, Version, id);
        var publication = WorkflowPublication.LatestAndPublished;
        var metadata = new WorkflowMetadata();
        var workflow =new Workflow(identity, publication, metadata, root, Variables, Metadata, ApplicationProperties);

        _identityGraphService.AssignIdentitiesAsync(workflow);
        
        return workflow;
    }

    public async Task<Workflow> BuildWorkflowAsync(IWorkflow workflowDefinition, CancellationToken cancellationToken = default)
    {
        WithDefinitionId(workflowDefinition.GetType().Name);
        await workflowDefinition.BuildAsync(this, cancellationToken);
        return BuildWorkflow();
    }
}