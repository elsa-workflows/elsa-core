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
        public virtual ValueTask<string?> SaveAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default) => new ValueTask<string?>();
        public virtual ValueTask<object?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default) => new ValueTask<object?>();
    }
    
    public abstract class WorkflowContextRefresher<T> : ILoadWorkflowContext, ISaveWorkflowContext
    {
        public virtual IEnumerable<Type> SupportedTypes => new[] { typeof(T) };
        public virtual ValueTask<string?> SaveAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default) => new ValueTask<string?>();
        public virtual ValueTask<object?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default) => new ValueTask<object?>();
    }
}