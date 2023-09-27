using MongoDB.Driver;
using System;

namespace Elsa.Persistence.MongoDb.Options
{
    public class ElsaMongoDbOptions
    {
        public string? ConnectionString { get; set; }
        public string? DatabaseName { get; set; }

        /// <summary>
        /// This parameter to opt-out automatic registration of
        /// <see cref="Serializers.VariablesSerializer"/>
        /// </summary>
        public bool DoNotRegisterVariablesSerializer { get; set; }

        /// <summary>
        /// If true it will use the new LINQ3 provider introduced in MongoDB.Driver 2.19.0 but 
        /// it has some breaking changes, so it is preferibly to enable only if you are really sure
        /// that it works without any problem in your project.
        /// </summary>
        [Obsolete("Prefer to use the ConfigureMongoClientSettings")]
        public bool UseNewLinq3Provider { get; set; }

        /// <summary>
        /// Let the caller configure MongoClientSettings.
        /// </summary>
        public Action<MongoClientSettings> ConfigureMongoClientSettings { get; set; } = _ => { };

        /// <summary>
        /// As a general rule of thumb we should avoid creating more than one instance of MongoClient, the problem
        /// is that each instance has a Connection Pool and can put eccessive strain for the server. Giving the
        /// user the ability to create the MongoClient instance we can avoid this problem.
        /// </summary>
        public Func<MongoClientSettings, MongoUrl, IMongoClient> MongoClientFactory { get; set; } = (settings, _) => new MongoClient(settings);
    }
}
