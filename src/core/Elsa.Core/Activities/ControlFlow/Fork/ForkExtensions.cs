using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Builders;
using Elsa.Expressions;
using Elsa.Services.Models;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForkExtensions
    {
        public static Fork WithBranches(this Fork activity, IEnumerable<string> value) => activity.With(x => x.Branches, new HashSet<string>(value));
        public static Fork WithBranches(this Fork activity, string[] value) => activity.With(x => x.Branches, new HashSet<string>(value));
    }
}