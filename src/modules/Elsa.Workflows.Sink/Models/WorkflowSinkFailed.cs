using System;

namespace Elsa.Workflows.Sink.Models;

public class SinkExportFailed : Exception
{
    public SinkExportFailed(string message) : base(message)
    {
    }
}