using System;
using Microsoft.Extensions.Logging;

namespace Elsa.Alterations.Core.Models;

public record AlterationLogEntry(string AlterationBatchId, string AlterationId, string Message, LogLevel LogLevel, DateTimeOffset Timestamp);