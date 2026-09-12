using System.Text.Json;
using Elsa.Common.Serialization;
using Elsa.Testing.Shared;
using Elsa.Workflows.Options;

namespace Elsa.Workflows.Runtime.UnitTests.Serialization;

internal static class ActivationStrategyAliasTestHelpers
{
    public static SerializationTypeOptions CreateRegisteredOptions()
    {
        var options = new SerializationTypeOptions();
        WorkflowRuntimeTypeAliasRegistrar.Register(options, []);
        return options;
    }

    public static ISerializationTypeRegistry CreateRegistry() =>
        SerializationTypeTestHelpers.CreateRegistry(CreateRegisteredOptions());

    public static JsonSerializerOptions CreateJsonOptions() =>
        SerializationTypeTestHelpers.CreateTypeJsonOptions(CreateRegistry());
}
