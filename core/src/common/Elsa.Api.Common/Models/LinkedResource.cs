namespace Elsa.Models;

public record LinkedResource(Link[] Links)
{
    public LinkedResource() : this([]) { }
}

public record Link(string Href, string Rel, string Method);