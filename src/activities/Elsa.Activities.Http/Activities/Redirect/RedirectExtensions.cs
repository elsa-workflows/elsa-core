using System;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;
using Microsoft.AspNetCore.Http;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class RedirectExtensions
    {
        public static Redirect WithLocation(this Redirect activity, IWorkflowExpression<PathString> value) => activity.With(x => x.Location, value);
        public static Redirect WithLocation(this Redirect activity, Func<ActivityExecutionContext, PathString> value) => activity.With(x => x.Location, new CodeExpression<PathString>(value));
        public static Redirect WithLocation(this Redirect activity, Func<PathString> value) => activity.With(x => x.Location, new CodeExpression<PathString>(value));
        public static Redirect WithLocation(this Redirect activity, PathString value) => activity.With(x => x.Location, new CodeExpression<PathString>(value));
        public static Redirect WithPermanent(this Redirect activity, bool value) => activity.With(x => x.Permanent, value);
    }
}