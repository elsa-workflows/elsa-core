using System.Collections.Generic;

namespace Elsa.ComponentTests.Helpers
{
    public static class FakeConfiguration
    {
        public static IEnumerable<KeyValuePair<string, string>> Create(string dbConnectionString)
        {
            return new Dictionary<string, string>
            {
                ["ConnectionStrings:Sqlite"] = dbConnectionString,
            };
        }
    }
}
