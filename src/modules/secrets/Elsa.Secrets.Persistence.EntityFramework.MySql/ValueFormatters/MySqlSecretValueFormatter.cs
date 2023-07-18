using Elsa.Secrets.Services;
using Elsa.Secrets.ValueFormatters;

namespace Elsa.Secrets.Persistence.EntityFramework.MySql.ValueFormatters
{
    public class MySqlSecretValueFormatter : SqlSecretValueFormatter
    {
        public override string Type => "MySQLServer";

        public MySqlSecretValueFormatter(ISecuredSecretService securedSecretService) : base(securedSecretService)
        {
        }
    }
}
