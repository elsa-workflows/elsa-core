using System;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class SendMassTransitMessageExtensions
    {
        public static SendMassTransitMessage WithMessage(this SendMassTransitMessage activity, Func<ActivityExecutionContext, object> value) => activity.With(x => x.Message, value);
        public static SendMassTransitMessage WithMessage(this SendMassTransitMessage activity, Func<object> value) => activity.With(x => x.Message, value);
        public static SendMassTransitMessage WithMessage(this SendMassTransitMessage activity, object value) => activity.With(x => x.Message, value);
        public static SendMassTransitMessage WithEndpointAddress(this SendMassTransitMessage activity, Uri value) => activity.With(x => x.EndpointAddress, value);
    }
}