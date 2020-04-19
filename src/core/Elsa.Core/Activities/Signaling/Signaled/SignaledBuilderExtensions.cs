using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Signaling
{
    public static class SignaledBuilderExtensions
    {
        public static IActivityBuilder Signaled(this IBuilder builder, IWorkflowExpression<string> signal) => builder.Then<Signaled>(x => x.WithSignal(signal));
        public static IActivityBuilder Signaled(this IBuilder builder, Func<ActivityExecutionContext, string> signal) => builder.Then<Signaled>(x => x.WithSignal(signal));
        public static IActivityBuilder Signaled(this IBuilder builder, Func<string> signal) => builder.Then<Signaled>(x => x.WithSignal(signal));
        public static IActivityBuilder Signaled(this IBuilder builder, string signal) => builder.Then<Signaled>(x => x.WithSignal(signal));
    }
}