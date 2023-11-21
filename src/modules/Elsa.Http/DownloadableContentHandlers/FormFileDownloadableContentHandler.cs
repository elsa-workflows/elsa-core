using Elsa.Http.Abstractions;
using Elsa.Http.Contexts;
using Elsa.Http.Models;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.DownloadableContentHandlers;

/// <summary>
/// Handles content that represents a downloadable stream.
/// </summary>
public class FormFileDownloadableContentHandler : DownloadableContentHandlerBase
{
    /// <inheritdoc />
    public override bool GetSupportsContent(object content) => content is IFormFile;
    
    /// <inheritdoc />
    protected override Downloadable GetDownloadable(DownloadableContext context)
    {
        var file = (IFormFile)context.Content;
        var stream = file.OpenReadStream();
        var fileName = Path.GetFileName(file.FileName);
        var contentType = file.ContentType;
        return new Downloadable(stream, fileName, contentType);
    }
}