namespace Elsa.Studio.Workflows.Designer.Models;

/// <summary>
/// Lean projection of an <c>Elsa.Api.Client.Resources.ActivityDescriptors.Models.ActivityDescriptor</c>
/// for the React-side activity picker. Keeps the JSInterop payload small
/// (descriptors carry a lot of metadata the picker doesn't need).
/// </summary>
public record ActivityDescriptorDto(
    string TypeName,
    int Version,
    string Name,
    string DisplayName,
    string? Category,
    string? Description,
    string Color,
    string? Icon);
