﻿using System.Threading;
using System.Threading.Tasks;
using Elsa.Providers.WorkflowContexts;
using Elsa.Services.Models;

namespace Elsa.Services
{
    public interface ISaveWorkflowContext : IWorkflowContextProvider
    {
        ValueTask<string?> SaveAsync(SaveWorkflowContext context, CancellationToken cancellationToken = default);
    }
}