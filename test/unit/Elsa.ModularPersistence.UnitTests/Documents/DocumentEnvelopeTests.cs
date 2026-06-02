using Elsa.ModularPersistence.Documents;

namespace Elsa.ModularPersistence.UnitTests.Documents;

public class DocumentEnvelopeTests
{
    private readonly DateTimeOffset _createdAt = new(2026, 06, 01, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CanCreateDocumentEnvelope()
    {
        var document = CreateDocument();

        Assert.Equal("secret-1", document.Id);
        Assert.Equal("Secret", document.Type);
        Assert.Equal("tenant-a", document.TenantId);
        Assert.Equal(3, document.Version);
        Assert.Equal(_createdAt, document.CreatedAt);
        Assert.Equal(_createdAt.AddMinutes(1), document.UpdatedAt);
        Assert.Equal("""{"value":"redacted"}""", document.Data);
        Assert.Equal("classification", Assert.Single(document.Metadata).Key);
        Assert.Equal(document.Key, new DocumentKey("secret-1", "Secret", "tenant-a"));
    }

    [Fact]
    public void TrimsIdentityFieldsAndNormalizesEmptyTenant()
    {
        var document = new DocumentEnvelope(
            " secret-1 ",
            " Secret ",
            " ",
            0,
            _createdAt,
            _createdAt,
            "{}");

        Assert.Equal("secret-1", document.Id);
        Assert.Equal("Secret", document.Type);
        Assert.Null(document.TenantId);
    }

    [Fact]
    public void CopiesMetadata()
    {
        var metadata = new Dictionary<string, string>
        {
            ["source"] = "test"
        };

        var document = CreateDocument(metadata: metadata);
        metadata["source"] = "changed";

        Assert.Equal("test", document.Metadata["source"]);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RejectsEmptyId(string id)
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateDocument(id: id));

        Assert.Equal("id", exception.ParamName);
    }

    [Fact]
    public void RejectsNegativeVersion()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => CreateDocument(version: -1));

        Assert.Equal("version", exception.ParamName);
    }

    [Fact]
    public void RejectsUpdatedTimestampBeforeCreatedTimestamp()
    {
        var exception = Assert.Throws<ArgumentException>(() => new DocumentEnvelope(
            "secret-1",
            "Secret",
            null,
            0,
            _createdAt,
            _createdAt.AddTicks(-1),
            "{}"));

        Assert.Equal("updatedAt", exception.ParamName);
    }

    [Fact]
    public void RejectsEmptyData()
    {
        var exception = Assert.Throws<ArgumentException>(() => CreateDocument(data: " "));

        Assert.Equal("data", exception.ParamName);
    }

    [Fact]
    public void DocumentKeyNormalizesTenant()
    {
        var key = new DocumentKey(" id ", " Secret ", " tenant-a ");

        Assert.Equal("id", key.Id);
        Assert.Equal("Secret", key.Type);
        Assert.Equal("tenant-a", key.TenantId);
    }

    [Fact]
    public void ExpectedExactVersionRejectsNegativeVersion()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => ExpectedDocumentVersion.Exact(-1));

        Assert.Equal("version", exception.ParamName);
    }

    [Fact]
    public void ExpectedVersionDefaultsToAny()
    {
        var expectedVersion = default(ExpectedDocumentVersion);

        Assert.Equal(ExpectedDocumentVersionKind.Any, expectedVersion.Kind);
        Assert.Null(expectedVersion.Version);
    }

    private DocumentEnvelope CreateDocument(
        string id = "secret-1",
        string type = "Secret",
        string? tenantId = "tenant-a",
        long version = 3,
        string data = """{"value":"redacted"}""",
        IReadOnlyDictionary<string, string>? metadata = null) =>
        new(
            id,
            type,
            tenantId,
            version,
            _createdAt,
            _createdAt.AddMinutes(1),
            data,
            metadata ?? new Dictionary<string, string>
            {
                ["classification"] = "secret"
            });
}
