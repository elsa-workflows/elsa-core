using System;
using Elsa.Activities.ControlFlow;
using Elsa.Expressions;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class IfElseBuilderExtensions
    {
        public static ConnectionBuilder If(this ConnectionBuilder connectionBuilder, Func<bool> condition) => connectionBuilder.If(new CodeExpression<bool>(condition));
        public static ConnectionBuilder If(this ConnectionBuilder connectionBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, bool> condition) => connectionBuilder.If(new CodeExpression<bool>(condition));
        public static ConnectionBuilder If(this ConnectionBuilder connectionBuilder, IWorkflowExpression<bool> condition)
        {
            return connectionBuilder.Then<IfElse>(x => x.Condition = condition);
        }
    }
}