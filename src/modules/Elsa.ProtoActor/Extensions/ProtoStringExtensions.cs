namespace Elsa.ProtoActor.Extensions;

internal static class ProtoStringExtensions
{
    public static string EmptyIfNull(this string? value) => value ?? "";
    public static string? NullIfEmpty(this string? value) => value == "" ? default : value;
}