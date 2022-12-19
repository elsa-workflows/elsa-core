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
    }
}
