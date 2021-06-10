using System;
using System.Collections.Generic;
using Elsa.Providers.WorkflowContext;

namespace Elsa.Services
{
    public abstract class WorkflowContextProvider : IWorkflowContextProvider
    {
        public abstract IEnumerable<Type> SupportedTypes { get; }
    }
    
    public abstract class WorkflowContextProvider<T> : WorkflowContextProvider
    {
        public override IEnumerable<Type> SupportedTypes => new[] { typeof(T) };
    }
}