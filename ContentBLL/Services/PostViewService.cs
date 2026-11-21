using ContentBLL.Services.Interfaces;
using ContentDAL.UOW;
using ContentDomain.Entity;

namespace ContentBLL.Services;

public class PostViewService : IPostViewService
{
    private readonly IUnitOfWork _uow;
    private readonly IPostAuthorResolver _authorResolver;

    public PostViewService(IUnitOfWork uow, IPostAuthorResolver authorResolver)
    {
        _uow = uow;
        _authorResolver = authorResolver;
    }

    public async Task RecordAsync(int postId, CancellationToken ct = default)
    {
        // не кидаємо виключення якщо пост не знайдено — перегляд "тихий"
        var post = await _uow.PostRepository.GetByIdAsync(postId, ct);
        if (post is null) return;

        // The viewer is whoever holds the token. A caller who has never written has
        // no content-side user id yet, and the view is recorded anonymous.
        var caller = await _authorResolver.FindAsync(ct);

        var view = new PostView
        {
            PostId   = postId,
            UserId   = caller?.UserId,
            ViewedAt = DateTime.UtcNow
        };

        await _uow.PostViewRepository.AddAsync(view, ct);
    }

    public async Task<int> CountByPostAsync(int postId, CancellationToken ct = default)
    {
        return await _uow.PostViewRepository.CountByPostAsync(postId, ct);
    }
}
