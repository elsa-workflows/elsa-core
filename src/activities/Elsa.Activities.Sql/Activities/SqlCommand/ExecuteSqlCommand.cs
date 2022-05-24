using Elsa.Services;
using Elsa.Attributes;
using Elsa.Services.Models;
using Elsa.ActivityResults;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Activities.Sql.Factory;
using Elsa.Activities.Sql.Models;

namespace Elsa.Activities.Sql.Activities
{
    /// <summary>
    /// Execute an SQL query on given database using connection string
    /// </summary>
    [Trigger(
        Category = "SQL",
        DisplayName = "Execute SQL Command",
        Description = "Execute given SQL command and returned number of rows affected",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ExecuteSqlCommand : Activity
    {
        /// <summary>
        /// Allowed databases to run SQL
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "Allowed databases to run SQL.",
            Options = new[] { "MSSQL Server", "PostgreSql" },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Database { get; set; }

        /// <summary>
        /// SQl script to execute
        /// </summary>
        [ActivityInput(
            Hint = "SQL command to execute",
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Command { get; set; } = default!;

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

        public ExecuteSqlCommand(ISqlClientFactory sqlClientFactory) 
        {
            _sqlClientFactory = sqlClientFactory;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteCommand();

        private IActivityExecutionResult ExecuteCommand()
        {
            var sqlServerClient = _sqlClientFactory.CreateClient(new CreateSqlClientModel(Database, ConnectionString));
            Output = sqlServerClient.ExecuteCommand(Command);

            return Done();
        }
    }
}
