using Elsa.Secrets.Services;

namespace Elsa.Secrets.ValueFormatters
{
    public class MsSqlSecretValueFormatter : SqlSecretValueFormatter
    {
        public override string Type => "MSSQLServer";

        public MsSqlSecretValueFormatter(ISecuredSecretService securedSecretService) : base(securedSecretService)
        {
        }
    }
}