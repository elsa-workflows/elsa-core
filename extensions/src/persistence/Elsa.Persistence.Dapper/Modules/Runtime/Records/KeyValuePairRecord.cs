using Elsa.Persistence.Dapper.Records;

namespace Elsa.Persistence.Dapper.Modules.Runtime.Records;

internal class KeyValuePairRecord : Record
{
    public string Value { get; set; } = null!;
}