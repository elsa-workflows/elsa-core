using System.Collections.Generic;
using Elsa.Attributes;
using Elsa.Expressions;
using Elsa.Services;

namespace Elsa.Samples.Server.Host;

public class SampleActivity : Activity
{
    [ActivityInput(SupportedSyntaxes = new[]{SyntaxNames.JavaScript, SyntaxNames.Liquid})] public ICollection<string> MyList { get; set; }
}