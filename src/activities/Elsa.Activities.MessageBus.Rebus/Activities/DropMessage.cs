using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Attributes;
using Elsa.Design;
using Elsa.Results;
using Elsa.Services;
using Elsa.Services.Models;
using Rebus.Activation;
using Rebus.Bus;
using Rebus.Config;
using Rebus.Encryption;

namespace Elsa.Activities.Reflection.Activities
{
    /// <summary>
    /// Execute a Method by reflection.
    /// </summary>
    [ActivityDefinition(
        Category = "Messagebus",
        Description = "Drop a message to a bus (using nbus)",
        RuntimeDescription = "a => !!a.state.queue ? `Queue: <strong>${ a.state.queue }</strong><br />" +
        "Type: <strong>${ a.state.busType }</strong><br />` : 'Drop a message to a bus (using nbus)'",
        Outcomes = new[] { OutcomeNames.Done }
    )]
    public class DropMessage : Activity
    {

        public DropMessage()
        {

        }


        [ActivityProperty(Hint = "The variables to use as parameters, seperated by comma in order of method call.")]
        public string InputVariableName
        {
            get => GetState<string>(null, "InputVariableName");
            set => SetState(value, "InputVariableName");
        }

        [ActivityProperty(Hint = "Queue name for the message")]
        public string Queue
        {
            get => GetState<string>(null, "Queue");
            set => SetState(value, "Queue");
        }

        [ActivityProperty(Hint = "Queue encryption key for the message (untested!)")]
        public string EncryptionKey
        {
            get => GetState<string>(null, "EncryptionKey");
            set => SetState(value, "EncryptionKey");
        }

        [ActivityProperty(Hint = "Type of the bus",
                        Type = ActivityPropertyTypes.Select)]
        [SelectOptions("MSMQ", "RabbitMQ", "Azure Storage Queues", "Amazon SQS")]
        public string BusType
        {
            get => GetState<string>(null, "BusType");
            set => SetState(value, "BusType");
        }


        [ActivityProperty(Hint = "ConnectionString for the bus.")]
        public string ConnectionString
        {
            get => GetState<string>(null, "ConnectionString");
            set => SetState(value, "ConnectionString");
        }

        protected override async Task<ActivityExecutionResult> OnExecuteAsync(
            WorkflowExecutionContext context,
            CancellationToken cancellationToken)
        {

            RebusConfigurer configurer;
            using (var activator = new BuiltinHandlerActivator())
            {
                configurer = Configure.With(activator);

                if (BusType == "MSMQ")
                {
                    configurer = configurer.Transport(t => t.UseMsmq(Queue));
                }
                else if (BusType == "RabbitMQ")
                {
                    configurer = configurer.Transport(t => t.UseRabbitMq(ConnectionString, Queue));
                }
                else if (BusType == "Azure Service Bus")
                {
                    configurer = configurer.Transport(t => t.UseAzureStorageQueues(ConnectionString, Queue));
                }
                else if (BusType == "Amazon SQS")
                {
                    configurer = configurer.Transport(t => t.UseAzureStorageQueues(ConnectionString, Queue));
                }

                if (!string.IsNullOrEmpty(EncryptionKey))
                {
                    configurer = configurer.Options(t => t.EnableEncryption(EncryptionKey));
                }

                IBus bus = configurer.Start();

                object messageObject = context.GetVariable("Result");

                await bus.Send(messageObject);

            }

            return Outcome(OutcomeNames.Done);
        }


    }
}