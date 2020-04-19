using System;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class RedirectBuilderExtensions
    {
        public static IActivityBuilder Redirect(
            this IBuilder builder,
            Uri location,
            bool permanent = default) => builder.Then<Redirect>(x => x
            .WithLocation(location)
            .WithPermanent(permanent));
    }
}