using MailKit.Security;

namespace Elsa.Activities.Email.Options
{
    public class SmtpOptions
    {
        public string DefaultSender { get; set; }
        public SmtpDeliveryMethod DeliveryMethod { get; set; }
        public string PickupDirectoryLocation { get; set; }
        public string Host { get; set; }
        public int Port { get; set; } = 25;
        public SecureSocketOptions SecureSocketOptions { get; set; } = SecureSocketOptions.Auto;
        public bool RequireCredentials { get; set; }
        public bool UseDefaultCredentials { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}