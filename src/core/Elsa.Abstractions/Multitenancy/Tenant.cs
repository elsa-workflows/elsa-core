namespace Elsa.Abstractions.MultiTenancy
{
    public class Tenant
    {
        public string Name { get; }
        public string Prefix { get; }
        public string ConnectionString { get; }

        public Tenant(string name, string prefix, string connectionString)
        {
            Name = name;
            Prefix = prefix;
            ConnectionString = connectionString;
        }
    }
}
