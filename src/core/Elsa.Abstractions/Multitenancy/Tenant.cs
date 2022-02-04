namespace Elsa.Abstractions.MultiTenancy
{
    public class Tenant
    {
        public string Name { get; }
        public string Prefix { get; }
        public string ConnectionString { get; }
        public bool IsDefault { get; }

        public Tenant(string name, string prefix, string connectionString, bool isDefault = false)
        {
            Name = name;
            Prefix = prefix;
            ConnectionString = connectionString;
            IsDefault = isDefault;
        }
    }
}
