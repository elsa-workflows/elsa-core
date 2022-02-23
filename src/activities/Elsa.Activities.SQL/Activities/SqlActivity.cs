using Elsa.Services;
using Elsa.Attributes;
using Elsa.Services.Models;
using Elsa.ActivityResults;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Activities.Sql.Client;

namespace Elsa.Activities.Sql.Activities
{
    /// <summary>
    /// Execute an SQL query on given database using connection string
    /// </summary>
    [Trigger(
        Category = "SQL",
        Description = "Run SQL scripts",
        Outcomes = new string[0]
    )]
    public class SqlActivity : Activity
    {
        [ActivityInput(
            Hint = "SQl script to execute",
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Query { get; set; } = default!;

        /// <summary>
        /// The ConnectionStrings to run SQL
        /// </summary>
        [ActivityInput(
            Hint = "The ConnectionStrings to run SQL",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string ConnectionString { get; set; } = default!;

        [ActivityOutput] public int? Output { get; set; }

        public SqlActivity() {}

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteQuery();

        private IActivityExecutionResult ExecuteQuery()
        {
            var sql = new SqlClient(ConnectionString);
            Output = sql.Execute(Query);

            return Done();
        }
    }
}
