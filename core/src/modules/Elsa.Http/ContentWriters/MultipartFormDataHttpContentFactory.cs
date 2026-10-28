using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.ContentWriters;

/// <summary>
/// Writes <c>multipart/form-data</c> content with one part per field.
/// </summary>
/// <remarks>
/// Accepts a dictionary, a JSON object, a JSON object string, or any other object.
/// Scalar values are text parts. Dictionary values that are a byte array, <see cref="Stream"/>, <see cref="HttpFile"/>, or <see cref="IFormFile"/> are file parts.
/// </remarks>
public class MultipartFormDataHttpContentFactory : IHttpContentFactory
{
    /// <inheritdoc />
    public IEnumerable<string> SupportedContentTypes => ["multipart/form-data"];

    /// <inheritdoc />
    public HttpContent CreateHttpContent(object content, string? contentType = null)
    {
        var multipartContent = new MultipartFormDataContent();

        foreach (var (name, value) in FormContentReader.ReadValues(content))
            AddPart(multipartContent, name, value);

        return multipartContent;
    }

    private static void AddPart(MultipartFormDataContent multipartContent, string name, object? value)
    {
        switch (value)
        {
            case byte[] bytes:
                multipartContent.Add(new ByteArrayContent(bytes), name, name);
                return;
            case Stream stream:
                AddFile(multipartContent, name, stream, name, contentType: null);
                return;
            case HttpFile file:
                AddFile(multipartContent, name, file.Stream, string.IsNullOrWhiteSpace(file.Filename) ? name : file.Filename, file.ContentType);
                return;
            case IFormFile formFile:
                AddFile(
                    multipartContent,
                    name,
                    formFile.OpenReadStream(),
                    string.IsNullOrWhiteSpace(formFile.FileName) ? name : formFile.FileName,
                    formFile.ContentType);
                return;
            default:
                multipartContent.Add(new StringContent(FormContentReader.ToFormString(value)), name);
                return;
        }
    }

    private static void AddFile(MultipartFormDataContent multipartContent, string name, Stream stream, string fileName, string? contentType)
    {
        if (stream.CanSeek)
            stream.Position = 0;

        var fileContent = new StreamContent(stream);

        if (!string.IsNullOrWhiteSpace(contentType))
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);

        multipartContent.Add(fileContent, name, fileName);
    }
}
