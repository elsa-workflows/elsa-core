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
using Elsa.Secrets.Manager;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using Elsa.Metadata;
using Elsa.Secrets.Providers;

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
    public class ExecuteSqlQuery : Activity, IActivityPropertyOptionsProvider, IRuntimeSelectListProvider
    {
        /// <summary>
        /// Allowed databases to run SQL
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "Allowed databases to run SQL.",
            Options = new[] { "", "MSSQLServer", "PostgreSql" },
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
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Sql }
        )]
        public string Query { get; set; } = default!;

        [ActivityInput(
              UIHint = ActivityInputUIHints.Dropdown,
              Label = "Credentials string",
              Hint = "Secret stored in credential manager",
              OptionsProvider = typeof(ExecuteSqlQuery),
              SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
           )]
        public string? CredentialString { get; set; }

        [ActivityOutput(DisableWorkflowProviderSelection = true, DefaultWorkflowStorageProvider = TransientWorkflowStorageProvider.ProviderName)]
        public DataSet? Output { get; set; }

        private readonly ISqlClientFactory _sqlClientFactory;
        private readonly ISecretsProvider _secretsProvider;

        public ExecuteSqlQuery(ISqlClientFactory sqlClientFactory, ISecretsProvider secretsProvider) 
        {
            _sqlClientFactory = sqlClientFactory;
            _secretsProvider = secretsProvider;
        }

        public object GetOptions(PropertyInfo property) => new RuntimeSelectListProviderSettings(GetType());

        public async ValueTask<SelectList> GetSelectListAsync(object? context = default, CancellationToken cancellationToken = default)
        {
            var secretsPostgre = await _secretsProvider.GetSecrets("PostgreSql", ":");
            var secretsMssql = await _secretsProvider.GetSecrets("MSSQLServer", ":");

            var items = secretsMssql.Select(x => new SelectListItem(x)).ToList();
            items.AddRange(secretsPostgre.Select(x => new SelectListItem(x)).ToList());
            items.Insert(0, new SelectListItem("", "empty"));

            var list = new SelectList { Items = items };

            return list;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteQuery();

        private IActivityExecutionResult ExecuteQuery()
        {
            var sqlServerClient = _sqlClientFactory.CreateClient(new CreateSqlClientModel(Database, CredentialString));
            Output = sqlServerClient.ExecuteQuery(Query);

            return Done();
        }
    }
}