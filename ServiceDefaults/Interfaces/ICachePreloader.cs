namespace ServiceDefaults.Interfaces;

public interface ICachePreloader
{
    Task PreloadAsync(CancellationToken cancellationToken);
}