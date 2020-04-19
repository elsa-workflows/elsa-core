using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class RedirectExtensions
    {
        public static Redirect WithLocation(this Redirect activity, IWorkflowExpression<Uri> value) => activity.With(x => x.Location, value);
        public static Redirect WithLocation(this Redirect activity, Func<ActivityExecutionContext, Uri> value) => activity.With(x => x.Location, new CodeExpression<Uri>(value));
        public static Redirect WithLocation(this Redirect activity, Func<Uri> value) => activity.With(x => x.Location, new CodeExpression<Uri>(value));
        public static Redirect WithLocation(this Redirect activity, Uri value) => activity.With(x => x.Location, new CodeExpression<Uri>(value));
        public static Redirect WithPermanent(this Redirect activity, bool value) => activity.With(x => x.Permanent, value);
    }
}