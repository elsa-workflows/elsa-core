using Elsa.Persistence;

namespace Elsa
{
    public class Migrations : DataMigration
    {
        public int Create()
        {
            //SchemaBuilder.CreateMapIndexTable<>()
            return 1;
        }
    }
}