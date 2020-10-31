using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public abstract class WorkflowContextProvider<T> : IWorkflowContextProvider
    {
        public IEnumerable<Type> SupportedTypes => new[] { typeof(T) };
        public virtual ValueTask<object?> LoadContextAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default) => new ValueTask<object?>();
        public virtual ValueTask<string?> SaveContextAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default) => new ValueTask<string?>();
    }
}