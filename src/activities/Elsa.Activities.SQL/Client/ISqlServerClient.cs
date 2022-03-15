using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Elsa.Activities.ExecuteSqlServerQuery.Client
{
    public interface ISqlServerClient
    {
        public int Execute(string sqlCommand);
    }
}
