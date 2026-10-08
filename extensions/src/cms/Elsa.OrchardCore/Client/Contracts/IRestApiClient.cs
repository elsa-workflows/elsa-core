using System.Text.Json.Nodes;
using Elsa.Http;
using Elsa.OrchardCore.Client.Models;

namespace Elsa.OrchardCore.Client.Contracts;

/// <summary>
/// Defines methods for interacting with a REST API for content management.
/// </summary>
public interface IRestApiClient
{
    /// <summary>
    /// Retrieves a content item from the content management system using its unique identifier.
    /// </summary>
    /// <param name="contentItemId">The unique identifier of the content item to retrieve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="JsonObject"/> containing the content item's data.</returns>
    Task<JsonObject> GetContentItemAsync(string contentItemId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing content item in the content management system by applying the specified patch details.
    /// </summary>
    /// <param name="contentItemId">The unique identifier of the content item to patch.</param>
    /// <param name="request">The request containing the patch data and publishing options for the content item.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="JsonObject"/> containing the updated content item's data after the patch is applied.</returns>
    Task<JsonObject> PatchContentItemAsync(string contentItemId, PatchContentItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a localized version of a content item for a specified culture using its unique identifier.
    /// </summary>
    /// <param name="contentItemId">The unique identifier of the content item to localize.</param>
    /// <param name="request">The request containing the culture code for localization.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="JsonObject"/> representing the localized content item's data.</returns>
    Task<JsonObject> LocalizeContentItemAsync(string contentItemId, LocalizeContentItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new content item in the content management system using the specified request data.
    /// </summary>
    /// <param name="request">The request containing details for the new content item, including its type, properties, and publish status.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="JsonObject"/> representing the newly created content item.</returns>
    Task<JsonObject> CreateContentItemAsync(CreateContentItemRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Uploads a collection of files to a specified folder in the media library.
    /// </summary>
    /// <param name="files">The collection of files to be uploaded.</param>
    /// <param name="folderPath">The optional path to the target folder in the media library. If not specified, files will be uploaded to the root folder.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="JsonObject"/> containing the result of the upload operation.</returns>
    Task<JsonObject> UploadFilesAsync(IEnumerable<HttpFile> files, string? folderPath = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves tags by retrieving their corresponding content items from the system.
    /// If any of the specified tags do not exist, they are created.
    /// </summary>
    /// <param name="request">The request containing the collection of tags to resolve.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="JsonObject"/> containing the resolved tags and their associated content items.</returns>
    Task<JsonObject> ResolveTagsAsync(ResolveTagsRequest request, CancellationToken cancellationToken = default);
}