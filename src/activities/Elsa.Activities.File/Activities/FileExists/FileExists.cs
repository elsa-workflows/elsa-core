using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Elsa.Activities.File
{
    public class FileExists : Activity
    {
        [ActivityProperty(Hint = "Path of the file to delete.")]
        public string Path { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            var exists = System.IO.File.Exists(Path);
            return Done(exists);
        }
    }
}
