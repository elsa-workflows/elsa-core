using System;
using System.Collections.Generic;
using Microsoft.Extensions.Localization;

namespace Flowsharp.Models
{
    public class ActivityDescriptor
    {
        public string Name { get; set; }
        public LocalizedString DisplayText { get; set; }
        public LocalizedString Description { get; set; }
        public Func<IEnumerable<LocalizedString>> EndpointsDelegate { get; set; }
    }
}