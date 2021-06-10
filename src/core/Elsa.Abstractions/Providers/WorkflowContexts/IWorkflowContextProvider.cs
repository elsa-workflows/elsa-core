using System;
using System.Collections.Generic;

namespace Elsa.Providers.WorkflowContext
{
    public interface IWorkflowContextProvider
    {
        IEnumerable<Type> SupportedTypes { get; }
    }
}