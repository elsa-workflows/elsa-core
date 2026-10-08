namespace Elsa.Http.Contexts;

/// <summary>
/// Represents the context in which an HTTP response is being parsed.
/// </summary>
public record HttpResponseParserContext(Stream Content, string ContentType, Type? ReturnType, IDictionary<string, string[]> Headers, CancellationToken CancellationToken);