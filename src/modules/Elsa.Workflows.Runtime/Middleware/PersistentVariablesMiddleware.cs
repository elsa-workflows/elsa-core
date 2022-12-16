using Elsa.Expressions.Helpers;
using Elsa.Workflows.Core;
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
        
        var persistentVariables = context.Workflow.Variables.Where(x => x.StorageDriverId != null).ToList();
        
        foreach (var variable in persistentVariables)
        {
            var drive = _storageDriverManager.GetDriveById(variable.StorageDriverId!);
            if (drive == null) continue;
            var id = $"{context.Id}:{variable.Name}";
            var value = await drive.ReadAsync(id, dataDriveContext);
            if (value == null) continue;
            var parsedValue = ParseVariableValue(variable, value);
            context.MemoryRegister.Declare(variable);
            variable.Set(context.MemoryRegister, parsedValue);
        }

        // Invoke next middleware.
        await Next(context);
        
        // Persist variables.
        foreach (var variable in persistentVariables)
        {
            var drive = _storageDriverManager.GetDriveById(variable.StorageDriverId!);
            if (drive == null) continue;
            if (!context.MemoryRegister.TryGetBlock(variable.Name, out var block)) continue;
            if (block.Value == null) continue;
            var id = $"{context.Id}:{variable.Name}";
            await drive.WriteAsync(id, block.Value, dataDriveContext);
        }
    }

    private object ParseVariableValue(Variable variable, object value)
    {
        if (!variable.GetType().GenericTypeArguments.Any())
            return value;

        var type = variable.GetType().GenericTypeArguments.First();
        return value.ConvertTo(type)!;
    }
}