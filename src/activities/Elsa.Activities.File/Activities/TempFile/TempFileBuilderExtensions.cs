using Elsa.Builders;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Elsa.Activities.File
{
    public static class TempFileBuilderExtensions
    {
        public static IActivityBuilder TempFile(this IBuilder builder, [CallerLineNumber] int lineNumber = default, [CallerFilePath] string? sourceFile = default) => builder.Then<TempFile>(null, lineNumber, sourceFile);
    }
}
