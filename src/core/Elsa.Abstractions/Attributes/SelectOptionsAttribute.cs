using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Elsa.Attributes
{
    public class SelectOptionsAttribute : Attribute
    {
        public SelectOptionsAttribute()
        {
        }

        public SelectOptionsAttribute(params string[] items) => 
            Items = items.Select(x => new SelectOption(x)).ToArray();

        public SelectOptionsAttribute(string optionsProviderMethod, Type providerType)
        {
            OptionsProviderMethod = optionsProviderMethod;
            ProviderType = providerType;
        }

        public SelectOption[] Items { get; } = new SelectOption[0];
        public string? OptionsProviderMethod { get; }
        public Type? ProviderType { get; }

        public IEnumerable<SelectOption> GetOptions() => Items ?? GetItemsFromProvider();

        private IEnumerable<SelectOption> GetItemsFromProvider()
        {
            if (ProviderType == null || OptionsProviderMethod == null)
                return Enumerable.Empty<SelectOption>();

            var method = ProviderType.GetMethod(OptionsProviderMethod, BindingFlags.Public | BindingFlags.Static);

            if (method == null)
                throw new InvalidOperationException($"No static method called {OptionsProviderMethod} exists on type ${ProviderType}");

            return (IEnumerable<SelectOption>)method.Invoke(null, null);
        }
    }

    public class SelectOption
    {
        public SelectOption(string label, string? value = default)
        {
            Label = label;
            Value = value;
        }

        public string? Value { get; set; }
        public string Label { get; set; }
    }
}