using System;
using System.Collections.Generic;
using System.Linq;

namespace Elsa.Metadata
{
    [Obsolete("Use ActivityInputDescriptor instead.")]
    public class ActivityPropertyDescriptor : ActivityInputDescriptor
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
            IEnumerable<string>? supportedSyntaxes = default,
            bool isReadOnly = false,
            bool isBrowsable = true)
            : base(
                name,
                type,
                uiHint,
                label,
                hint,
                options,
                category,
                0,
                defaultValue,
                defaultSyntax,
                supportedSyntaxes,
                isReadOnly,
                isBrowsable
            )
        {
        }
    }

    public class ActivityInputDescriptor
    {
        public ActivityInputDescriptor()
        {
        }

        public ActivityInputDescriptor(
            string name,
            Type type,
            string uiHint,
            string label,
            string? hint = default,
            object? options = default,
            string? category = default,
            float order = 0,
            object? defaultValue = default,
            string? defaultSyntax = "Literal",
            IEnumerable<string>? supportedSyntaxes = default,
            bool isReadOnly = false,
            bool isBrowsable = true,
            bool isDesignerCritical = false,
            string? defaultWorkflowStorageProvider = default,
            bool disableWorkflowProviderSelection = false)
        {
            Name = name;
            Type = type;
            UIHint = uiHint;
            Label = label;
            Hint = hint;
            Options = options;
            Category = category;
            Order = order;
            DefaultValue = defaultValue;
            DefaultSyntax = defaultSyntax;
            SupportedSyntaxes = supportedSyntaxes?.ToList() ?? new List<string>();
            IsReadOnly = isReadOnly;
            IsBrowsable = isBrowsable;
            IsDesignerCritical = isDesignerCritical;
            DefaultWorkflowStorageProvider = defaultWorkflowStorageProvider;
            DisableWorkflowProviderSelection = disableWorkflowProviderSelection;
        }

        public string Name { get; set; } = default!;
        public Type Type { get; set; } = default!;
        public string UIHint { get; set; } = default!;
        public string Label { get; set; } = default!;
        public string? Hint { get; set; }
        public object? Options { get; set; }
        public string? Category { get; set; }
        public float Order { get; set; }
        public object? DefaultValue { get; set; }
        public string? DefaultSyntax { get; set; }
        public IList<string> SupportedSyntaxes { get; set; } = new List<string>();
        public bool? IsReadOnly { get; set; }
        public bool? IsBrowsable { get; set; }
        public bool IsDesignerCritical { get; set; }
        public string? DefaultWorkflowStorageProvider { get; set; }
        public bool DisableWorkflowProviderSelection { get; set; }
    }
}