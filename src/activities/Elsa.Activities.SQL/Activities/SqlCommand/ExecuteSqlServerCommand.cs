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
        Category = "SQL Server",
        DisplayName = "Execute SQL Command",
        Description = "Execute given SQL command and returned number of rows affected",
        Outcomes = new string[0]
    )]
    public class ExecuteSqlServerCommand : Activity
    {
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

        public ExecuteSqlServerCommand(ISqlClientFactory sqlClientFactory) 
        {
            _sqlClientFactory = sqlClientFactory;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteCommand();

        private IActivityExecutionResult ExecuteCommand()
        {
            var sqlServerClient = _sqlClientFactory.CreateClient(ConnectionString);
            Output = sqlServerClient.ExecuteCommand(Command);

            return Done();
        }
    }
}
