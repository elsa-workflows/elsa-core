using System;
using System.Collections;
using System.Collections.Generic;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForEachExtensions
    {
        public static ForEach WithCollection(this ForEach activity, IWorkflowExpression<ICollection> value) => activity.With(x => x.Collection, value);
        public static ForEach WithCollection(this ForEach activity, Func<ActivityExecutionContext, ICollection> value) => activity.WithCollection(new CodeExpression<ICollection>(value));
        public static ForEach WithCollection(this ForEach activity, Func<ICollection> value) => activity.WithCollection(new CodeExpression<ICollection>(value));
        public static ForEach WithCollection(this ForEach activity, ICollection value) => activity.WithCollection(new CodeExpression<ICollection>(value));
    }
}