using System;
using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Http
{
    public static class RedirectBuilderExtensions
    {
        /// <summary>
        /// Creates a Redirect activity.
        /// </summary>
        /// <param name="location">The URL to redirect the request to.</param>
        /// <param name="permanent">Wether or not this redirect is permanent (301)</param>
        /// <returns>The <see cref="IActivityBuilder"/> with the Redirect activity built onto it.</returns>
        /// <inheritdoc cref="BuilderExtensions.Then{T}(IBuilder, Action{ISetupActivity{T}}?, Action{IActivityBuilder}?, int, string?)"/>
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