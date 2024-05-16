namespace Elsa.Models;

public record LinkedResource(List<Link> Links)
{
    public LinkedResource() : this(new List<Link>()) { }
}

public record Link(string Href, string Rel, string Method);