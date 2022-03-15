namespace Elsa.Activities.Sql.Client
{
    public interface ISqlServerClient
    {
        public int ExecuteCommand(string sqlCommand);
        public string ExecuteQuery(string sqlQuery);
    }
}
