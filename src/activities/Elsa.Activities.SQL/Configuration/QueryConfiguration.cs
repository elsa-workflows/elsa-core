using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.SQL.Configuration
{
    public class QueryConfiguration
    {
        public string ConnectionString { get; }
        public string Query { get; }

        public QueryConfiguration(string connectionString, string query)
        {
            ConnectionString = connectionString;
            Query = query;
        }

        public override int GetHashCode()
        {
            return System.HashCode.Combine(ConnectionString, Query);
        }
    }
}
