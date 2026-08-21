using Elsa.Workflows.Models;

namespace Elsa.Workflows.Core.UnitTests.OutputConverters.Fixtures;

public sealed class ReferenceOutputConverter : IOutputConverter
{
    public Guid InstanceId { get; } = Guid.NewGuid();

    public object? Convert(OutputConversionContext context)
    {
        var prefix = context.Settings?.TryGetProperty("prefix", out var prefixElement) == true
            ? prefixElement.GetString()
            : null;

        return $"{prefix}{context.Value}";
    }
}
