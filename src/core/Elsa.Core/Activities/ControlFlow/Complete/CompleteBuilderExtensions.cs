using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class CompleteBuilderExtensions
    {
        public static ActivityBuilder Complete(this IBuilder builder, Action<Complete> setup) => builder.Then(setup);

        public static ActivityBuilder Complete(
            this IBuilder builder,
            IWorkflowExpression output) =>
            builder.Complete(a => a.WithOutput(output));
    }
}