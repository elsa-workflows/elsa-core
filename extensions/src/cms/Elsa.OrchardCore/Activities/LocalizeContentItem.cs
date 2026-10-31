using System.Text.Json.Nodes;
using Elsa.Extensions;
using Elsa.OrchardCore.Client.Contracts;
using Elsa.OrchardCore.Client.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

/// <summary>
/// Represents an activity for localizing a content item in Orchard Core.
/// </summary>
/// <remarks>
/// This activity is used to create a localized version of a content item in a specified culture. It interacts with an
/// Orchard Core API client to perform the localization action and returns the localized content item as a result.
/// </remarks>
[Activity("OrchardCore", "Orchard Core", "Localizes a content item", Kind = ActivityKind.Task)]
public class LocalizeContentItem : CodeActivity<JsonObject>
{
    /// <summary>
    /// Gets or sets the unique identifier of the content item to be localized.
    /// This identifier is required to interact with the Orchard Core API for performing
    /// localization actions on the specified content item.
    /// </summary>
    [Input(Description = "The ID of the content item to localize.")]
    public Input<string> ContentItemId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the culture code to be used when creating a localized version of a content item.
    /// This property determines the language and regional formatting for the localized content.
    /// </summary>
    [Input(Description = "The culture code to use when creating a localized version.")]
    public Input<string> CultureCode { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var contentItemId = ContentItemId.Get(context);
        var cultureCode = CultureCode.Get(context);
        var apiClient = context.GetRequiredService<IRestApiClient>();
        var request = new LocalizeContentItemRequest
        {
            CultureCode = cultureCode
        };
        var localizedContentItem = await apiClient.LocalizeContentItemAsync(contentItemId, request, context.CancellationToken);
        context.SetResult(localizedContentItem);
    }
}