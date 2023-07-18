using Elsa.Secrets.Services;

namespace Elsa.Secrets.ValueFormatters
{
    public class PostgreSqlSecretValueFormatter : SqlSecretValueFormatter
    {
        public override string Type => "PostgreSql";

        public PostgreSqlSecretValueFormatter(ISecuredSecretService securedSecretService) : base(securedSecretService)
        {
        }
    }
}
