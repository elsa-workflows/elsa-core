using System.Runtime.CompilerServices;
using Elsa.Expressions.Models;
using Elsa.Workflows.Core.Attributes;
using Elsa.Workflows.Core.Contracts;
using Elsa.Workflows.Core.Models;
using Elsa.Workflows.Core.Services;

namespace Elsa.Workflows.Core.Activities;

/// <summary>
/// Read a line of text from the console.
/// </summary>
[Activity("Elsa", "Console", "Read a line of text from the console.")]
public class ReadLine : CodeActivity<string>
{
    /// <inheritdoc />
    public ReadLine([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line)
    {
    }

    /// <inheritdoc />
    public ReadLine(MemoryBlockReference output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(output, source, line)
    {
    }

    /// <inheritdoc />
    public ReadLine(Output<string>? output, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(output, source, line)
    {
    }

    /// <inheritdoc />
    protected override void Execute(ActivityExecutionContext context)
    {
        var provider = context.GetService<IStandardInStreamProvider>() ?? new StandardInStreamProvider(Console.In);
        var reader = provider.GetTextReader();
        var text = reader.ReadLine()!;
        context.Set(Result, text);
    }
}