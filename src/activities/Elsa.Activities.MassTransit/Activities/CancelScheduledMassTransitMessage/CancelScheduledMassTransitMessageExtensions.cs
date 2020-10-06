using System;
using Elsa.Builders;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.MassTransit
{
    public static class CancelScheduledMassTransitMessageExtensions
    {
        public static CancelScheduledMassTransitMessage WithTokenId(this CancelScheduledMassTransitMessage activity, Func<ActivityExecutionContext, string?>? value) => activity.With(x => x.TokenId, value);
        public static CancelScheduledMassTransitMessage WithTokenId(this CancelScheduledMassTransitMessage activity, Func<string?>? value) => activity.With(x => x.TokenId, value);
        public static CancelScheduledMassTransitMessage WithTokenId(this CancelScheduledMassTransitMessage activity, string? value) => activity.With(x => x.TokenId, value);
    }
}