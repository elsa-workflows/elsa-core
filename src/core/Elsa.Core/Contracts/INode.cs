namespace Elsa.Contracts;

public interface INode
{
    string Id { get; set; }
    string NodeType { get; set; }
    IDictionary<string, object> Metadata { get; set; }
}