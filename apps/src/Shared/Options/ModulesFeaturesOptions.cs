using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Options
{
    public sealed class ModulesFeaturesOptions
    {
        public bool Agents { get; set; } = false;

        public bool Workflows { get; set; } = true;

        public bool Webhooks { get; set; } = false;

        public bool Dashboard { get; set; } = true;
    }
}
