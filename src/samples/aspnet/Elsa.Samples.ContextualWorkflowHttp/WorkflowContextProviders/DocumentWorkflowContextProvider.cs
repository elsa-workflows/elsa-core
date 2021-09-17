using System.Threading;
using System.Threading.Tasks;
using Elsa.Samples.ContextualWorkflowHttp.Indexes;
using Elsa.Services;
using Elsa.Services.Models;
using YesSql;
using Document = Elsa.Samples.ContextualWorkflowHttp.Models.Document;
using IIdGenerator = Elsa.Services.IIdGenerator;

namespace Elsa.Samples.ContextualWorkflowHttp.WorkflowContextProviders
{
    public class DocumentWorkflowContextProvider : WorkflowContextRefresher<Document>
    {
        private readonly ISession _session;
        private readonly IIdGenerator _idGenerator;

        public DocumentWorkflowContextProvider(ISession session, IIdGenerator idGenerator)
        {
            _session = session;
            _idGenerator = idGenerator;
        }

        public override async ValueTask<Document?> LoadAsync(LoadWorkflowContext context, CancellationToken cancellationToken = default) =>
            await _session.Query<Document, DocumentIndex>(x => x.DocumentUid == context.ContextId).FirstOrDefaultAsync();

        public override ValueTask<string?> SaveAsync(SaveWorkflowContext<Document> context, CancellationToken cancellationToken = default)
        {
            var document = context.Context!;

            if (string.IsNullOrWhiteSpace(document.DocumentId))
                document.DocumentId = _idGenerator.Generate();

            _session.Save(document);
            return new ValueTask<string?>(document.DocumentId);
        }
    }
}