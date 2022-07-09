using Elsa.Models;
using Newtonsoft.Json.Linq;

namespace Elsa.DataMasking.Core.Models;

public record HttpEndpointWorkflowJournalFilterContext(WorkflowExecutionLogRecord Record, JToken RequestModel, string Path, JToken Body, JToken RawBody) : WorkflowJournalFilterContext(Record);