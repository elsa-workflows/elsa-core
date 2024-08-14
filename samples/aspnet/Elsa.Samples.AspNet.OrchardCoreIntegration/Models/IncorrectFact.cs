namespace Elsa.Samples.AspNet.OrchardCoreIntegration;

public class IncorrectFact
{
    public string IncorrectStatement { get; set; }
    public string CorrectStatement { get; set; }
    public string Explanation { get; set; }
    public ICollection<string> References { get; set; }
}