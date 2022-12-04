using Elsa.Secrets.ValueFormatters;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Secrets.Persistence.EntityFramework.MySql.ValueFormatters
{
    public class MySqlSecretValueFormatter : SqlSecretValueFormatter
    {
        public override string Type => "MySQLServer";
    }
}
