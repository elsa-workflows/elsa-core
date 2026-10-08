namespace Elsa.Logging.Console;

public sealed record JsonFormatterOptions
{
    public bool Indented { get; set; } = true;
}