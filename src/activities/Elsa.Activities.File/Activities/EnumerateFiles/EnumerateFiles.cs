using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Enumerates files in a given folder",
        Outcomes = new[] { OutcomeNames.Done })]
    public class EnumerateFiles : Activity
    {
        [Required]
        [ActivityInput(Hint = "Path of the folder to enumerate files.")]
        public string Path { get; set; } = default!;

        [ActivityInput(Hint = "Pattern for files to return.")]
        public string? Pattern { get; set; }

        [ActivityInput(Hint = "Ignore inaccessible files.", Label = "Ignore Inaccessible", DefaultValue = true)]
        public bool IgnoreInaccessible { get; set; }

        [ActivityInput(Hint = "Set case sensitivity.", Label = "Match Casing", DefaultValue = MatchCasing.CaseInsensitive)]
        public MatchCasing MatchCasing { get; set; }

        [ActivityInput(Hint = "Return files from sub directories", Label = "Sub Directories", DefaultValue = false)]
        public bool SubDirectories { get; set; }

        [ActivityOutput(Hint = "List of files.")]
        public IEnumerable<string>? Files { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(Pattern))
            {
                Files = Directory.EnumerateFiles(Path).ToList();
                return Done(Files);
            }

            var options = new EnumerationOptions()
            {
                IgnoreInaccessible = IgnoreInaccessible,
                MatchCasing = MatchCasing,
                RecurseSubdirectories = SubDirectories
            };
            
            Files = Directory.EnumerateFiles(Path, Pattern, options).ToList();
            return Done(Files);
        }
    }
}
