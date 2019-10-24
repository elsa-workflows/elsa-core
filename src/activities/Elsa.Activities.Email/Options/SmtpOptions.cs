using System;
using System.Net.Mail;

namespace Elsa.Activities.Email.Options
{
    public class SmtpOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string DefaultSender { get; set; }
        public SmtpCredentials Credentials { get; set; }
        public TimeSpan? Timeout { get; set; }
        public SmtpDeliveryFormat? DeliveryFormat { get; set; }
        public SmtpDeliveryMethod? DeliveryMethod { get; set; }
        public bool? EnableSsl { get; set; }
        public string PickupDirectoryLocation { get; set; }
    }
}