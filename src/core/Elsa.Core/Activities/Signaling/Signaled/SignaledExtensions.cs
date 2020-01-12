using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class SignaledExtensions
    {
        public static Signaled WithSignal(this Signaled activity, IWorkflowExpression<string> value) => activity.With(x => x.Signal, value);
        public static Signaled WithSignal(this Signaled activity, Func<ActivityExecutionContext, string> value) => activity.With(x => x.Signal, new CodeExpression<string>(value));
        public static Signaled WithSignal(this Signaled activity, Func<string> value) => activity.With(x => x.Signal, new CodeExpression<string>(value));
        public static Signaled WithSignal(this Signaled activity, string value) => activity.With(x => x.Signal, new CodeExpression<string>(value));
    }
}