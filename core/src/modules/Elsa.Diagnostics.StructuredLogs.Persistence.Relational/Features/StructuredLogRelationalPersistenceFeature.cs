using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Extensions;
using Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Options;
using Elsa.Features.Abstractions;
using Elsa.Features.Services;

namespace Elsa.Diagnostics.StructuredLogs.Persistence.Relational.Features;

public class StructuredLogRelationalPersistenceFeature(IModule module) : FeatureBase(module)
{
    public Action<RelationalStructuredLogOptions>? ConfigureOptions { get; set; }

    public override void Apply()
    {
        Services.AddRelationalStructuredLogPersistence(ConfigureOptions);
    }
}
