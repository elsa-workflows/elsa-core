using System.Reflection;

namespace Elsa.Workflows;

/// <summary>
/// Represents an interface for handling UI hint information and retrieving property UI handlers.
/// </summary>
public interface IUIHintHandler
{
    /// <summary>
    /// Gets the UI hint for the property.
    /// </summary>
    /// <remarks>
    /// The UI hint provides additional information to the UI framework regarding how to display and edit the property.
    /// </remarks>
    string UIHint { get; }

    /// <summary>
    /// Retrieves the UI handlers associated with a given property asynchronously.
    /// </summary>
    /// <param name="propertyInfo">The PropertyInfo object representing the property for which to retrieve the UI handlers.</param>
    /// <param name="cancellationToken">The cancellation token used to cancel the async operation.</param>
    /// <returns>A ValueTask representing the asynchronous operation. It resolves to an IEnumerable of IPropertyUIHandler objects representing the UI handlers associated with the specified property.</returns>
    ValueTask<IEnumerable<Type>> GetPropertyUIHandlersAsync(PropertyInfo propertyInfo, CancellationToken cancellationToken);
}