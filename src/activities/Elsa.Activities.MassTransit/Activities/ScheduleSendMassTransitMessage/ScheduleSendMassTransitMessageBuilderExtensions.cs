using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class ScheduleSendMassTransitMessageBuilderExtensions
    {
        public static ActivityBuilder ScheduleSendMassTransitMessage(this IBuilder builder, Action<ScheduleSendMassTransitMessage>? setup = default) => builder.Then(setup);

        public static ActivityBuilder ScheduleSendMassTransitMessage(
            this IBuilder builder, 
            IWorkflowExpression message, 
            Uri endpointAddress, 
            IWorkflowExpression<Instant> scheduledTime) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress)
                .WithScheduledTime(scheduledTime));
        
        public static ActivityBuilder ScheduleSendMassTransitMessage(
            this IBuilder builder, 
            Func<ActivityExecutionContext, object> message, 
            Uri endpointAddress, 
            Func<ActivityExecutionContext, Instant> scheduledTime) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress)
                .WithScheduledTime(scheduledTime));
        
        public static ActivityBuilder ScheduleSendMassTransitMessage(
            this IBuilder builder, 
            Func<object> message, 
            Uri endpointAddress, 
            Func<Instant> scheduledTime) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress)
                .WithScheduledTime(scheduledTime));
        
        public static ActivityBuilder ScheduleSendMassTransitMessage(
            this IBuilder builder, 
            object message, 
            Uri endpointAddress, 
            Instant scheduledTime) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress)
                .WithScheduledTime(scheduledTime));
    }
}