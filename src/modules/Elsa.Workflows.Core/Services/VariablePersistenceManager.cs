using Elsa.Expressions.Models;
using Elsa.Extensions;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Memory;

namespace Elsa.Workflows;

/// <inheritdoc />
public class VariablePersistenceManager(IStorageDriverManager storageDriverManager) : IVariablePersistenceManager
{
    /// <inheritdoc />
    public async Task LoadVariablesAsync(WorkflowExecutionContext workflowExecutionContext)
    {
        var cancellationToken = workflowExecutionContext.CancellationToken;
        var contexts = workflowExecutionContext.ActivityExecutionContexts.ToList();

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
                var driver = storageDriverManager.Get(metadata.StorageDriverType!);

                block.Metadata = metadata with
                {
                    IsInitialized = true
                };

                if (driver == null)
                    continue;

                var id = GetStateId(variable);
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
        var contexts = workflowExecutionContext.ActivityExecutionContexts.ToList();

        foreach (var context in contexts)
        {
            var variables = GetLocalVariables(context).ToList();
            var storageDriverContext = new StorageDriverContext(context, cancellationToken);

            foreach (var variable in variables)
            {
                var block = variable.GetBlock(context.ExpressionExecutionContext);
                var metadata = (VariableBlockMetadata)block.Metadata!;
                var driver = storageDriverManager.Get(metadata.StorageDriverType!);

                if (driver == null)
                    continue;

                var id = GetStateId(variable);
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
        var variableList = GetLocalVariables(context).ToList();
        var cancellationToken = context.CancellationToken;
        var storageDriverContext = new StorageDriverContext(context, cancellationToken);

        foreach (var variable in variableList)
        {
            if (!register.TryGetBlock(variable.Id, out var block))
                continue;

            var metadata = (VariableBlockMetadata)block.Metadata!;
            var driver = storageDriverManager.Get(metadata.StorageDriverType!);

            if (driver == null)
                continue;

            var id = GetStateId(variable);
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

    private string GetStateId(Variable variable) => variable.Id;
}