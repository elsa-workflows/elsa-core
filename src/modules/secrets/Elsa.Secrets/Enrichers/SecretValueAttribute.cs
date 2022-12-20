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
        /// Sets the DefaultSyntax value for the ActivityInput to <see cref="Elsa.Expressions.SyntaxNames.Secret">Secrets</see>
        /// </summary>
        public bool ApplySecretsSyntax { get; set;  } = true;

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
