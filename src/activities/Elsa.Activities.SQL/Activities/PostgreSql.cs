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
using Elsa.Persistence.EntityFramework.Core.Services;
using Microsoft.EntityFrameworkCore;
using Elsa.Activities.SQL.Services;
using Elsa.Activities.SQL.Persistence;

namespace Elsa.Activities.SQL.Activities
{
    /// <summary>
    /// Stores a set of possible user actions and halts the workflow until one of the actions has been performed.
    /// </summary>
    [Trigger(
        Category = "Postgre Sql",
        Description = "Run SQL scripts",
        Outcomes = new string[0]
    )]
    public class PostgreSql : Activity
    {
        private readonly IContentSerializer _serializer;
        private readonly IConfiguration _configuration;

        [ActivityInput(
            Hint = "SQl script to execute",
            SupportedSyntaxes = new[] { SyntaxNames.JavaScript, SyntaxNames.Liquid }
        )]
        public string Query { get; set; } = default!;

        /// <summary>
        /// The ConnectionStrings to run SQL
        /// </summary>
        [ActivityInput(
            UIHint = ActivityInputUIHints.CheckList,
            Hint = "SQL databases to run query",
            Options = new[] { "PostgreSql", "MongoDb", "MySql", "Sqlite", "SqlServer" },
            DefaultSyntax = SyntaxNames.Json,
            SupportedSyntaxes = new[] { SyntaxNames.Json, SyntaxNames.JavaScript, SyntaxNames.Liquid })]

        public HashSet<string> Databases { get; set; } = new() { "PostgreSql" };

        [ActivityOutput] public int? Output { get; set; }

        public PostgreSql(IContentSerializer serializer, IConfiguration configuration, SqlActivityStore store)
        {
            _configuration = configuration;
            _serializer = serializer;
            _store = store;
        }

        private readonly SqlActivityStore _store;
        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context) => ExecuteQuery();

        private IActivityExecutionResult ExecuteQuery()
        {
            var connectionString = _configuration[$"ConnectionStrings:{Databases.First()}"];
            var context = _store.GetContext();


            var config = new QueryConfiguration(connectionString, Databases.First());
          //  var execution = new QueryExecution(config);

          //  Output = execution.Run();
             
            return Done();
        }
    }
}
