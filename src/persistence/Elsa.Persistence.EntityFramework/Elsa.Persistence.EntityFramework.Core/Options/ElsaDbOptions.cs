namespace Elsa.Persistence.EntityFramework.Core.Options
{
    public class ElsaDbOptions
    {
        public ElsaDbOptions(string connectionString) => ConnectionString = connectionString;

        public string ConnectionString { get; }
    }
}
