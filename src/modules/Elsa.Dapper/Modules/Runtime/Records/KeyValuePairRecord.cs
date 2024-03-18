using Elsa.Dapper.Records;

namespace Elsa.Dapper.Modules.Runtime.Records;

internal class KeyValuePairRecord : Record
{
    public string Key { get; set; } = default!;
    public string Value { get; set; } = default!;
}