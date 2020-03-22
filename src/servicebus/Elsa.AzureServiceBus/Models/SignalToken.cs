using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.AzureServiceBus.Models
{
    public class Signal
    {
        public Signal()
        {
        }

        public Signal(string name, string workflowInstanceId)
        {
            Name = name;
            WorkflowInstanceId = workflowInstanceId;
        }


        public string Name { get; set; }
        public string WorkflowInstanceId { get; set; }
    }
}
