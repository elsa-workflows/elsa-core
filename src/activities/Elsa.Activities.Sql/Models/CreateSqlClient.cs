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
