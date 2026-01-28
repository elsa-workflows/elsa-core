using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Elsa.Models;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.Core.Services;
using Elsa.Persistence.Specifications;
using Elsa.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Elsa.Persistence.EntityFramework.Core.Stores
{
    public class EntityFrameworkWorkflowInstanceStore : ElsaContextEntityFrameworkStore<WorkflowInstance>, IWorkflowInstanceStore
    {
        private readonly IContentSerializer _contentSerializer;
        private readonly ILogger<EntityFrameworkWorkflowInstanceStore> _logger;

        public EntityFrameworkWorkflowInstanceStore(
            IElsaContextFactory dbContextFactory,
            IMapper mapper,
            IContentSerializer contentSerializer,
            ILogger<EntityFrameworkWorkflowInstanceStore> logger) : base(dbContextFactory, mapper, logger)
        {
            _contentSerializer = contentSerializer;
            _logger = logger;
        }

        public override async Task DeleteAsync(WorkflowInstance entity, CancellationToken cancellationToken = default)
        {
            var workflowInstanceId = entity.Id;

            await DoWork(async dbContext =>
            {
                await dbContext.Set<WorkflowExecutionLogRecord>().AsQueryable().Where(x => x.WorkflowInstanceId == workflowInstanceId).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<Bookmark>().AsQueryable().Where(x => x.WorkflowInstanceId == workflowInstanceId).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<WorkflowInstance>().AsQueryable().Where(x => x.Id == workflowInstanceId).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
            }, cancellationToken);
        }

        public override async Task<int> DeleteManyAsync(ISpecification<WorkflowInstance> specification, CancellationToken cancellationToken = default)
        {
            var workflowInstanceIds = (await FindManyAsync<string>(specification, (wf) => wf.Id, cancellationToken: cancellationToken)).ToList();
            await DeleteManyByIdsAsync(workflowInstanceIds, cancellationToken);
            return workflowInstanceIds.Count;
        }

        public async Task DeleteManyByIdsAsync(IEnumerable<string> ids, CancellationToken cancellationToken = default)
        {
            var idList = ids.ToList();

            if (!idList.Any())
                return;

            await DoWork(async dbContext =>
            {
                await dbContext.Set<WorkflowExecutionLogRecord>().AsQueryable().Where(x => idList.Contains(x.WorkflowInstanceId)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<Bookmark>().AsQueryable().Where(x => idList.Contains(x.WorkflowInstanceId)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
                await dbContext.Set<WorkflowInstance>().AsQueryable().Where(x => idList.Contains(x.Id)).BatchDeleteWithWorkAroundAsync(dbContext, cancellationToken);
            }, cancellationToken);
        }

        protected override Expression<Func<WorkflowInstance, bool>> MapSpecification(ISpecification<WorkflowInstance> specification)
        {
            return AutoMapSpecification(specification);
        }

        protected override void OnSaving(ElsaContext dbContext, WorkflowInstance entity)
        {
            var data = new
            {
                entity.Input,
                entity.Output,
                entity.Variables,
                entity.ActivityData,
                entity.Metadata,
                entity.BlockingActivities,
                entity.ScheduledActivities,
                entity.Scopes,
                entity.Fault,
                entity.Faults,
                entity.CurrentActivity
            };

            var json = _contentSerializer.Serialize(data);
            string compressed = Compress(json);

            dbContext.Entry(entity).Property("Data").CurrentValue = compressed;
        }

        protected override void OnLoading(ElsaContext dbContext, WorkflowInstance entity)
        {
            var data = new
            {
                entity.Input,
                entity.Output,
                entity.Variables,
                entity.ActivityData,
                entity.Metadata,
                entity.BlockingActivities,
                entity.ScheduledActivities,
                entity.Scopes,
                entity.Fault,
                entity.Faults,
                entity.CurrentActivity
            };

            var jsonValue = (string)dbContext.Entry(entity).Property("Data").CurrentValue;
            string json = "";
            
            if (!string.IsNullOrEmpty(jsonValue))
            {
                if (jsonValue.StartsWith("{"))
                {
                    json = jsonValue;
                }
                else
                {
                    json = Decompress(jsonValue);
                }
            }

            if (!string.IsNullOrWhiteSpace(json))
            {
                try
                {
                    data = JsonConvert.DeserializeAnonymousType(json, data, DefaultContentSerializer.CreateDefaultJsonSerializationSettings())!;
                }
                catch (JsonSerializationException e)
                {
                    _logger.LogWarning(e, "Failed to deserialize workflow instance JSON data");
                }
            }

            entity.Input = data.Input;
            entity.Output = data.Output;
            entity.Variables = data.Variables;
            entity.ActivityData = data.ActivityData;
            entity.Metadata = data.Metadata;
            entity.BlockingActivities = data.BlockingActivities;
            entity.ScheduledActivities = data.ScheduledActivities;
            entity.Scopes = data.Scopes;
            entity.Fault = data.Fault;
            entity.Faults = data.Faults;
            entity.CurrentActivity = data.CurrentActivity;
        }

        public static string Decompress(string input)
        {
            byte[] compressed = Convert.FromBase64String(input);
            byte[] decompressed = Decompress(compressed);
            return Encoding.UTF8.GetString(decompressed);
        }

        public static string Compress(string input)
        {
            byte[] encoded = Encoding.UTF8.GetBytes(input);
            byte[] compressed = Compress(encoded);
            return Convert.ToBase64String(compressed);
        }

        public static byte[] Decompress(byte[] input)
        {
            using (var memoryStream = new MemoryStream(input))
            {
                using (var outputStream = new MemoryStream())
                {
                    using (var decompressStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                    {
                        decompressStream.CopyTo(outputStream);
                    }
                    return outputStream.ToArray();
                }
            }
        }

        public static byte[] Compress(byte[] input)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var gzipStream = new GZipStream(memoryStream, CompressionLevel.Optimal))
                {
                    gzipStream.Write(input, 0, input.Length);
                }
                return memoryStream.ToArray();
            }
        }
    }
}