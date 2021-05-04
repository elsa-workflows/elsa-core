using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class WorkflowContextRefresher : ILoadWorkflowContext, ISaveWorkflowContext
    {
        public abstract IEnumerable<Type> SupportedTypes { get; }
        public virtual ValueTask<string?> SaveAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default) => new();
        public virtual ValueTask<object?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default) => new();
    }
    
    public abstract class WorkflowContextRefresher<T> : ILoadWorkflowContext, ISaveWorkflowContext where T:class
    {
        public virtual IEnumerable<Type> SupportedTypes => new[] { typeof(T) };
        ValueTask<string?> ISaveWorkflowContext.SaveAsync(SaveWorkflowContext context, CancellationToken cancellationToken) => SaveAsync(new SaveWorkflowContext<T>(context), cancellationToken);
        async ValueTask<object?> ILoadWorkflowContext.LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken) => await LoadAsync(context, cancellationToken);
        public virtual ValueTask<string?> SaveAsync(SaveWorkflowContext<T> context, CancellationToken cancellationToken = default) => new();
        public virtual ValueTask<T?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken) => new();
    }
}