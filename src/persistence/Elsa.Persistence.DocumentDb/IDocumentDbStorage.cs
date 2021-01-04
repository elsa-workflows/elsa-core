using Microsoft.Azure.Documents.Client;
using System;
using System.Threading.Tasks;
using Elsa.Persistence.DocumentDb.Documents;

namespace Elsa.Persistence.DocumentDb
{
    public interface IDocumentDbStorage
    {
        string ToString();
        Task<DocumentClient> GetDocumentClient();
        Task<Uri> GetCollectionUriAsync<T>() where T : DocumentBase;
    }
}