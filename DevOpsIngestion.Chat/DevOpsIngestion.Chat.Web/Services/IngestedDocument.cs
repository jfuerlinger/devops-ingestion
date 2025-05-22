using Microsoft.Extensions.VectorData;

namespace DevOpsIngestion.Chat.Web.Services;

public class IngestedDocument
{
    private const int _vectorDimensions = 2;
    private const string _vectorDistanceFunction = DistanceFunction.CosineSimilarity;

    [VectorStoreKey]
    public required Guid Key { get; set; }

    [VectorStoreData(IsIndexed = true)]
    public required string SourceId { get; set; }

    [VectorStoreData]
    public required string DocumentId { get; set; }

    [VectorStoreData]
    public required string DocumentVersion { get; set; }

    // The vector is not used but required for some vector databases
    [VectorStoreVector(_vectorDimensions, DistanceFunction = _vectorDistanceFunction)]
    public ReadOnlyMemory<float> Vector { get; set; } = new ReadOnlyMemory<float>([0, 0]);
}
