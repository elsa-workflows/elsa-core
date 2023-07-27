namespace Elsa.Secrets.ValueFormatters
{
    public class MsSqlSecretValueFormatter : SqlSecretValueFormatter
    {
        public override string Type => "MSSQLServer";
    }
}