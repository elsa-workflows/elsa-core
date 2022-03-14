using Elsa.Activities.OpcUa.Configuration;
using Opc.Ua;
using Opc.Ua.Client;
using Opc.Ua.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Elsa.Activities.OpcUa.Services
{
    public class Client : IClient
    {
        
        public OpcUaBusConfiguration Configuration { get; }
        private Session? _session;
        private Subscription? _subscription;
        private Func<MonitoredItem, CancellationToken, Task>? _handler;
        public Client(OpcUaBusConfiguration configuration)
        {
            Configuration = configuration;
            
        }

        public void SubscribeWithHandler(Func<MonitoredItem, CancellationToken, Task> handler)
        {
          
            if (_session != null) return;

            //Console.WriteLine("Step 1 - Create application configuration and certificate.");
            var config = new ApplicationConfiguration()
            {
                ApplicationName = Configuration.ClientId, 
                ApplicationUri = Utils.Format(@"urn:{0}:" + Configuration.ClientId, System.Net.Dns.GetHostName()),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = Configuration.ClientId },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = Configuration.OperationTimeout ?? 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = Configuration.SessionTimeout??60000 },
                TraceConfiguration = new TraceConfiguration()
            };
            config.Validate(ApplicationType.Client).GetAwaiter().GetResult();
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
            }

            var application = new ApplicationInstance
            {
                ApplicationName = Configuration.ClientId,
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = config
            };
            application.CheckApplicationInstanceCertificate(false, 2048).GetAwaiter().GetResult();

            //Configuration.ConnectionString
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(Configuration.ConnectionString, useSecurity: false);

            _session = Session.Create(config, new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(config)), false, "", (uint)Configuration.SessionTimeout, null, null).GetAwaiter().GetResult();

            //Console.WriteLine("Step 4 - Create a subscription. Set a faster publishing interval if you wish.");
            _subscription = new Subscription(_session.DefaultSubscription) { PublishingInterval = Configuration.PublishingInterval??1000 };

            //Console.WriteLine("Step 5 - Add a list of items you wish to monitor to the subscription.");
            var list = new List<MonitoredItem>() ;

            //list.Add(new MonitoredItem(_subscription.DefaultItem) { DisplayName = "ServerStatusCurrentTime", StartNodeId = "i=2258" });
            //var m = new MonitoredItem(_subscription.DefaultItem) { DisplayName = "ServerStatusCurrentTime", StartNodeId = "i=2258" };

            foreach (var tags in Configuration.Tags)
            {
                var m = new MonitoredItem(_subscription.DefaultItem) { DisplayName = tags.Value, StartNodeId = tags.Key};
                list.Add(m);
            }

            //Set notification.
            list.ForEach(i => i.Notification += OnNotification);
            _subscription.AddItems(list);

            //Console.WriteLine("Step 6 - Add the subscription to the session.");
            _session.AddSubscription(_subscription);
            _subscription.Create();


            _handler = handler;
        }

        private void OnNotification(MonitoredItem item, MonitoredItemNotificationEventArgs e)
        {
            //foreach (var value in item.DequeueValues())
            //{
            //    System.Console.WriteLine("{0}: {1}, {2}, {3}", item.DisplayName, value.Value, value.SourceTimestamp, value.StatusCode);
            //}

            if (_handler!=null)
                _handler(item,new CancellationToken());
        }



        public async Task PublishMessage(string message)
        {
            if (_session != null) return;

            //Console.WriteLine("Step 1 - Create application configuration and certificate.");
            var config = new ApplicationConfiguration()
            {
                ApplicationName = Configuration.ClientId,
                ApplicationUri = Utils.Format(@"urn:{0}:" + Configuration.ClientId, System.Net.Dns.GetHostName()),
                ApplicationType = ApplicationType.Client,
                SecurityConfiguration = new SecurityConfiguration
                {
                    ApplicationCertificate = new CertificateIdentifier { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\MachineDefault", SubjectName = Configuration.ClientId },
                    TrustedIssuerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Certificate Authorities" },
                    TrustedPeerCertificates = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\UA Applications" },
                    RejectedCertificateStore = new CertificateTrustList { StoreType = @"Directory", StorePath = @"%CommonApplicationData%\OPC Foundation\CertificateStores\RejectedCertificates" },
                    AutoAcceptUntrustedCertificates = true
                },
                TransportConfigurations = new TransportConfigurationCollection(),
                TransportQuotas = new TransportQuotas { OperationTimeout = Configuration.OperationTimeout ?? 15000 },
                ClientConfiguration = new ClientConfiguration { DefaultSessionTimeout = Configuration.SessionTimeout ?? 60000 },
                TraceConfiguration = new TraceConfiguration()
            };
            config.Validate(ApplicationType.Client).GetAwaiter().GetResult();
            if (config.SecurityConfiguration.AutoAcceptUntrustedCertificates)
            {
                config.CertificateValidator.CertificateValidation += (s, e) => { e.Accept = (e.Error.StatusCode == StatusCodes.BadCertificateUntrusted); };
            }

            var application = new ApplicationInstance
            {
                ApplicationName = Configuration.ClientId,
                ApplicationType = ApplicationType.Client,
                ApplicationConfiguration = config
            };
            application.CheckApplicationInstanceCertificate(false, 2048).GetAwaiter().GetResult();

            //Configuration.ConnectionString
            var selectedEndpoint = CoreClientUtils.SelectEndpoint(Configuration.ConnectionString, useSecurity: true);

            _session = Session.Create(config, new ConfiguredEndpoint(null, selectedEndpoint, EndpointConfiguration.Create(config)), false, "", (uint)Configuration.SessionTimeout, null, null).GetAwaiter().GetResult();

            // read a variable node from the OPC UA server (for example a variable node based on a complex type, contained in the sample OPC PLC provided by Microsoft)
            ExpandedNodeId nodeID = ExpandedNodeId.Parse(Configuration.Tags.FirstOrDefault().Key);
            VariableNode node = (VariableNode)_session.ReadNode(ExpandedNodeId.ToNodeId(nodeID, _session.NamespaceUris));

            
            // now that we have loaded the complex type, we can read the value
            DataValue value = _session.ReadValue(ExpandedNodeId.ToNodeId(nodeID, _session.NamespaceUris));

        }

        public void Dispose()
        {
            
        }

        public void StartClient()
        {
            //if (_bus?.Advanced.Workers.Count == 0)
            //    _bus.Advanced.Workers.SetNumberOfWorkers(1);
        }

        public void StopClient()
        {
            //if (_bus?.Advanced.Workers.Count == 1)
            //    _bus.Advanced.Workers.SetNumberOfWorkers(0);
        }
    }
}