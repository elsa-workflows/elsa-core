using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using NodaTime;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class SendMassTransitMessageBuilderExtensions
    {
        public static ActivityBuilder SendMassTransitMessage(this IBuilder builder, Action<SendMassTransitMessage>? setup = default) => builder.Then(setup);

        public static ActivityBuilder SendMassTransitMessage(
            this IBuilder builder, 
            IWorkflowExpression message, 
            Uri endpointAddress) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress));
        
        public static ActivityBuilder SendMassTransitMessage(
            this IBuilder builder, 
            Func<ActivityExecutionContext, object> message, 
            Uri endpointAddress) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress));
        
        public static ActivityBuilder SendMassTransitMessage(
            this IBuilder builder, 
            Func<object> message, 
            Uri endpointAddress) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress));
        
        public static ActivityBuilder SendMassTransitMessages(
            this IBuilder builder, 
            object message, 
            Uri endpointAddress) =>
            builder.ScheduleSendMassTransitMessage(x => x
                .WithMessage(message)
                .WithEndpointAddress(endpointAddress));
    }
}