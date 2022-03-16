using Elsa.Services;
using Elsa.Attributes;
using Elsa.Services.Models;
using Elsa.ActivityResults;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Activities.Sql.Factory;

namespace Elsa.Activities.Sql.Activities
{
    /// <summary>
    /// Execute an SQL query on given database using connection string
    /// </summary>
    [Trigger(
        Category = "Execute Sql Query",
        Description = "Run SQL scripts",
        Outcomes = new string[0]
    )]
    public class ExecuteSqlServerQuery : Activity
    {
        /// <summary>
        /// SQl script to execute
        /// </summary>
        [ActivityInput(
            Hint = "SQL script to execute",
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Query { get; set; } = default!;

        /// <summary>
        /// Connection string to run SQL
        /// </summary>
        [ActivityInput(
            Hint = "Connection string to run SQL",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string ConnectionString { get; set; } = default!;

        [ActivityOutput] public int? Output { get; set; }

        private readonly ISqlClientFactory _sqlClientFactory;

        public ExecuteSqlServerQuery(ISqlClientFactory sqlClientFactory) 
        {
            _sqlClientFactory = sqlClientFactory;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteQuery();

        private IActivityExecutionResult ExecuteQuery()
        {
            var sqlServerClient = _sqlClientFactory.CreateClient(ConnectionString);
            Output = sqlServerClient.Execute(Query);

            return Done();
        }
    }
}
