namespace Collaboration.Application.Interfaces.Services;

public interface IUserSnapshotService
{
    Task<(string Username, string? AvatarUrl)> GetUserSnapshotDataAsync(
        string userId, CancellationToken cancellationToken = default);
}