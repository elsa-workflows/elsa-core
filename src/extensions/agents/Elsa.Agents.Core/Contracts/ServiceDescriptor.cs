using Microsoft.SemanticKernel;

namespace Elsa.Agents;

/// <summary>
/// Represents a service descriptor used to define and configure services in the context of the Semantic Kernel framework.
/// </summary>
/// <remarks>
/// This class encapsulates the name of the service and an action to configure the kernel builder with the specific service.
/// It is typically used to register and customize services for use within agents or features.
/// </remarks>
public class ServiceDescriptor
{
    /// <summary>
    /// Gets or sets the name of the service.
    /// </summary>
    /// <remarks>
    /// The name is used to identify the service descriptor and can be helpful for distinguishing
    /// between different services when configuring or resolving dependencies within the Semantic Kernel framework.
    /// </remarks>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Gets or sets the action used to configure the kernel builder for a specific service.
    /// </summary>
    /// <remarks>
    /// This property defines an <see cref="Action{IKernelBuilder}"/> used to customize the Semantic Kernel's configuration
    /// by adding or modifying services within the kernel. It provides a mechanism to integrate and set up specific
    /// functionality in the context of agents or features.
    /// </remarks>
    public Action<IKernelBuilder> ConfigureKernel { get; set; } = null!;
}