using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Workflows.Models;

/// <summary>
/// Associates a converter descriptor with its keyed dependency registration.
/// </summary>
public sealed record OutputConverterRegistration(
    OutputConverterDescriptor Descriptor,
    string ServiceKey,
    ServiceLifetime ServiceLifetime);
