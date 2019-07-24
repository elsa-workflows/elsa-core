namespace Elsa.Persistence.YesSql.Options
{
    public class YesSqlOptions
    {
        public string ConnectionString { get; set; }
        public DatabaseProvider DatabaseProvider { get; set; }
        public string TablePrefix { get; set; }
    }
}