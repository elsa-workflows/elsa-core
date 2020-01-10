using System;
using System.Collections.Generic;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForEachExtensions
    {
        public static ForEach WithCollection(this ForEach activity, IWorkflowExpression<ICollection<object>> value) => activity.With(x => x.Collection, value);
        public static ForEach WithCollection(this ForEach activity, Func<ActivityExecutionContext, ICollection<object>> value) => activity.WithCollection(new CodeExpression<ICollection<object>>(value));
        public static ForEach WithCollection(this ForEach activity, Func<ICollection<object>> value) => activity.WithCollection(new CodeExpression<ICollection<object>>(value));
        public static ForEach WithCollection(this ForEach activity, ICollection<object> value) => activity.WithCollection(() => value);
    }
}