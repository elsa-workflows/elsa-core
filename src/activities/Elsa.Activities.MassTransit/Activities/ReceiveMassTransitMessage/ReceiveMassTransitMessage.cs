using System;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    [Trigger(
        Category = "MassTransit",
        DisplayName = "Receive MassTransit Message",
        Description = "Receive a message via MassTransit."
    )]
    public class ReceiveMassTransitMessage : Activity
    {
        [ActivityInput(Hint = "The assembly-qualified type name of the message to receive.")]
        public Type? MessageType { get; set; }

        [ActivityOutput] public object? Output { get; set; }

        protected override bool OnCanExecute(ActivityExecutionContext context) => MessageType?.IsAssignableFrom(context.Input?.GetType()) ?? false;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => context.WorkflowExecutionContext.IsFirstPass ? ExecuteInternal(context) : Suspend();

        protected override IActivityExecutionResult OnResume(ActivityExecutionContext context) => ExecuteInternal(context);

        private IActivityExecutionResult ExecuteInternal(ActivityExecutionContext context)
        {
            Output = context.Input;
            return Done();
        }
    }
}