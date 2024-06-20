using System.Runtime.CompilerServices;
using Elsa.Workflows.Contracts;
using Elsa.Workflows.Models;
using Elsa.Workflows.Services;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionInputs;

public class WriteMultiLine : CodeActivity
{
    public WriteMultiLine([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line) { }


    public WriteMultiLine(List<Input<string>> text, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => Text = text;

    public List<Input<string>>? Text { get; set; } = default;

    protected override void Execute(ActivityExecutionContext context)
    {
        var provider = context.GetService<IStandardOutStreamProvider>() ?? new StandardOutStreamProvider(Console.Out);
        var textWriter = provider.GetTextWriter();

        Text?.ForEach(t => {
            var text = context.Get(t);
            textWriter.WriteLine(text);
        });
    }
}

