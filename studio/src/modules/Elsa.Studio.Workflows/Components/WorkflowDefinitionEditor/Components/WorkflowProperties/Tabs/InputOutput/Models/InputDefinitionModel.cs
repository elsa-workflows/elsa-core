using Elsa.Api.Client.Resources.StorageDrivers.Models;
using Elsa.Api.Client.Resources.WorkflowDefinitions.Models;

namespace Elsa.Studio.Workflows.Components.WorkflowDefinitionEditor.Components.WorkflowProperties.Tabs.InputOutput.Models;

/// <summary>
/// Represents a input definition model.
/// </summary>
public class InputDefinitionModel : ArgumentDefinitionModel
{
    /// <summary>
    /// Gets or sets the uihint.
    /// </summary>
    public UIHintDescriptor? UIHint { get; set; }

    /// <summary>
    /// Gets or sets the storage driver.
    /// </summary>
    public StorageDriverDescriptor StorageDriver { get; set; } = default!;
}