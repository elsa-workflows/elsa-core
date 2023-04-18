using Elsa.Activities.Sql.Factory;
using Elsa.Activities.Sql.Models;
using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Expressions;
using Elsa.Metadata;
using Elsa.Services;
using Elsa.Services.Models;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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
            OptionsProvider = typeof(ExecuteSqlCommand),
            DefaultSyntax = SyntaxNames.Literal,
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string? Database { get; set; }

        /// <summary>
        /// Return options to be used by the designer. The designer will pass back whatever context is provided here.
        /// </summary>
        public object GetOptions(PropertyInfo property) => new RuntimeSelectListProviderSettings(GetType(), this._sqlClientFactory.Databases);


        public ValueTask<SelectList> GetSelectListAsync(object? context = null, CancellationToken cancellationToken = default)
        {
            var tList = (List<string>)context!;
            var items = tList.Select(x => new SelectListItem(x)).ToList();
            return new ValueTask<SelectList>(new SelectList(items));
        }

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
            Label = "Connection string",
            SupportedSyntaxes = new[] { SyntaxNames.Literal, SyntaxNames.JavaScript, SyntaxNames.Liquid }
         )]
        public string? ConnectionString { get; set; }

        [ActivityOutput] public int? Output { get; set; }

        private readonly ISqlClientFactory _sqlClientFactory;

        public ExecuteSqlCommand(ISqlClientFactory sqlClientFactory) => _sqlClientFactory = sqlClientFactory;

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteCommand();

        private IActivityExecutionResult ExecuteCommand()
        {
            var sqlServerClient = _sqlClientFactory.CreateClient(new CreateSqlClientModel(Database, ConnectionString));
            Output = sqlServerClient.ExecuteCommand(Command);

            return Done();
        }


    }
}
