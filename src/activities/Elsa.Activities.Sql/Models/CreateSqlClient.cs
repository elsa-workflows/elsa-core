using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.Sql.Models
{
    public class CreateSqlClientModel
    {
        public CreateSqlClientModel(string database, string connectionString)
        {
            Database = database;
            ConnectionString = connectionString;
        }

        public string Database { get; private set; }
        public string ConnectionString { get; private set; }
    }
}
