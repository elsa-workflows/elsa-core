namespace Elsa.Bpmn.Interchange.IntegrationTests.Support;

/// <summary>
/// <see cref="BpmnAssetReader.Read"/> is shared by every test that needs a BPMN fixture, so its guard against a
/// rooted or nested file name — which would otherwise resolve outside the <c>Assets</c> directory — is covered once,
/// here, rather than per call site.
/// </summary>
public class BpmnAssetReaderTests
{
    [Fact(DisplayName = "Reading a plain fixture name returns its contents")]
    public void Read_OfAPlainFileName_ReturnsContents()
    {
        var content = BpmnAssetReader.Read("camunda-order-process.bpmn");

        Assert.Contains("order-process", content);
    }

    [Theory(DisplayName = "Reading a rooted or nested file name is refused rather than resolved outside Assets")]
    [InlineData("/etc/passwd")]
    [InlineData("../secrets.txt")]
    [InlineData("nested/file.bpmn")]
    public void Read_OfARootedOrNestedFileName_Throws(string fileName)
    {
        Assert.Throws<ArgumentException>(() => BpmnAssetReader.Read(fileName));
    }
}
