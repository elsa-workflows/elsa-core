using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class RedirectBuilderExtensions
    {
        public static IActivityBuilder Redirect(
            this IBuilder builder,
            Uri location,
            bool permanent = default,
            [CallerLineNumber] int lineNumber = default,
            [CallerFilePath] string? sourceFile = default) => builder.Then<Redirect>(
            redirect => redirect
                .Set(x => x.Location, location)
                .Set(x => x.Permanent, permanent),
            null,
            lineNumber,
            sourceFile);
    }
}