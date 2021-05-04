using System;
using System.Collections.Generic;

namespace Elsa.Services
{
    public interface IWorkflowContextProvider
    {
        IEnumerable<Type> SupportedTypes { get; }
    }
}