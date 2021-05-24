using System;
using AutoFixture;
using Elsa.Testing.Shared.AutoFixture;
using Elsa.Testing.Shared.Helpers;

namespace Elsa.ComponentTests.Helpers
{
    public abstract class WorkflowsComponentTestBase : IDisposable
    {
        protected WorkflowsComponentTestBase(ElsaHostApplicationFactory hostApplicationFactory)
        {
            HostApplicationFactory = hostApplicationFactory;
            Fixture = new Fixture().Customize(new NodaTimeCustomization());
            TempFolder = new TemporaryFolder();
            hostApplicationFactory.SetDbConnectionString($@"Data Source={TempFolder.Folder}-elsa.db;Cache=Shared");
        }

        protected ElsaHostApplicationFactory HostApplicationFactory { get; }
        protected IFixture Fixture { get; }
        protected TemporaryFolder TempFolder { get; }
        
        public virtual void Dispose() => TempFolder.Dispose();
    }
}