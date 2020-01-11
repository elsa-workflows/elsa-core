using System.Collections.Generic;
using Elsa.Builders;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class ForkExtensions
    {
        public static Fork WithBranches(this Fork activity, IEnumerable<string> value) => activity.With(x => x.Branches, new HashSet<string>(value));
        public static Fork WithBranches(this Fork activity, string[] value) => activity.With(x => x.Branches, new HashSet<string>(value));
    }
}