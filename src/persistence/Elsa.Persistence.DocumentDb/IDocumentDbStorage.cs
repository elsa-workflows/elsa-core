using Microsoft.Azure.Documents.Client;
using System;
using System.Threading.Tasks;

namespace Elsa.Persistence.DocumentDb
{
    public interface IDocumentDbStorage
    {
        string ToString();
        Task<DocumentClient> GetDocumentClient();
        Uri GetWorkflowDefinitionCollectionUri();
        Uri GetWorkflowInstanceCollectionUri();
    }
}