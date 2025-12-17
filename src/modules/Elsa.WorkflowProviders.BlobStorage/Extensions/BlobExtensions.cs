using FluentStorage.Blobs;

namespace Elsa.Extensions;

public static class BlobExtensions
{
    public static string GetExtension(this Blob blob) => Path.GetExtension(blob.Name).TrimStart('.');
}