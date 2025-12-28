namespace Elsa.Workflows.Contracts;

public interface IPayloadStorage
{
    string TypeIdentifier { get; }

    ValueTask<string> Get(Uri url, CancellationToken cancellationToken);

    ValueTask<Uri> Set(string name, string data, CancellationToken cancellationToken);
}
