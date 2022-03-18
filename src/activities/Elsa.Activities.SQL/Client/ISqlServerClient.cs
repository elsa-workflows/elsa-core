using System.Data;

namespace Elsa.Activities.Sql.Client
{
    public interface ISqlServerClient
    {
        public int ExecuteCommand(string sqlCommand);
        public DataSet ExecuteQuery(string sqlQuery);
    }
}
