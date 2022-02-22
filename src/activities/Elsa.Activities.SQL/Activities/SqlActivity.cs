using System;
using System.Collections.Generic;
using Elsa.Activities;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Elsa.Services;
using Elsa.Attributes;
using Elsa.Serialization;
using Elsa.Services.Models;
using Elsa.ActivityResults;
using Elsa.Design;
using Elsa.Expressions;
using Microsoft.Extensions.Configuration;
using Elsa.Activities.SQL.Configuration;
using Elsa.Activities.SQL.Client;

namespace Elsa.Activities.SQL.Activities
{
    /// <summary>
    /// Stores a set of possible user actions and halts the workflow until one of the actions has been performed.
    /// </summary>
    [Trigger(
        Category = "Sql",
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
