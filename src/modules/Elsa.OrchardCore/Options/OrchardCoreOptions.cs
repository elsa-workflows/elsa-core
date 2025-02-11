namespace Elsa.OrchardCore;

public class OrchardCoreOptions
{
    public ISet<string> ContentTypes { get; set; } = new HashSet<string>();
}