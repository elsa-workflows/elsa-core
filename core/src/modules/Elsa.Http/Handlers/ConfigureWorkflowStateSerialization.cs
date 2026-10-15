using System.Text.Json;
using Elsa.Http.Serialization;
using Elsa.Workflows;
using Elsa.Workflows.State;

namespace Elsa.Http.Handlers;

/// <summary>
/// Configures the serialization of <see cref="WorkflowState"/> objects.
/// </summary>
public class ConfigureWorkflowStateSerialization : SerializationOptionsConfiguratorBase
{
    public override void Configure(JsonSerializerOptions options)
    {
        options.Converters.Add(new HttpStatusCodeCaseForWorkflowInstanceConverter());
    }
}