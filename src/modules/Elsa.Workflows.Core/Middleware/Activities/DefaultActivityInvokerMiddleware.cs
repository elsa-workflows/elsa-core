using Elsa.Common.Extensions;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Activities;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Pipelines.ActivityExecution;
using Elsa.Workflows.Core.Services;
using Delegate = System.Delegate;

namespace Elsa.Workflows.Core.Middleware.Activities;

/// <summary>
/// Provides extension methods to <see cref="IActivityExecutionBuilder"/>.
/// </summary>
public static class ActivityInvokerMiddlewareExtensions
{
    /// <summary>
    /// Adds the <see cref="DefaultActivityInvokerMiddleware"/> component to the pipeline.
    /// </summary>
    public static IActivityExecutionBuilder UseDefaultActivityInvoker(this IActivityExecutionBuilder builder) => builder.UseMiddleware<DefaultActivityInvokerMiddleware>();
}

/// <summary>
/// A default activity execution middleware component that evaluates the current activity's properties, executes the activity and adds any produced bookmarks to the workflow execution context.
/// </summary>
public class DefaultActivityInvokerMiddleware : IActivityExecutionMiddleware
{
    private readonly ActivityMiddlewareDelegate _next;

    /// <summary>
    /// Constructor
    /// </summary>
    public DefaultActivityInvokerMiddleware(ActivityMiddlewareDelegate next)
    {
        _next = next;
    }

    /// <inheritdoc />
    public async ValueTask InvokeAsync(ActivityExecutionContext context)
    {
        var workflowExecutionContext = context.WorkflowExecutionContext;

        // Restore uninitialized variables for each containing activity.
        await LoadVariablesAsync(context);
        
        // Evaluate input properties.
        await EvaluateInputPropertiesAsync(context);

        // Execute activity.
        await ExecuteActivityAsync(context);

        // Reset execute delegate.
        workflowExecutionContext.ExecuteDelegate = null;

        // If a bookmark was used to resume, burn it if not burnt by the activity.
        var resumedBookmark = workflowExecutionContext.ResumedBookmarkContext?.Bookmark;

        if (resumedBookmark is { AutoBurn: true })
            workflowExecutionContext.Bookmarks.Remove(resumedBookmark);

        // Update execution count.
        context.IncrementExecutionCount();

        // Invoke next middleware.
        await _next(context);

        // If the activity created any bookmarks, copy them into the workflow execution context.
        if (context.Bookmarks.Any())
        {
            // Store bookmarks.
            workflowExecutionContext.Bookmarks.AddRange(context.Bookmarks);
        }
        
        // Persist variables.
        await SaveVariablesAsync(context);
    }

    /// <summary>
    /// Executes the activity using the specified context.
    /// This method is virtual so that modules might override this implementation to do things like e.g. asynchronous processing.
    /// </summary>
    protected virtual async ValueTask ExecuteActivityAsync(ActivityExecutionContext context)
    {
        var executeDelegate = context.WorkflowExecutionContext.ExecuteDelegate;

        if (executeDelegate == null)
        {
            var methodInfo = typeof(IActivity).GetMethod(nameof(IActivity.ExecuteAsync))!;
            executeDelegate = (ExecuteActivityDelegate)Delegate.CreateDelegate(typeof(ExecuteActivityDelegate), context.Activity, methodInfo);
        }

        await executeDelegate(context);
    }

    private async Task EvaluateInputPropertiesAsync(ActivityExecutionContext context)
    {
        // Evaluate containing composite input properties, if any.
        var compositeContainerContext = context.GetAncestors().FirstOrDefault(x => x.Activity is Composite);
        
        if (compositeContainerContext != null && !compositeContainerContext.GetHasEvaluatedProperties())
            await compositeContainerContext.EvaluateInputPropertiesAsync();

        // Evaluate input properties.
        await context.EvaluateInputPropertiesAsync();
    }

    private async Task LoadVariablesAsync(ActivityExecutionContext context)
    {
        var variables = GetVariablesInScope(context).ToList(); 
        var register = context.WorkflowExecutionContext.MemoryRegister;
        
        EnsureVariablesAreDeclared(register, variables);
        
        // Foreach variable memory block, load its value from their associated storage driver.
        var cancellationToken = context.CancellationToken;
        var storageDriverContext = new StorageDriverContext(context.WorkflowExecutionContext, cancellationToken);
        var blocks = register.Blocks.Values.Where(x => x.Metadata is VariableBlockMetadata { IsInitialized: false, StorageDriverType: not null }).ToList();
        var storageDriverManager = context.GetRequiredService<IStorageDriverManager>();

        foreach (var block in blocks)
        {
            var metadata = (VariableBlockMetadata)block.Metadata!;
            var driver = storageDriverManager.Get(metadata.StorageDriverType!);

            block.Metadata = metadata with { IsInitialized = true };
            
            if (driver == null)
                continue;

            var variable = metadata.Variable;
            var id = $"{context.WorkflowExecutionContext.Id}:{metadata.Variable.Id}";
            var value = await driver.ReadAsync(id, storageDriverContext);
            if (value == null) continue;

            var parsedValue = variable.ParseValue(value);
            register.Declare(variable);
            variable.Set(register, parsedValue);
        }
    }
    
    private async Task SaveVariablesAsync(ActivityExecutionContext context)
    {
        var register = context.WorkflowExecutionContext.MemoryRegister;
        
        // Foreach variable memory block, save its value using their associated storage driver.
        var cancellationToken = context.CancellationToken;
        var storageDriverContext = new StorageDriverContext(context.WorkflowExecutionContext, cancellationToken);
        var blocks = register.Blocks.Values.Where(x => x.Metadata is VariableBlockMetadata { StorageDriverType: not null }).ToList();
        var storageDriverManager = context.GetRequiredService<IStorageDriverManager>();

        foreach (var block in blocks)
        {
            var metadata = (VariableBlockMetadata)block.Metadata!;
            var driver = storageDriverManager.Get(metadata.StorageDriverType!);

            if (driver == null)
                continue;

            var variable = metadata.Variable;
            var id = $"{context.WorkflowExecutionContext.Id}:{variable.Id}";
            var value = block.Value;
            
            if (value == null)
                await driver.DeleteAsync(id, storageDriverContext);
            else
                await driver.WriteAsync(id, value, storageDriverContext);
        }
    }

    private IEnumerable<Variable> GetVariablesInScope(ActivityExecutionContext context)
    {
        // Get variables for the current activity's immediate composite container.
        var immediateCompositeVariables = ((Composite?)context.ActivityNode.Ancestors().FirstOrDefault(x => x.Activity is Composite)?.Activity)?.Variables.Where(x => x.StorageDriverType != null) ?? Enumerable.Empty<Variable>();
        
        // Get variables for the current activity itself, if it's a container.
        var directVariables = (context.Activity is Composite composite ? composite.Variables.Where(x => x.StorageDriverType != null) : Enumerable.Empty<Variable>());

        // Return a concatenated list of variables.
        return immediateCompositeVariables.Concat(directVariables);
    }

    private void EnsureVariablesAreDeclared(MemoryRegister register, IEnumerable<Variable> variables)
    {
        foreach (var variable in variables)
        {
            if (!register.IsDeclared(variable))
                register.Declare(variable);
        }
    }
}