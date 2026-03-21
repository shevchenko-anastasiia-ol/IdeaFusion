using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;

namespace ContentBLL.Services;

public class PostViewService : IPostViewService
{
    private readonly IUnitOfWork _uow;
 
    public PostViewService(IUnitOfWork uow)
    {
        _uow = uow;
    }
 
    public async Task RecordAsync(int postId, int? userId, CancellationToken ct = default)
    {
        // не кидаємо виключення якщо пост не знайдено — перегляд "тихий"
        var post = await _uow.PostRepository.GetByIdAsync(postId, ct);
        if (post is null) return;
 
        var view = new PostView
        {
            PostId   = postId,
            UserId   = userId,
            ViewedAt = DateTime.UtcNow
        };
 
        await _uow.PostViewRepository.AddAsync(view, ct);
    }
 
    public async Task<int> CountByPostAsync(int postId, CancellationToken ct = default)
    {
        return await _uow.PostViewRepository.CountByPostAsync(postId, ct);
    }
}