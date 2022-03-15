using Elsa.Activities.ExecuteSqlServerQuery.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.ExecuteSqlServerQuery.Factory
{
    public interface ISqlClientFactory
    {
        ISqlServerClient CreateClient(string connectionString);
    }
}
