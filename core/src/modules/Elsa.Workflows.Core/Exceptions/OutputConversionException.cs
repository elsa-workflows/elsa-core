namespace Elsa.Workflows.Exceptions;

/// <summary>
/// Represents a privacy-safe failure while converting an activity output's bound value.
/// </summary>
public class OutputConversionException : Exception, ISafeExceptionMetadataProvider
{
    /// <summary>
    /// Initializes a new output conversion exception.
    /// </summary>
    public OutputConversionException(
        string converterId,
        OutputConversionFailureStage stage,
        string activityId,
        string activityType,
        string outputName,
        string? destinationId,
        Type sourceType,
        Type? destinationType,
        Exception? innerException = null)
        : base(CreateMessage(converterId, stage, activityId, outputName), innerException)
    {
        ConverterId = converterId;
        Stage = stage;
        ActivityId = activityId;
        ActivityType = activityType;
        OutputName = outputName;
        DestinationId = destinationId;
        SourceTypeName = sourceType.FullName ?? sourceType.Name;
        DestinationTypeName = destinationType?.FullName;
    }

    /// <summary>The configured converter ID.</summary>
    public string ConverterId { get; }

    /// <summary>The stage at which conversion failed.</summary>
    public OutputConversionFailureStage Stage { get; }

    /// <summary>The definition ID of the activity producing the output.</summary>
    public string ActivityId { get; }

    /// <summary>The activity type name.</summary>
    public string ActivityType { get; }

    /// <summary>The declared output name.</summary>
    public string OutputName { get; }

    /// <summary>The optional destination ID.</summary>
    public string? DestinationId { get; }

    /// <summary>The declared source type name.</summary>
    public string SourceTypeName { get; }

    /// <summary>The optional declared destination type name.</summary>
    public string? DestinationTypeName { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> GetSafeMetadata()
    {
        var metadata = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [nameof(ConverterId)] = ConverterId,
            [nameof(Stage)] = Stage.ToString(),
            [nameof(ActivityId)] = ActivityId,
            [nameof(ActivityType)] = ActivityType,
            [nameof(OutputName)] = OutputName,
            [nameof(SourceTypeName)] = SourceTypeName
        };

        if (DestinationId != null)
            metadata[nameof(DestinationId)] = DestinationId;

        if (DestinationTypeName != null)
            metadata[nameof(DestinationTypeName)] = DestinationTypeName;

        return metadata;
    }

    private static string CreateMessage(string converterId, OutputConversionFailureStage stage, string activityId, string outputName) =>
        $"Output converter '{converterId}' failed during {stage} for output '{outputName}' of activity '{activityId}'.";
}
