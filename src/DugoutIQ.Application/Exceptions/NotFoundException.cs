namespace DugoutIQ.Application.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string resourceType, string resourceId)
        : base($"{resourceType} {resourceId} could not be found.")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
    }

    public string ResourceType { get; }

    public string ResourceId { get; }
}
