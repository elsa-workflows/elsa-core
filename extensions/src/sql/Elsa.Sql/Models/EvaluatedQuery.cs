namespace Elsa.Sql.Models
{
    /// <summary>
    /// Represents a safely formatted SQL expression result.
    /// </summary>
    public class EvaluatedQuery
    {
        /// <summary>
        /// Query with parameterized values
        /// </summary>
        public string Query { get; set; }

        /// <summary>
        /// Parameters to inject into the query at execution
        /// </summary>
        public Dictionary<string, object?> Parameters { get; set; } = new Dictionary<string, object?>();

        /// <summary>
        /// An evaluated query response.
        /// </summary>
        /// <param name="query">The evaluated query</param>
        public EvaluatedQuery(string query) => Query = query;

        /// <summary>
        /// An evaluated query response.
        /// </summary>
        /// <param name="query">The evaluated query</param>
        /// <param name="parameters">Parameters to pass into the parameterized query</param>
        public EvaluatedQuery(string query, Dictionary<string, object?> parameters)
        {
            Query = query;
            Parameters = parameters;
        }
    }
}