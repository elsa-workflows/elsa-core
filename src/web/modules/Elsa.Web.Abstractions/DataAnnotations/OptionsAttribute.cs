using System;

namespace Elsa.Web.DataAnnotations
{
    public class OptionsAttribute : Attribute
    {
        public OptionsAttribute(string optionLabel, params string[] options)
        {
            OptionLabel = optionLabel;
            Options = options;
        }

        public string OptionLabel { get; }
        public string[] Options { get; }
    }
}