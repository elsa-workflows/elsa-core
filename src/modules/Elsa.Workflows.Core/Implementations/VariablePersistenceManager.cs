using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Implementations;

/// <inheritdoc />
public class VariablePersistenceManager : IVariablePersistenceManager
{
    private readonly IStorageDriverManager _storageDriverManager;

    /// <summary>
    /// Constructor.
    /// </summary>
    public VariablePersistenceManager(IStorageDriverManager storageDriverManager)
    {
        _storageDriverManager = storageDriverManager;
    }

    /// <inheritdoc />
    public IEnumerable<Variable> GetVariables(WorkflowExecutionContext context) => context.Workflow.Variables.Where(x => x.StorageDriverType != null).ToList();

    /// <inheritdoc />
    public IEnumerable<Variable> GetVariables(ActivityExecutionContext context)
    {
        // Get variables for the current activity itself, if it's a container.
        return context.Activity is Composite composite
            ? composite.Variables.Where(x => x.StorageDriverType != null)
            : Enumerable.Empty<Variable>();
    }

    /// <inheritdoc />
    public IEnumerable<Variable> GetVariablesInScope(ActivityExecutionContext context)
    {
        // Get variables between the current activity and immediate composite container.
        var ancestors = context.ActivityNode.Ancestors();
        
        foreach (var node in ancestors)
        {
            if (node.Activity is IVariableContainer variableContainer)
                foreach (var variable in variableContainer.Variables)
                    yield return variable;

            if (node.Activity is Composite)
                break;
        }
    }

    /// <inheritdoc />
    public async Task LoadVariablesAsync(WorkflowExecutionContext context, IEnumerable<Variable> variables)
    {
        var register = context.MemoryRegister;
        var variableList = variables as ICollection<Variable> ?? variables.ToList();

        EnsureVariables(context, variableList);

        // Foreach variable memory block, load its value from their associated storage driver.
        var cancellationToken = context.CancellationToken;
        var storageDriverContext = new StorageDriverContext(context, cancellationToken);

        foreach (var variable in variableList)
        {
            var block = EnsureBlock(register, variable);
            var metadata = (VariableBlockMetadata)block.Metadata!;
            var driver = _storageDriverManager.Get(metadata.StorageDriverType!);

            block.Metadata = metadata with { IsInitialized = true };

            if (driver == null)
                continue;

            var id = GetStateId(context, variable);
            var value = await driver.ReadAsync(id, storageDriverContext);
            if (value == null) continue;

            var parsedValue = variable.ParseValue(value);
            register.Declare(variable);
            variable.Set(register, parsedValue);
        }
    }

    /// <inheritdoc />
    public async Task SaveVariablesAsync(WorkflowExecutionContext context)
    {
        var register = context.MemoryRegister;

        // Foreach variable memory block, save its value using their associated storage driver.
        var cancellationToken = context.CancellationToken;
        var storageDriverContext = new StorageDriverContext(context, cancellationToken);
        var blocks = register.Blocks.Values.Where(x => x.Metadata is VariableBlockMetadata { StorageDriverType: not null }).ToList();

        foreach (var block in blocks)
        {
            var metadata = (VariableBlockMetadata)block.Metadata!;
            var driver = _storageDriverManager.Get(metadata.StorageDriverType!);

            if (driver == null)
                continue;

            var variable = metadata.Variable;
            var id = GetStateId(context, variable);
            var value = block.Value;

            if (value == null)
                await driver.DeleteAsync(id, storageDriverContext);
            else
                await driver.WriteAsync(id, value, storageDriverContext);
        }
    }

    /// <inheritdoc />
    public void EnsureVariables(WorkflowExecutionContext context, IEnumerable<Variable> variables)
    {
        foreach (var variable in variables) context.MemoryRegister.Declare(variable);
    }

    /// <inheritdoc />
    public async Task DeleteVariablesAsync(WorkflowExecutionContext context, IEnumerable<Variable> variables)
    {
        var register = context.MemoryRegister;
        var variableList = variables as ICollection<Variable> ?? variables.ToList();
        var cancellationToken = context.CancellationToken;
        var storageDriverContext = new StorageDriverContext(context, cancellationToken);

        foreach (var variable in variableList)
        {
            if (!register.TryGetBlock(variable.Id, out var block))
                continue;

            var metadata = (VariableBlockMetadata)block.Metadata!;
            var driver = _storageDriverManager.Get(metadata.StorageDriverType!);

            if (driver == null)
                continue;

            var id = GetStateId(context, variable);
            await driver.DeleteAsync(id, storageDriverContext);
            register.Blocks.Remove(variable.Id);
        }
    }

    private MemoryBlock EnsureBlock(MemoryRegister register, Variable variable)
    {
        if (!register.TryGetBlock(variable.Id, out var block))
            block = register.Declare(variable);
        return block;
    }

    private string GetStateId(WorkflowExecutionContext context, Variable variable) => $"{context.Id}:{context.Workflow.Id}:{variable.Name}";
}