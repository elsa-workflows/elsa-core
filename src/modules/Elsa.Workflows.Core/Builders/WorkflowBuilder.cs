using Elsa.Extensions;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Memory;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.Builders;

/// <inheritdoc />
public class WorkflowBuilder(IActivityVisitor activityVisitor, IIdentityGraphService identityGraphService, IActivityRegistry activityRegistry, IIdentityGenerator identityGenerator)
    : IWorkflowBuilder
{
    /// <inheritdoc />
    public string? Id { get; set; }

    /// <inheritdoc />
    public string? DefinitionId { get; set; }

    /// <inheritdoc />
    public string? TenantId { get; set; }

    /// <inheritdoc />
    public int Version { get; set; } = 1;

    /// <inheritdoc />
    public string? Name { get; set; }

    /// <inheritdoc />
    public string? Description { get; set; }

    /// <inheritdoc />
    public bool IsReadonly { get; set; }

    /// <inheritdoc />
    public bool IsSystem { get; set; }

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
    public Variable? Result { get; set; } = new();

    /// <inheritdoc />
    public IDictionary<string, object> CustomProperties { get; set; } = new Dictionary<string, object>();

    /// <inheritdoc />
    public WorkflowOptions WorkflowOptions { get; } = new();

    /// <inheritdoc />
    public IWorkflowBuilder WithDefinitionId(string definitionId)
    {
        DefinitionId = definitionId;
        return this;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithTenantId(string tenantId)
    {
        TenantId = tenantId;
        return this;
    }

    /// <inheritdoc />
    public Variable<T> WithVariable<T>()
    {
        var variable = new Variable<T>();
        Variables.Add(variable);
        variable.WithWorkflowStorage();
        variable.Id = null!; // This ensures that a deterministic ID is assigned by the builder.  
        return variable;
    }

    /// <inheritdoc />
    public Variable<T> WithVariable<T>(string name, T value)
    {
        var variable = WithVariable<T>();
        variable.Name = name;
        variable.Value = value;
        return variable;
    }

    /// <inheritdoc />
    public Variable<T> WithVariable<T>(T value)
    {
        var variable = WithVariable<T>();
        variable.Value = value;
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
        foreach (var variable in variables)
            Variables.Add(variable);
        return this;
    }

    /// <inheritdoc />
    public InputDefinition WithInput<T>(string name, string? description = default)
    {
        return WithInput(name, typeof(T), description);
    }

    /// <inheritdoc />
    public InputDefinition WithInput(string name, Type type, string? description = default)
    {
        return WithInput(inputDefinition =>
        {
            inputDefinition.Name = name;
            inputDefinition.Type = type;

            if (description != null)
                inputDefinition.Description = description;
        });
    }

    /// <inheritdoc />
    public InputDefinition WithInput(string name, Type type, Action<InputDefinition>? setup = default)
    {
        return WithInput(inputDefinition =>
        {
            inputDefinition.Name = name;
            inputDefinition.Type = type;
            setup?.Invoke(inputDefinition);
        });
    }

    /// <inheritdoc />
    public InputDefinition WithInput(Action<InputDefinition> setup)
    {
        var inputDefinition = new InputDefinition();
        setup(inputDefinition);
        Inputs.Add(inputDefinition);
        return inputDefinition;
    }

    /// <inheritdoc />
    public IWorkflowBuilder WithInput(InputDefinition inputDefinition)
    {
        Inputs.Add(inputDefinition);
        return this;
    }

    public OutputDefinition WithOutput<T>(string name, string? description = default)
    {
        return WithOutput(name, typeof(T), description);
    }

    public OutputDefinition WithOutput(string name, Type type, string? description = default)
    {
        return WithOutput(outputDefinition =>
        {
            outputDefinition.Name = name;
            outputDefinition.Type = type;

            if (description != null)
                outputDefinition.Description = description;
        });
    }

    public OutputDefinition WithOutput(string name, Type type, Action<OutputDefinition>? setup = default)
    {
        return WithOutput(outputDefinition =>
        {
            outputDefinition.Name = name;
            outputDefinition.Type = type;
            setup?.Invoke(outputDefinition);
        });
    }

    public OutputDefinition WithOutput(Action<OutputDefinition> setup)
    {
        var outputDefinition = new OutputDefinition();
        setup(outputDefinition);
        return WithOutput(outputDefinition);
    }

    public OutputDefinition WithOutput(OutputDefinition outputDefinition)
    {
        Outputs.Add(outputDefinition);
        return outputDefinition;
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

    /// <summary>
    /// Marks the workflow as readonly.
    /// </summary>
    public IWorkflowBuilder AsReadonly()
    {
        IsReadonly = true;
        return this;
    }

    /// <summary>
    ///   Marks the workflow as a system workflow.
    ///   System workflows are not visible in the UI by default and are not meant to be modified by users.
    /// </summary>
    public IWorkflowBuilder AsSystemWorkflow()
    {
        IsSystem = true;
        return this;
    }

    /// <inheritdoc />
    public async Task<Workflow> BuildWorkflowAsync(CancellationToken cancellationToken = default)
    {
        var definitionId = string.IsNullOrEmpty(DefinitionId) ? string.Empty : DefinitionId;
        var id = string.IsNullOrEmpty(Id) ? string.Empty : Id;
        var tenantId = string.IsNullOrEmpty(TenantId) ? null : TenantId;
        var root = Root ?? new Sequence();
        var identity = new WorkflowIdentity(definitionId, Version, id, tenantId);
        var publication = WorkflowPublication.LatestAndPublished;
        var name = string.IsNullOrEmpty(Name) ? definitionId : Name;
        var workflowMetadata = new WorkflowMetadata(name, Description);
        var workflow = new Workflow(identity, publication, workflowMetadata, WorkflowOptions, root, Variables, Inputs, Outputs, Outcomes, CustomProperties, IsReadonly, IsSystem);

        // If a Result variable is defined, install it into the workflow, so we can capture the output into it.
        if (Result != null)
        {
            workflow.ResultVariable = Result;
            workflow.Result = new Output<object>(Result);
        }

        var graph = await activityVisitor.VisitAsync(workflow, cancellationToken);
        var nodes = graph.Flatten().ToList();

        // Register all activity types first. The identity graph service will need to know about all activity types.
        var distinctActivityTypes = nodes.Select(x => x.Activity.GetType()).Distinct().ToList();
        await activityRegistry.RegisterAsync(distinctActivityTypes, cancellationToken);

        // Assign identities to all activities.
        await identityGraphService.AssignIdentitiesAsync(nodes);

        // Give unnamed variables in each variable container a predictable name.
        var variableContainers = nodes.Where(x => x.Activity is IVariableContainer).Select(x => (IVariableContainer)x.Activity).ToList();

        foreach (var container in variableContainers)
        {
            var index = 0;
            var unnamedVariables = container.Variables.Where(x => string.IsNullOrWhiteSpace(x.Name)).ToList();

            foreach (var unnamedVariable in unnamedVariables)
                unnamedVariable.Name = $"Variable_{index++}";
        }

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