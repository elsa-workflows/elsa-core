using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;

namespace Elsa.Persistence.DocumentDb
{

    public class DocumentDbStorageOptions
    {
        /// <summary>
        /// Gets or sets the name of the database.
        /// </summary>
        /// <value>
        /// The name of the database.
        /// </value>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the url of the DocumentDB.
        /// </summary>
        /// <value>
        /// The url of the DocumentDB.
        /// </value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the secret to access DocumentDB.
        /// </summary>
        /// <value>
        /// The secret to access DocumentDB.
        /// </value>
        public string AuthSecret { get; set; }

        /// <summary>
        /// Gets or sets the collection details under the DocumentDB.
        /// </summary>
        /// <value>
        /// The collection details.
        /// </value>
        public IDictionary<string, DocumentDbCollectionInfo> CollectionInfos { get; set; }

        /// <summary>
        /// Get or sets the request timeout for DocumentDB client. Default value set to 30 seconds
        /// </summary>
        public TimeSpan RequestTimeout { get; set; }

        /// <summary>
        /// Get or set the interval timespan to process expired entries. Default value 15 minutes
        /// Expired items under "locks", "jobs", "lists", "sets", "hashs", "counters/aggregated" will be checked 
        /// </summary>
        public TimeSpan ExpirationCheckInterval { get; set; }

        /// <summary>
        /// Get or sets the interval timespan to aggregated the counters. Default value 1 minute
        /// </summary>
        public TimeSpan CountersAggregateInterval { get; set; }

        /// <summary>
        /// Gets or sets the interval timespan to poll the queue for processing any new jobs. Default value 2 minutes
        /// </summary>
        public TimeSpan QueuePollInterval { get; set; }

        /// <summary>
        /// Gets or sets the connection mode for the DocumentDB client. Default value is Direct.
        /// </summary>
        public ConnectionMode ConnectionMode { get; set; }

        /// <summary>
        /// Gets or sets the connection protocol for the DocumentDB client. Default value is TCP.
        /// </summary>
        public Protocol ConnectionProtocol { get; set; }

        /// <summary>
        /// Create an instance of AzureDocumentDB Storage option with default values
        /// </summary>
        public DocumentDbStorageOptions()
        {
            RequestTimeout = TimeSpan.FromSeconds(30);
            ExpirationCheckInterval = TimeSpan.FromMinutes(2);
            CountersAggregateInterval = TimeSpan.FromMinutes(2);
            QueuePollInterval = TimeSpan.FromSeconds(15);
            ConnectionMode = ConnectionMode.Direct;
            ConnectionProtocol = Protocol.Tcp;
        }
    }
}
