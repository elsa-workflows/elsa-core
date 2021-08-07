using Elsa.ActivityResults;
using Elsa.Attributes;
using Elsa.Services;
using Elsa.Services.Models;
using System.Collections.Generic;
using System.IO;

// ReSharper disable once CheckNamespace
namespace Elsa.Activities.File
{
    [Action(Category = "File",
        Description = "Enumerates files in a given folder",
        Outcomes = new[] { OutcomeNames.Done })]
    public class EnumerateFiles : Activity
    {
        [ActivityInput(Hint = "Path of the folder to enumerate files.")]
        public string Path { get; set; } = default!;

        [ActivityInput(Hint = "Pattern for files to return.")]
        public string? Pattern { get; set; }

        [ActivityInput(Hint = "Ignore inaccessible files.", Label = "Ignore Inaccessible")]
        public bool IgnoreInaccessible { get; set; } = true;

        [ActivityInput(Hint = "Set case sensitivity.", Label = "Match Casing")]
        public MatchCasing MatchCasing { get; set; } = MatchCasing.CaseInsensitive;

        [ActivityInput(Hint = "Return files from sub directories", Label = "Sub Directories")]
        public bool SubDirectories { get; set; } = false;

        [ActivityOutput(Hint = "List of files.")]
        public IEnumerable<string>? Files { get; set; }

        protected override IActivityExecutionResult OnExecute(ActivityExecutionContext context)
        {
            if (string.IsNullOrWhiteSpace(Pattern))
                return Done(Directory.EnumerateFiles(Path!));

            var options = new EnumerationOptions()
            {
                IgnoreInaccessible = IgnoreInaccessible,
                MatchCasing = MatchCasing,
                RecurseSubdirectories = SubDirectories
            };
            
            Files = Directory.EnumerateFiles(Path, Pattern, options);
            return Done();
        }
    }
}
