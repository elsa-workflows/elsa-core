using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Memory;

namespace Elsa.Workflows.Core.Services;

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
    public async Task LoadVariablesAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var cancellationToken = workflowExecutionContext.CancellationToken;
        
        var contexts = workflowExecutionContext.ActiveActivityExecutionContexts
            .Cast<IExecutionContext>()
            .Concat(new[]{workflowExecutionContext})
            .ToList();

        foreach (var context in contexts)
        {
            var variables = GetLocalVariables(context).ToList();

            foreach (var variable in variables)
            {
                context.ExpressionExecutionContext.Memory.Declare(variable);
                var storageDriverContext = new StorageDriverContext(context, cancellationToken);
                var register = context.ExpressionExecutionContext.Memory;
                var block = EnsureBlock(register, variable);
                var metadata = (VariableBlockMetadata)block.Metadata!;
                var driver = _storageDriverManager.Get(metadata.StorageDriverType!);

                block.Metadata = metadata with { IsInitialized = true };

                if (driver == null)
                    continue;

                var id = GetStateId(workflowExecutionContext, context, variable);
                var value = await driver.ReadAsync(id, storageDriverContext);
                if (value == null) continue;

                var parsedValue = variable.ParseValue(value);
                register.Declare(variable);
                variable.Set(register, parsedValue);
            }
        }
    }

    /// <inheritdoc />
    public async Task SaveVariablesAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var cancellationToken = workflowExecutionContext.CancellationToken;
        
        var contexts = workflowExecutionContext.ActiveActivityExecutionContexts
            .Cast<IExecutionContext>()
            .Concat(new[]{workflowExecutionContext})
            .ToList();

        foreach (var context in contexts)
        {
            var variables = GetLocalVariables(context);
            var storageDriverContext = new StorageDriverContext(context, cancellationToken);

            foreach (var variable in variables)
            {
                var block = variable.GetBlock(context.ExpressionExecutionContext);
                var metadata = (VariableBlockMetadata)block.Metadata!;
                var driver = _storageDriverManager.Get(metadata.StorageDriverType!);

                if (driver == null)
                    continue;
                
                var id = GetStateId(workflowExecutionContext, context, variable);
                var value = block.Value;

                if (value == null)
                    await driver.DeleteAsync(id, storageDriverContext);
                else
                    await driver.WriteAsync(id, value, storageDriverContext);
            }
        }
    }


    /// <inheritdoc />
    public async Task DeleteVariablesAsync(ActivityExecutionContext context)
    {
        var register = context.ExpressionExecutionContext.Memory;
        var variableList = GetLocalVariables(context);
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

            var id = GetStateId(context.WorkflowExecutionContext, context, variable);
            await driver.DeleteAsync(id, storageDriverContext);
            register.Blocks.Remove(variable.Id);
        }
    }

    private IEnumerable<Variable> GetLocalVariables(IExecutionContext context) => context.Variables;

    private MemoryBlock EnsureBlock(MemoryRegister register, Variable variable)
    {
        if (!register.TryGetBlock(variable.Id, out var block))
            block = register.Declare(variable);
        return block;
    }

    private string GetStateId(WorkflowExecutionContext workflowExecutionContext, IExecutionContext context, Variable variable) => $"{context.Id}:{workflowExecutionContext.Workflow.Id}:{variable.Name}";
}