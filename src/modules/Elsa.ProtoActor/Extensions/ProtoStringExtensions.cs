namespace Elsa.ProtoActor.Extensions;

public static class ProtoStringExtensions
{
    public static string EmptyIfNull(this string? value) => value ?? "";
    public static string? NullIfEmpty(this string value) => value == "" ? default : value;
}