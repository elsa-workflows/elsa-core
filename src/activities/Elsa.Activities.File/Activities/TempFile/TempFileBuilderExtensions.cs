using Elsa.Builders;
using System.Runtime.CompilerServices;

namespace Elsa.Activities.File
{
    public static class TempFileBuilderExtensions
    {
        public static IActivityBuilder TempFile(this IBuilder builder, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then<TempFile>(null, lineNumber, sourceFile);
    }
}
