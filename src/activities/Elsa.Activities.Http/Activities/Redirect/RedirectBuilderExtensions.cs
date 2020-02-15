using Elsa.Builders;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class RedirectBuilderExtensions
    {
        public static ActivityBuilder Redirect(
            this IBuilder builder,
            PathString location,
            bool permanent = default) => builder.Then<Redirect>(x => x
            .WithLocation(location)
            .WithPermanent(permanent));
    }
}