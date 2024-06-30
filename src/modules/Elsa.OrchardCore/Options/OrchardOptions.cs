namespace Elsa.OrchardCore.Options;

public class OrchardOptions
{
    public ISet<string> ContentTypes { get; set; } = new HashSet<string>();
}