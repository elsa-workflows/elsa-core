using System.Runtime.CompilerServices;
using Elsa.Workflows.Models;

namespace Elsa.Workflows.IntegrationTests.Activities.CollectionInputs;

public class DynamicArguments : CodeActivity
{
    public DynamicArguments([CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : base(source, line) { }

    public DynamicArguments(Dictionary<string, Input<object>> arguments, [CallerFilePath] string? source = default, [CallerLineNumber] int? line = default) : this(source, line) => Arguments = arguments;

    public Dictionary<string, Input<object>>? Arguments { get; set; } = default;

    protected override void Execute(ActivityExecutionContext context)
    {
        var provider = context.GetService<IStandardOutStreamProvider>() ?? new StandardOutStreamProvider(Console.Out);
        var textWriter = provider.GetTextWriter();

        if (Arguments != null)
        {
            foreach (var argument in Arguments)
            {
                var name = argument.Key;
                string value = context.Get(argument.Value) switch
                {
                    bool boolValue => $"{boolValue} (bool)",
                    string stringValue => $"{stringValue} (string)",
                    double doubleValue => $"{doubleValue} (double)",
                    _ => throw new NotImplementedException(),
                };

                textWriter.WriteLine($"{name}: {value}");
            }
        }
    }
}

