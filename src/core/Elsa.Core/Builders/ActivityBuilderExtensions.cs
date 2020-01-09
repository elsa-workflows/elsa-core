using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Primitives;
using Elsa.Expressions;
using Elsa.Results;
using Elsa.Services.Models;

namespace Elsa.Builders
{
    public static class ActivityBuilderExtensions
    {
        public static ActivityBuilder SetVariable(this ActivityBuilder activityBuilder, string variableName, IWorkflowExpression<object> value) => activityBuilder.Then<SetVariable>(x =>
        {
            x.VariableName = variableName;
            x.Value = value;
        });
        
        public static ActivityBuilder SetVariable(this ActivityBuilder activityBuilder, string variableName, Func<object> value) => activityBuilder.SetVariable(variableName, (IWorkflowExpression<object>)new CodeExpression<object>(value));
        public static ActivityBuilder SetVariable<T>(this ActivityBuilder activityBuilder, string variableName, T value) => activityBuilder.SetVariable(variableName, () => value);
        
        public static ActivityBuilder Then(this ActivityBuilder activityBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task<IActivityExecutionResult>> activity) => activityBuilder.Then(new Inline(activity));
        public static ActivityBuilder Then(this ActivityBuilder activityBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, Task> activity) => activityBuilder.Then(new Inline(activity));
        public static ActivityBuilder Then(this ActivityBuilder activityBuilder, Action<WorkflowExecutionContext, ActivityExecutionContext> activity) => activityBuilder.Then(new Inline(activity));
        public static ActivityBuilder Then(this ActivityBuilder activityBuilder, Action activity) => activityBuilder.Then(new Inline(activity));

        public static IfElseBuilder If(this ActivityBuilder activityBuilder, IWorkflowExpression<bool> condition)
        {
            var ifElse = activityBuilder.Then<IfElse>(x => x.Condition = condition);
            return new IfElseBuilder(ifElse);
        }

        public static IfElseBuilder If(this ActivityBuilder activityBuilder, Func<bool> condition) => If(activityBuilder, new CodeExpression<bool>(condition));
        public static IfElseBuilder If(this ActivityBuilder activityBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, bool> condition) => If(activityBuilder, new CodeExpression<bool>(condition));

        public static ForBuilder For(this ActivityBuilder activityBuilder, IWorkflowExpression<int> start, IWorkflowExpression<int> end, IWorkflowExpression<int> step)
        {
            var @for = activityBuilder.Then<For>(x =>
            {
                x.Start = start;
                x.End = end;
                x.Step = step;
            });
            return new ForBuilder(@for);
        }

        public static ForBuilder For(this ActivityBuilder activityBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, int> start, Func<WorkflowExecutionContext, ActivityExecutionContext, int> end, Func<WorkflowExecutionContext, ActivityExecutionContext, int>? step = null) => activityBuilder.For(new CodeExpression<int>(start), new CodeExpression<int>(end), new CodeExpression<int>(step ?? ((w, a) => 1)));
        public static ForBuilder For(this ActivityBuilder activityBuilder, Func<int> start, Func<int> end, Func<int>? step = null) => activityBuilder.For(new CodeExpression<int>(start), new CodeExpression<int>(end), new CodeExpression<int>(step ?? (() => 1)));
        public static ForBuilder For(this ActivityBuilder activityBuilder, int start, int end, int step = 1) => activityBuilder.For(() => start, () => end, () => step);


        public static ForEachBuilder ForEach(this ActivityBuilder activityBuilder, IWorkflowExpression<ICollection<object>> collection)
        {
            var forEach = activityBuilder.Then<ForEach>(x => x.Collection = collection);
            return new ForEachBuilder(forEach);
        }

        public static ForEachBuilder ForEach(this ActivityBuilder activityBuilder, Func<WorkflowExecutionContext, ActivityExecutionContext, ICollection<object>> collection) => activityBuilder.ForEach(new CodeExpression<ICollection<object>>(collection));
        public static ForEachBuilder ForEach(this ActivityBuilder activityBuilder, Func<ICollection<object>> collection) => activityBuilder.ForEach(new CodeExpression<ICollection<object>>(collection));
        public static ForEachBuilder ForEach(this ActivityBuilder activityBuilder, ICollection<object> collection) => activityBuilder.ForEach(new CodeExpression<ICollection<object>>(() => collection));
    }
}