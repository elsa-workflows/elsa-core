using System;
using System.Collections.Generic;

namespace Elsa.Providers.WorkflowContexts
{
    public interface IWorkflowContextProvider
    {
        IEnumerable<Type> SupportedTypes { get; }
    }
}