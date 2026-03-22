namespace Collaboration.Domain.Exceptions;

public class ConcurrencyException : Exception
{
    public string EntityId { get; }
    public long ExpectedVersion { get; }
    public long ActualVersion { get; }

    public ConcurrencyException(
        string entityId,
        long expectedVersion,
        long actualVersion)
        : base($"Concurrency conflict for entity {entityId}. Expected version: {expectedVersion}, Actual version: {actualVersion}")
    {
        EntityId = entityId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}