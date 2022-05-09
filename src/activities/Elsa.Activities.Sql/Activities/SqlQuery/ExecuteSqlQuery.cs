using Elsa.Services;
using Elsa.Attributes;
using Elsa.Services.Models;
using Elsa.ActivityResults;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Activities.Sql.Factory;
using System.Data;
using Elsa.Activities.Sql.Models;
using Elsa.Providers.WorkflowStorage;

namespace Elsa.Activities.Sql.Activities
{
    /// <summary>
    /// Execute an SQL query on given database using connection string
    /// </summary>
    [Trigger(
        Category = "SQL",
        DisplayName = "Execute SQL Query",
        Description = "Execute given SQL query and returned execution result",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class ExecuteSqlQuery : Activity
    {
        /// <summary>
        /// Allowed databases to run SQL
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "Allowed databases to run SQL.",
            Options = new[] { "MSSQL Server", "PostgreSql" },
            DefaultValue = "MSSQL Server",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Database { get; set; } = "MSSQL Server";

        /// <summary>
        /// SQl script to execute
        /// </summary>
        [ActivityInput(
            Hint = "SQL query to execute",
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

        [ActivityOutput(DisableWorkflowProviderSelection = true, DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName)]
        public DataSet? Output { get; set; }

        private readonly ISqlClientFactory _sqlClientFactory;

        public ExecuteSqlQuery(ISqlClientFactory sqlClientFactory)
        {
            _sqlClientFactory = sqlClientFactory;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteQuery();

        private IActivityExecutionResult ExecuteQuery()
        {
            var sqlServerClient = _sqlClientFactory.CreateClient(new CreateSqlClientModel(Database, ConnectionString));
            Output = sqlServerClient.ExecuteQuery(Query);

            return Done();
        }
    }
}