using System;

namespace Elsa.Secrets.Enrichers
{
    /// <summary>
    /// Attribute placed on activity input that needs to reference secret from credential manager.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SecretValueAttribute : Attribute
    {
        /// <summary>
        /// Secret Type (Ex: MSSQLServer, MySQLServer, AMPQ, etc...)
        /// </summary>
        public string Type { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type">Secret Type (Ex: MSSQLServer, MySQLServer, AMPQ, etc...)</param>
        public SecretValueAttribute(string type)
        {
            this.Type=type;
        }

    }
}
