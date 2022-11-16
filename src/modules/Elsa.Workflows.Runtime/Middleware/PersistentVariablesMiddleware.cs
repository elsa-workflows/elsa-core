using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.WorkflowExecution;
using Elsa.Workflows.Core.Services;
using Elsa.Workflows.Core.State;

namespace Elsa.Workflows.Runtime.Middleware;

/// <summary>
/// Takes care of loading & persisting workflow variables.
/// </summary>
public class PersistentVariablesMiddleware : WorkflowExecutionMiddleware
{
    private readonly IStorageDriverManager _storageDriverManager;

    public PersistentVariablesMiddleware(WorkflowMiddlewareDelegate next, IStorageDriverManager storageDriverManager) : base(next)
    {
        _storageDriverManager = storageDriverManager;
    }

    public override async ValueTask InvokeAsync(WorkflowExecutionContext context)
    {
        var cancellationToken = context.CancellationToken;
        
        // Load persistent variables.
        var dataDriveContext = new DataDriveContext(context, cancellationToken);
        
        var persistentVariables = context.Workflow.Variables
            .Where(x => x.StorageDriverId != null)
            .Select(x => new PersistentVariableState(x.Name, x.StorageDriverId!))
            .ToList();
        
        foreach (var variableState in persistentVariables)
        {
            var drive = _storageDriverManager.GetDriveById(variableState.StorageDriverId);
            if (drive == null) continue;
            var id = $"{context.Id}:{variableState.Name}";
            var value = await drive.ReadAsync(id, dataDriveContext);
            if (value == null) continue;
            var variable = new Variable(variableState.Name, value);
            context.MemoryRegister.Declare(variable);
        }

        // Invoke next middleware.
        await Next(context);
        
        // Persist variables.
        
        foreach (var variableState in persistentVariables)
        {
            var drive = _storageDriverManager.GetDriveById(variableState.StorageDriverId);
            if (drive == null) continue;
            if (!context.MemoryRegister.TryGetBlock(variableState.Name, out var block)) continue;
            if (block.Value == null) continue;
            var id = $"{context.Id}:{variableState.Name}";
            await drive.WriteAsync(id, block.Value, dataDriveContext);
        }
    }
}