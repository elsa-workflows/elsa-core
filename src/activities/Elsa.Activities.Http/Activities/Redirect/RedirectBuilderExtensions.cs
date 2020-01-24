using System;
using System.Net.Http;
using Elsa.Activities.ControlFlow;
using Elsa.Activities.Http;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
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