using System;
using Elsa.Scripting;

namespace Elsa.Expressions
{
    public interface IWorkflowExpression<T> : IWorkflowExpression
    {
    }

    public interface IWorkflowExpression
    {
        string Type { get; }
        Type ReturnType { get; }
    }
}