using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Elsa.Attributes
{
    public class SelectOptionsAttribute : ActivityPropertyOptionsAttribute
    {
        public SelectOptionsAttribute()
        {
        }

        public SelectOptionsAttribute(params string[] items)
        {
            Items = items.Cast<object>().ToArray();
        }

        public SelectOptionsAttribute(string optionsProviderMethod, Type providerType)
        {
            OptionsProviderMethod = optionsProviderMethod;
            ProviderType = providerType;
        }

        public object[] Items { get; }
        public string OptionsProviderMethod { get; }
        public Type ProviderType { get; }

        public override object GetOptions()
        {
            if (Items != null)
                return new
                {
                    Items
                };

            
            var selectItems = GetItemsFromProvider();
            return new
            {
                Items = selectItems.Select(ToObject).ToArray()
            };
        }

        private IEnumerable<object> GetItemsFromProvider()
        {
            var method = ProviderType.GetMethod(OptionsProviderMethod, BindingFlags.Public | BindingFlags.Static);

            return (IEnumerable<object>) method.Invoke(null, null);
        }

        private object ToObject(object item)
        {
            if ((item is SelectItem selectItem) && selectItem.Options != null && selectItem.Label == null)
                return selectItem.Options;

            return item;
        }
    }

    public class SelectItem
    {
        public string Label { get; set; }
        public object[] Options { get; set; }
    }
    
    public class SelectOption<T>
    {
        public SelectOption()
        {
        }

        public SelectOption(string label, T value)
        {
            Label = label;
            Value = value;
        }
        
        public string Label { get; set; }
        public T Value { get; set; }
    }
    
    public class SelectOption : SelectOption<string>
    {
        public SelectOption()
        {
        }

        public SelectOption(string label, string value) : base(label, value)
        {
        }
    }
}