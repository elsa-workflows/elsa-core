using System;
using System.Collections.Generic;
using System.Linq;
using Elsa.Serialization.Formatters;

namespace Elsa.Serialization
{
    public class TokenFormatterProvider : ITokenFormatterProvider
    {
        private readonly IEnumerable<ITokenFormatter> formatters;

        public TokenFormatterProvider(IEnumerable<ITokenFormatter> formatters)
        {
            this.formatters = formatters;
        }
        
        public ITokenFormatter GetFormatter(string format)
        {
            return formatters.Single(x => string.Equals(x.Format, format, StringComparison.OrdinalIgnoreCase));
        }
    }
}