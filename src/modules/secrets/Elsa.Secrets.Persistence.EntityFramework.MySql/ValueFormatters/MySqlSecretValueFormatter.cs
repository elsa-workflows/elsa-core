using Elsa.Secrets.ValueFormatters;

namespace Elsa.Secrets.Persistence.EntityFramework.MySql.ValueFormatters
{
    public class MySqlSecretValueFormatter : SqlSecretValueFormatter
    {
        public override string Type => "MySQLServer";
    }
}
