namespace Elsa.Activities.Sql.Client
{
    public interface ISqlServerClient
    {
        public int Execute(string sqlCommand);
    }
}
