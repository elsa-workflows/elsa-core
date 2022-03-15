using Elsa.Activities.ExecuteSqlServerQuery.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.ExecuteSqlServerQuery.Factory
{
    public class SqlClientFactory : ISqlClientFactory
    {
        public ISqlServerClient CreateClient(string connectionString)
        { 
            return new SqlServerClient(connectionString);
        }
    }
}
