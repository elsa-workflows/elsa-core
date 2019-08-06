using System.Threading;
using System.Threading.Tasks;
using Elsa.Persistence.YesSql.Indexes;
using Elsa.Runtime;
using YesSql;
using YesSql.Sql;

namespace Elsa.Persistence.YesSql.StartupTasks
{
    public class StoreInitializationTask : IStartupTask
    {
        private readonly IStore store;

        public StoreInitializationTask(IStore store)
        {
            this.store = store;
        }
        
        public Task ExecuteAsync(CancellationToken cancellationToken = default)
        {
            CreateTables();
            return Task.CompletedTask;
        }

        private void CreateTables()
        {
            using (var connection = store.Configuration.ConnectionFactory.CreateConnection())
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction(store.Configuration.IsolationLevel))
                {
                    new SchemaBuilder(store.Configuration, transaction, false)
                        .CreateMapIndexTable(nameof(WorkflowDefinitionIndex), table => table
                            .Column<string>("WorkflowDefinitionId")
                            .Column<int>("Version")
                            .Column<bool>("IsPublished")
                            .Column<bool>("IsLatest")
                        )
                        .CreateMapIndexTable(nameof(WorkflowDefinitionStartActivitiesIndex), table => table
                            .Column<string>("StartActivityId")
                            .Column<string>("StartActivityType")
                        )
                        .CreateMapIndexTable(nameof(WorkflowInstanceIndex), table => table
                            .Column<string>("WorkflowInstanceId")
                            .Column<string>("WorkflowDefinitionId")
                            .Column<string>("CorrelationId")
                            .Column<string>("WorkflowStatus")
                        )
                        .CreateMapIndexTable(nameof(WorkflowInstanceBlockingActivitiesIndex), table => table
                            .Column<string>("ActivityId")
                            .Column<string>("ActivityType")
                            .Column<string>("CorrelationId")
                        );

                    transaction.Commit();
                }
            }
        }
    }
}