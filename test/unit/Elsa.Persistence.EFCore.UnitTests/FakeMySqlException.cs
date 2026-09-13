namespace MySqlConnector;

internal sealed class MySqlException(int number) : Exception
{
    public int Number { get; } = number;
}
