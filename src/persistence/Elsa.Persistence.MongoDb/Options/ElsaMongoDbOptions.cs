namespace Elsa.Persistence.MongoDb.Options
{
    public class ElsaMongoDbOptions
    {
        public string? ConnectionString { get; set; }
        public string? DatabaseName { get; set; }

        /// <summary>
        /// This parameter to opt-out automatic registration of
        /// <see cref="Elsa.Persistence.MongoDb.Serializers.VariablesSerializer"/>
        /// </summary>
        public bool DoNotRegisterVariablesSerializer { get; set; }

        /// <summary>
        /// If true it will use the new LINQ3 provider introduced in MongoDB.Driver 2.19.0 but 
        /// it has some breaking changes, so it is preferibly to enable only if you are really sure
        /// that it works without any problem in your project.
        /// </summary>
        public bool UseNewLinq3Provider { get; set; }
    }
}
