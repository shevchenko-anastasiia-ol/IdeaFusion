namespace ContentBLL.Services.Interfaces;

public interface IPostViewService
{
    /// <summary>Records a view of a post by the caller. The viewer is read from the token.</summary>
    Task RecordAsync(int postId, CancellationToken ct = default);

    Task<int> CountByPostAsync(int postId, CancellationToken ct = default);
}
