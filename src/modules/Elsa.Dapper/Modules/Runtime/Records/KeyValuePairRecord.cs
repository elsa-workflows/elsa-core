using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

internal class KeyValuePairRecord : Record
{
    public string Value { get; set; } = null!;
}