using System.Reflection;
using Elsa.Http.ContentWriters;
using Elsa.Workflows.UIHints.Dropdown;

namespace Elsa.Http.UIHints;

/// <summary>
/// Provides options for the <see cref="SendHttpRequest"/> activity's <see cref="SendHttpRequest.ContentType"/> property.
/// </summary>
public class HttpContentTypeOptionsProvider : DropDownOptionsProviderBase
{
    private readonly IEnumerable<IHttpContentFactory> _httpContentFactories;

    /// <summary>
    /// Creates a new instance of the <see cref="HttpContentTypeOptionsProvider"/> class.
    /// </summary>
    public HttpContentTypeOptionsProvider(IEnumerable<IHttpContentFactory> httpContentFactories)
    {
        _httpContentFactories = httpContentFactories;
    }

    /// <inheritdoc />
    protected override ValueTask<ICollection<SelectListItem>> GetItemsAsync(PropertyInfo propertyInfo, object? context, CancellationToken cancellationToken)
    {
        var contentTypes = _httpContentFactories.SelectMany(x => x.SupportedContentTypes).Distinct().OrderBy(x => x).ToArray();
        var selectListItems = new List<SelectListItem> { new("", "") };

        selectListItems.AddRange(contentTypes.Select(x => new SelectListItem(x, x)));

        return new(selectListItems);
    }
}