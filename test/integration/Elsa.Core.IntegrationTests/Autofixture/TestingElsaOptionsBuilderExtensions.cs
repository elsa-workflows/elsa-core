using Elsa.Activities.UserTask.Activities;
using Elsa.Core.IntegrationTests.Workflows;

namespace Elsa.Core.IntegrationTests.Autofixture
{
    public static class TestingElsaOptionsExtensions
    {
        /// <summary>
        /// Adds the minimal activities and workflow applicable to running the <see cref="PersistableWorkflow"/>.
        /// </summary>
        /// <param name="elsa">An elsa options</param>
        /// <returns>The same else options, so calls may be chained</returns>
        public static ElsaOptions AddPersistableWorkflow(this ElsaOptions elsa)
        {
            elsa.AddActivity<UserTask>();
            elsa.AddWorkflow<PersistableWorkflow>();
            return elsa;
        }
    }
}