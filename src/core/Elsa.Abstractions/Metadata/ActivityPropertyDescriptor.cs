using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Metadata
{
    public class ActivityPropertyDescriptor
    {
        public ActivityPropertyDescriptor()
        {
        }

        public ActivityPropertyDescriptor(
            string name,
            Type type,
            string uiHint,
            string label,
            string? hint = default,
            object? options = default,
            string? category = default,
            object? defaultValue = default,
            string? defaultSyntax = "Literal",
            IEnumerable<string>? supportedSyntaxes = default)
        {
            Name = name;
            Type = type;
            UIHint = uiHint;
            Label = label;
            Hint = hint;
            Options = options;
            Category = category;
            DefaultValue = defaultValue;
            DefaultSyntax = defaultSyntax;
            SupportedSyntaxes = supportedSyntaxes?.ToList() ?? new List<string>();
        }

        public string Name { get; set; } = default!;
        public Type Type { get; set; } = default!;
        public string UIHint { get; set; } = default!;
        public string Label { get; set; } = default!;
        public string? Hint { get; set; }
        public object? Options { get; set; }
        public string? Category { get; set; }
        public object? DefaultValue { get; set; }
        public string? DefaultSyntax { get; set; }
        public IList<string> SupportedSyntaxes { get; set; } = new List<string>();
    }
}