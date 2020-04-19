using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class SendMassTransitMessageBuilderExtensions
    {
        public static IActivityBuilder SendMassTransitMessage(this IBuilder builder, Action<SendMassTransitMessage>? setup = default) => builder.Then(setup);

        public static IActivityBuilder SendMassTransitMessage(
            this IBuilder builder, 
            IWorkflowExpression message, 
            Uri endpointAddress) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress));
        
        public static IActivityBuilder SendMassTransitMessage(
            this IBuilder builder, 
            Func<ActivityExecutionContext, object> message, 
            Uri endpointAddress) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress));
        
        public static IActivityBuilder SendMassTransitMessage(
            this IBuilder builder, 
            Func<object> message, 
            Uri endpointAddress) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress));
        
        public static IActivityBuilder SendMassTransitMessages(
            this IBuilder builder, 
            object message, 
            Uri endpointAddress) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress));
    }
}