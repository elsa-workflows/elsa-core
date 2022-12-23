using Elsa.Expressions.Helpers;
using Elsa.Workflows.Core;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Runtime.Middleware;

/// <summary>
/// Takes care of loading & persisting workflow variables.
/// </summary>
public class PersistentVariablesMiddleware : WorkflowExecutionMiddleware
{
    private readonly IStorageDriverManager _storageDriverManager;

    /// <summary>
    /// Constructor.
    /// </summary>
    public PersistentVariablesMiddleware(WorkflowMiddlewareDelegate next, IStorageDriverManager storageDriverManager) : base(next)
    {
        _storageDriverManager = storageDriverManager;
    }

    /// <inheritdoc />
    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        
        // Load persistent variables.
        var storageDriverContext = new StorageDriverContext(context, cancellationToken);
        
        var persistentVariables = context.Workflow.Variables.Where(x => x.StorageDriverType != null).ToList();
        
        foreach (var variable in persistentVariables)
        {
            var driver = _storageDriverManager.Get(variable.StorageDriverType!);
            if (driver == null) continue;
            var id = $"{context.Id}:{context.Workflow.Id}:{variable.Name}";
            var value = await driver.ReadAsync(id, storageDriverContext);
            if (value == null) continue;
            var parsedValue = variable.ParseValue(value);
            var block = context.MemoryRegister.Declare(variable);
            var metadata = (VariableBlockMetadata)block.Metadata!;
            variable.Set(context.MemoryRegister, parsedValue);
            block.Metadata = metadata with { IsInitialized = true };
        }

        // Invoke next middleware.
        await Next(context);
        
        // Persist variables.
        foreach (var variable in persistentVariables)
        {
            var driver = variable.StorageDriverType != null ? _storageDriverManager.Get(variable.StorageDriverType) : default;
            if (driver == null) continue;
            if(!context.MemoryRegister.TryGetBlock(variable.Id, out var block))
                continue;
            
            var id = $"{context.Id}:{context.Workflow.Id}:{variable.Name}";
            var value = block.Value;
            
            if (value == null)
                await driver.DeleteAsync(id, storageDriverContext);
            else
                await driver.WriteAsync(id, value, storageDriverContext);
        }
    }
}