using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Persistence.MongoDb
{
    public class ElsaMongoDbOptions
    {
        public string? ConnectionString { get; set; }
        public string? Db { get; set; }
    }
}
