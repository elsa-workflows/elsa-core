using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.ControlFlow
{
    public static class BreakBuilderExtensions
    {
        public static IActivityBuilder Break(this IBuilder builder, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then<Break>(null, lineNumber, sourceFile);
    }
}