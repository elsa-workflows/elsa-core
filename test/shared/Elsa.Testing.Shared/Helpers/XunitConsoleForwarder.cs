using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit.Abstractions;

namespace Elsa.Testing.Shared.Helpers
{
    public class XunitConsoleForwarder : TextWriter
    {
        private readonly ITestOutputHelper _output;
        private IList<char> _line = new List<char>();
        public XunitConsoleForwarder(ITestOutputHelper output) => _output = output;
        public override Encoding Encoding => Console.Out.Encoding;

        public override void Write(char value)
        {
            if (value == '\n')
            {
                FlushLine();
                _line = new List<char>();
                return;
            }

            _line.Add(value);
        }

        protected override void Dispose(bool disposing)
        {
            if (_line.Count > 0)
                FlushLine();
            base.Dispose(disposing);
        }

        private void FlushLine()
        {
            if (_line.Count > 0 && _line.Last() == '\r')
                _line.RemoveAt(_line.Count - 1);

            _output.WriteLine(new string(_line.ToArray()));
        }
    }
}