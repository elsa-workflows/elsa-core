namespace Elsa.Bpmn.Interchange.IntegrationTests.Support;

/// <summary>
/// <see cref="BpmnAssetReader.Read"/> is shared by every test that needs a BPMN fixture, so its guard against a
/// rooted or nested file name — which would otherwise resolve outside the <c>Assets</c> directory — is covered once,
/// here, rather than per call site.
/// </summary>
public class BpmnAssetReaderTests
{
    [Test]
    [DisplayName("Reading a plain fixture name returns its contents")]
    public async Task Read_OfAPlainFileName_ReturnsContents()
    {
        var content = BpmnAssetReader.Read("camunda-order-process.bpmn");

        await Assert.That(content).Contains("order-process", StringComparison.CurrentCulture);
    }

    [Test]
    [DisplayName("Reading a rooted or nested file name is refused rather than resolved outside Assets")]
    [Arguments("/etc/passwd")]
    [Arguments("../secrets.txt")]
    [Arguments("nested/file.bpmn")]
    public void Read_OfARootedOrNestedFileName_Throws(string fileName)
    {
        Assert.ThrowsExactly<ArgumentException>(() => BpmnAssetReader.Read(fileName));
    }
}
