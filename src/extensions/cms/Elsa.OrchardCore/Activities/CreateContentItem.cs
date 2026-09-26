using System.Text.Json.Nodes;
using Elsa.Expressions.Helpers;
using Elsa.Extensions;
using Elsa.OrchardCore.Client.Contracts;
using Elsa.OrchardCore.Client.Models;
using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;

namespace Elsa.OrchardCore.Activities;

/// <summary>
/// Represents an activity that creates a content item in an Orchard Core CMS using the specified content type, properties, and publish settings.
/// </summary>
/// <remarks>
/// This activity interacts with the IRestApiClient interface to send a request to create a new content item.
/// </remarks>
[Activity("OrchardCore", "Orchard Core", "Patches a content item", Kind = ActivityKind.Task)]
public class CreateContentItem : CodeActivity<JsonObject>
{
    /// <summary>
    /// Gets or sets the content type of the content item to create.
    /// </summary>
    /// <remarks>
    /// This property specifies the type of content, as defined in the Orchard Core CMS,
    /// that the activity will create. The content type must be valid and match a registered
    /// content definition within the system.
    /// </remarks>
    [Input(Description = "The content type of the content item to create.")]
    public Input<string> ContentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the properties to apply to the content item.
    /// </summary>
    /// <remarks>
    /// This property represents the additional data or attributes to assign to the content item.
    /// The properties are expected to be provided in a format compatible with the Orchard Core CMS content model.
    /// </remarks>
    [Input(Description = "The properties to apply to the content item.")]
    public Input<object> Properties { get; set; } = null!;

    /// <summary>
    /// Gets or sets a value indicating whether the content item should be published immediately after creation.
    /// </summary>
    /// <remarks>
    /// This property determines if the content item created by the activity will be published automatically.
    /// If set to true, the content item is published upon creation; otherwise, it remains in a draft state.
    /// </remarks>
    [Input(Description = "Whether to publish the content item.")]
    public Input<bool> Publish { get; set; } = null!;

    /// <inheritdoc />
    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var contentType = ContentType.Get(context);
        var publish = Publish.Get(context);
        var properties = Properties.Get(context).ConvertTo<JsonObject>()!;
        var apiClient = context.GetRequiredService<IRestApiClient>();
        var request = new CreateContentItemRequest
        {
            ContentType = contentType,
            Properties = properties,
            Publish = publish
        };
        var contentItem = await apiClient.CreateContentItemAsync(request, context.CancellationToken);
        context.SetResult(contentItem);
    }
}