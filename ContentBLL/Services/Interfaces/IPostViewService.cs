namespace ContentBLL.Services.Interfaces;

public interface IPostViewService
{
    Task RecordAsync(int postId, int? userId, CancellationToken ct = default);
    Task<int> CountByPostAsync(int postId, CancellationToken ct = default);
}