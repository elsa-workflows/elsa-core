using Elsa.Workflows;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Attributes.Conditional;
using Elsa.Workflows.Models;
using Elsa.Workflows.UIHints;

namespace Elsa.Server.Web;

public class CondActivity : Activity
{
    [StateDropdownInput([
        ConditionalInputOptions.WithDescription, "getTicket", "Get ticket",
        ConditionalInputOptions.WithDescription, "addArticle", "Add artice"])]
    public Input<string> ApiName{ get; set; } = default;


    [ConditionalInput(["addArticle"], Description = "Article double", DefaultValue = 0.0, UIHint = InputUIHints.SingleLine)]
    public Input<double> ActicleText {get; set;} = default;

    [ConditionalInput(["getTicket"], Description = "Article int", DefaultValue = 0, UIHint = InputUIHints.SingleLine)]
    public Input<int> ActicleInt {get; set;} = default;
}