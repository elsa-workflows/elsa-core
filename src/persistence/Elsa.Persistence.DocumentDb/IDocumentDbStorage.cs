using Microsoft.Azure.Documents.Client;
using System;
using System.Threading.Tasks;

namespace Elsa.Persistence.DocumentDb
{
    public interface IDocumentDbStorage
    {
        string ToString();
        Task<DocumentClient> GetDocumentClient();
        Task<(string Name, Uri Uri, string TenantId)> GetWorkflowDefinitionCollectionInfoAsync();
        Task<(string Name, Uri Uri, string TenantId)> GetWorkflowInstanceCollectionInfoAsync();
    }
}