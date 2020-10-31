using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface IWorkflowContextProvider
    {
        IEnumerable<Type> SupportedTypes { get; }
        ValueTask<object?> LoadContextAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default);
        ValueTask<string?> SaveContextAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default);
    }
}