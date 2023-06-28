namespace Elsa.Api.Client.Contracts;

/// <summary>
/// Resolves the .NET type of an activity type name.
/// </summary>
public interface IActivityTypeService
{
    /// <summary>
    /// Resolves the .NET type of the specified activity type name.
    /// </summary>
    /// <param name="activityTypeName">The activity type name.</param>
    /// <returns>Returns the .NET type of the specified activity type name.</returns>
    Type ResolveType(string activityTypeName);
}