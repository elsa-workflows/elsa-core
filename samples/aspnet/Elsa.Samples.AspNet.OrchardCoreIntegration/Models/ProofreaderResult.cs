namespace Elsa.Samples.AspNet.OrchardCoreIntegration;

public class ProofreaderResult
{
    public string Corrected { get; set; }
    public ICollection<ProofreadError> Errors { get; set; }
}