using FluentStorage.Model;

namespace Elsa.Extensions;

public static class BlobExtensions
{
    public static string GetExtension(this StoreObject blob) => Path.GetExtension(blob.Name).TrimStart('.');
}