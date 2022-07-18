using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Activities.Sql.Factory;
using Elsa.Activities.Sql.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Secrets.Extentions;
using Elsa.Secrets.Providers;
using Elsa.Services;
using Elsa.Services.Models;

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
    public class ExecuteSqlCommand : Activity, IActivityPropertyOptionsProvider, IRuntimeSelectListProvider
    {
        /// <summary>
        /// Allowed databases to run SQL
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Hint = "Allowed databases to run SQL.",
            Options = new[] { "", "MSSQLServer", "PostgreSql" },
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Database { get; set; }

        /// <summary>
        /// SQl script to execute
        /// </summary>
        [ActivityInput(
            Hint = "SQL command to execute",
            UIHint = ActivityInputUIHints.MultiLine,
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid, SyntaxNames.Sql }
        )]
        public string Command { get; set; } = default!;

        [ActivityInput(
            UIHint = ActivityInputUIHints.Dropdown,
            Label = "Connection string",
            OptionsProvider = typeof(ExecuteSqlCommand),
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
         )]
        public string? CredentialString { get; set; }

        [ActivityOutput] public int? Output { get; set; }

        private readonly ISqlClientFactory _sqlClientFactory;
        private readonly ISecretsProvider _secretsProvider;

        public ExecuteSqlCommand(ISqlClientFactory sqlClientFactory, ISecretsProvider secretsProvider) 
        {
            _sqlClientFactory = sqlClientFactory;
            _secretsProvider = secretsProvider;
        }

        public object GetOptions(PropertyInfo property) => new RuntimeSelectListProviderSettings(GetType());

        public async ValueTask<SelectList> GetSelectListAsync(object? context = default, CancellationToken cancellationToken = default)
        {
            var secretsPostgre = await _secretsProvider.GetSecretsForSelectListAsync(SecretType.PostgreSql);
            var secretsMssql = await _secretsProvider.GetSecretsForSelectListAsync(SecretType.MsSql);

            var items = secretsMssql.Select(x => new SelectListItem(x.Item1, x.Item2)).ToList();
            items.AddRange(secretsPostgre.Select(x => new SelectListItem(x.Item1, x.Item2)).ToList());
            items.Insert(0, new SelectListItem("", "empty"));

            var list = new SelectList { Items = items };

            return list;
        }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteCommand();

        private IActivityExecutionResult ExecuteCommand()
        {
            var sqlServerClient = _sqlClientFactory.CreateClient(new CreateSqlClientModel(Database, CredentialString));
            Output = sqlServerClient.ExecuteCommand(Command);

            return Done();
        }
    }
}
