using System.Runtime.CompilerServices;
using Elsa.Builders;

// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable once CheckNamespace
namespace Elsa.Activities.Console
{
    public static class ReadLineBuilderExtensions
    {
        public static IActivityBuilder ReadLine(this IBuilder builder, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then<ReadLine>(null, lineNumber, sourceFile);
    }
}