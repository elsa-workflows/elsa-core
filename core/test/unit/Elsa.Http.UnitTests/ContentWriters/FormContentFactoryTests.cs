using System.Text.Json;
using System.Text.Json.Nodes;
using Elsa.Http;
using Elsa.Http.ContentWriters;
using Microsoft.AspNetCore.Http;

namespace Elsa.Http.UnitTests.ContentWriters;

/// <summary>
/// Tests for the form content factories used by HTTP activities.
/// </summary>
public class FormContentFactoryTests
{
    [Fact]
    public async Task FormUrlEncoded_Dictionary_WritesFields()
    {
        var content = new Dictionary<string, object?>
        {
            ["name"] = "Ada Lovelace",
            ["note"] = null
        };
        var factory = new FormUrlEncodedHttpContentFactory();

        var httpContent = factory.CreateHttpContent(content, "application/x-www-form-urlencoded");
        var body = await httpContent.ReadAsStringAsync();

        Assert.Equal("application/x-www-form-urlencoded", httpContent.Headers.ContentType?.MediaType);
        AssertFormField(body, "name", "Ada Lovelace");
        AssertFormField(body, "note", "");
    }

    [Fact]
    public async Task FormUrlEncoded_Object_WritesProperties()
    {
        var factory = new FormUrlEncodedHttpContentFactory();

        var httpContent = factory.CreateHttpContent(new { name = "Ada", age = 36 }, "application/x-www-form-urlencoded");
        var body = await httpContent.ReadAsStringAsync();

        AssertFormField(body, "name", "Ada");
        AssertFormField(body, "age", "36");
    }

    [Fact]
    public async Task FormUrlEncoded_JsonString_WritesFields()
    {
        var factory = new FormUrlEncodedHttpContentFactory();

        var httpContent = factory.CreateHttpContent("  {\"name\":\"Ada\",\"age\":36}  ", "application/x-www-form-urlencoded");
        var body = await httpContent.ReadAsStringAsync();

        AssertFormField(body, "name", "Ada");
        AssertFormField(body, "age", "36");
    }

    [Fact]
    public async Task FormUrlEncoded_JsonObject_WritesScalarFields()
    {
        var content = new JsonObject
        {
            ["name"] = "Ada",
            ["active"] = true
        };
        var factory = new FormUrlEncodedHttpContentFactory();

        var httpContent = factory.CreateHttpContent(content, "application/x-www-form-urlencoded");
        var body = await httpContent.ReadAsStringAsync();

        AssertFormField(body, "name", "Ada");
        AssertFormField(body, "active", "True");
    }

    [Fact]
    public void FormUrlEncoded_NonObjectText_Throws()
    {
        var factory = new FormUrlEncodedHttpContentFactory();

        var exception = Assert.Throws<NotSupportedException>(() => factory.CreateHttpContent("[]", "application/x-www-form-urlencoded"));

        Assert.Contains("JSON object", exception.Message);
        Assert.Throws<JsonException>(() => factory.CreateHttpContent("{", "application/x-www-form-urlencoded"));
    }

    [Fact]
    public async Task Multipart_JsonObject_WritesTextParts()
    {
        var content = new JsonObject
        {
            ["name"] = "Ada",
            ["age"] = 36
        };
        var factory = new MultipartFormDataHttpContentFactory();

        var httpContent = factory.CreateHttpContent(content, "multipart/form-data");
        var parts = await ReadPartsAsync(httpContent);

        Assert.Contains("multipart/form-data", factory.SupportedContentTypes);
        Assert.Equal("multipart/form-data", httpContent.Headers.ContentType?.MediaType);
        Assert.Contains(httpContent.Headers.ContentType!.Parameters, parameter => parameter.Name == "boundary" && !string.IsNullOrEmpty(parameter.Value));
        Assert.Equal("Ada", parts["name"].Body);
        Assert.Null(parts["name"].FileName);
        Assert.Equal("36", parts["age"].Body);
    }

    [Fact]
    public async Task Multipart_FileValues_WritesFileParts()
    {
        var stream = new MemoryStream("stream-body"u8.ToArray());
        stream.Position = stream.Length;
        var formFileStream = new MemoryStream("form-file-body"u8.ToArray());
        var formFile = new FormFile(formFileStream, 0, formFileStream.Length, "attachment", "upload.txt")
        {
            Headers = new HeaderDictionary(),
            ContentType = "text/plain"
        };
        var content = new Dictionary<string, object>
        {
            ["title"] = "Report",
            ["blob"] = "hello"u8.ToArray(),
            ["data"] = stream,
            ["upload"] = new HttpFile(new MemoryStream("file-body"u8.ToArray()), "notes.txt", "text/plain"),
            ["attachment"] = formFile
        };
        var factory = new MultipartFormDataHttpContentFactory();

        var httpContent = factory.CreateHttpContent(content, "multipart/form-data");
        var parts = await ReadPartsAsync(httpContent);

        Assert.Equal("Report", parts["title"].Body);
        Assert.Null(parts["title"].FileName);
        Assert.Equal("hello", parts["blob"].Body);
        Assert.Equal("blob", parts["blob"].FileName);
        Assert.Equal("stream-body", parts["data"].Body);
        Assert.Equal("data", parts["data"].FileName);
        Assert.Equal("file-body", parts["upload"].Body);
        Assert.Equal("notes.txt", parts["upload"].FileName);
        Assert.Equal("text/plain", parts["upload"].MediaType);
        Assert.Equal("form-file-body", parts["attachment"].Body);
        Assert.Equal("upload.txt", parts["attachment"].FileName);
        Assert.Equal("text/plain", parts["attachment"].MediaType);
    }

    private static void AssertFormField(string body, string name, string expected)
    {
        var fields = body.Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(
                parts => Uri.UnescapeDataString(parts[0].Replace("+", " ", StringComparison.Ordinal)),
                parts => Uri.UnescapeDataString((parts.Length > 1 ? parts[1] : string.Empty).Replace("+", " ", StringComparison.Ordinal)));

        Assert.Equal(expected, fields[name]);
    }

    private static async Task<Dictionary<string, FormPart>> ReadPartsAsync(HttpContent content)
    {
        var multipart = Assert.IsType<MultipartFormDataContent>(content);
        var parts = new Dictionary<string, FormPart>();

        foreach (var part in multipart)
        {
            var name = Unquote(part.Headers.ContentDisposition?.Name);
            Assert.False(string.IsNullOrEmpty(name));
            parts[name] = new FormPart(
                NullIfEmpty(Unquote(part.Headers.ContentDisposition?.FileName)),
                part.Headers.ContentType?.MediaType,
                await part.ReadAsStringAsync());
        }

        return parts;
    }

    private static string Unquote(string? value) => value?.Trim('"') ?? string.Empty;

    private static string? NullIfEmpty(string? value) => string.IsNullOrEmpty(value) ? null : value;

    private sealed record FormPart(string? FileName, string? MediaType, string Body);
}
