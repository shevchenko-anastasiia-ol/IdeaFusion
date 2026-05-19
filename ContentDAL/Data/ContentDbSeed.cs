using Bogus;
using ContentDomain.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ContentDAL.Data;

public class ContentDbSeed
{
    private readonly ContentDbContext _context;
    private readonly ILogger<ContentDbSeed> _logger;
 
    private const int POSTS_COUNT = 40;
    private const int TAGS_COUNT = 20;
    private const int TAGS_PER_POST = 3;
    private const int COMMENTS_PER_POST = 5;
    private const int REPLIES_PER_COMMENT = 2;
    private const int LIKES_PER_POST = 10;
    private const int VIEWS_PER_POST = 20;
    private const int SAVED_PER_POST = 5;
 
    private const int AUTHORS_COUNT = 30;
 
    // імітуємо існуючих юзерів та колаборації з інших сервісів
    private static readonly int[] FakeUserIds = Enumerable.Range(1, AUTHORS_COUNT).ToArray();
    private static readonly int[] FakeCollaborationIds = Enumerable.Range(1, 10).ToArray();
 
    public ContentDbSeed(ContentDbContext context, ILogger<ContentDbSeed> logger)
    {
        _context = context;
        _logger = logger;
    }
 
    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("=== Starting ContentService database seeding ===");

            await RemoveSeedPostsAsync();

            var hasTags = await _context.Tags.AnyAsync();

            if (!hasTags)
                await SeedTagsAsync();
            else
                _logger.LogInformation("Tags already seeded. Skipping...");

            _logger.LogInformation("=== Seeding completed ===");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! CRITICAL ERROR during ContentService seeding !!!");
            throw;
        }
    }

    private async Task RemoveSeedPostsAsync()
    {
        var seededPosts = await _context.Posts
            .Where(p => p.CreatedBy == "seed")
            .ToListAsync();

        if (!seededPosts.Any())
        {
            _logger.LogInformation("No seeded posts found. Skipping cleanup.");
            return;
        }

        var postIds = seededPosts.Select(p => p.PostId).ToList();

        // Delete related data in dependency order
        await _context.SavedPosts.Where(x => postIds.Contains(x.PostId)).ExecuteDeleteAsync();
        await _context.PostViews.Where(x => postIds.Contains(x.PostId)).ExecuteDeleteAsync();
        await _context.Likes.Where(x => postIds.Contains(x.PostId)).ExecuteDeleteAsync();
        await _context.PostMedia.Where(x => postIds.Contains(x.PostId)).ExecuteDeleteAsync();

        // Delete replies first, then top-level comments
        var commentIds = await _context.Comments
            .Where(c => postIds.Contains(c.PostId))
            .Select(c => c.CommentId)
            .ToListAsync();
        await _context.Comments.Where(c => c.ParentCommentId != null && commentIds.Contains(c.CommentId)).ExecuteDeleteAsync();
        await _context.Comments.Where(c => postIds.Contains(c.PostId)).ExecuteDeleteAsync();

        await _context.PostTags.Where(pt => postIds.Contains(pt.PostId)).ExecuteDeleteAsync();

        _context.Posts.RemoveRange(seededPosts);
        await _context.SaveChangesAsync();

        // Remove orphaned fake authors and collaboration snapshots
        await _context.PostAuthors
            .Where(a => !_context.Posts.Any(p => p.PostAuthorId == a.PostAuthorId))
            .ExecuteDeleteAsync();
        await _context.CollaborationSnapshots
            .Where(c => !_context.Posts.Any(p => p.CollaborationSnapshotId == c.CollaborationSnapshotId))
            .ExecuteDeleteAsync();

        _logger.LogInformation("Removed {Count} seeded posts and their related data.", seededPosts.Count);
    }
 
    private async Task SeedPostAuthorsAsync()
    {
        try
        {
            _logger.LogInformation(">>> Seeding post authors...");
 
            var faker = new Faker();
            var authors = FakeUserIds.Select(userId => new PostAuthor
            {
                UserId    = userId,
                UserName  = faker.Internet.UserName(),
                AvatarUrl = faker.Random.Bool(0.7f) ? faker.Internet.Avatar() : null,
                SyncedAt  = faker.Date.Recent(7).ToUniversalTime()
            }).ToList();
 
            await _context.PostAuthors.AddRangeAsync(authors);
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("<<< Seeded {Count} post authors (SaveChanges: {Saved})", authors.Count, saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! ERROR seeding post authors !!!");
            throw;
        }
    }
 
    private async Task SeedCollaborationSnapshotsAsync()
    {
        try
        {
            _logger.LogInformation(">>> Seeding collaboration snapshots...");
 
            var faker = new Faker();
            var snapshots = FakeCollaborationIds.Select(collabId => new CollaborationSnapshot
            {
                CollaborationId = collabId,
                Name            = faker.Company.CompanyName(),
                AvatarUrl       = faker.Random.Bool(0.6f) ? faker.Internet.Avatar() : null,
                SyncedAt        = faker.Date.Recent(7).ToUniversalTime()
            }).ToList();
 
            await _context.CollaborationSnapshots.AddRangeAsync(snapshots);
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("<<< Seeded {Count} collaboration snapshots (SaveChanges: {Saved})", snapshots.Count, saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! ERROR seeding collaboration snapshots !!!");
            throw;
        }
    }
 
    private async Task SeedTagsAsync()
    {
        try
        {
            _logger.LogInformation(">>> Seeding tags...");
 
            var tagNames = new[]
            {
                "music", "art", "photography", "design", "illustration",
                "writing", "poetry", "film", "animation", "gamedev",
                "3d", "ui-ux", "branding", "fashion", "architecture",
                "street-art", "digital", "traditional", "collab", "wip"
            };
 
            var tags = tagNames.Take(TAGS_COUNT).Select(name => new Tag { Name = name }).ToList();
 
            await _context.Tags.AddRangeAsync(tags);
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("<<< Seeded {Count} tags (SaveChanges: {Saved})", tags.Count, saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! ERROR seeding tags !!!");
            throw;
        }
    }
 
    private async Task SeedPostsAsync()
    {
        try
        {
            _logger.LogInformation(">>> Seeding posts...");
 
            var tagIds = await _context.Tags.Select(t => t.TagId).ToListAsync();
            var authorIds = await _context.PostAuthors.Select(a => a.PostAuthorId).ToListAsync();
            var collabSnapshotIds = await _context.CollaborationSnapshots.Select(c => c.CollaborationSnapshotId).ToListAsync();
 
            if (!tagIds.Any())
            {
                _logger.LogWarning("No tags found. Cannot assign tags to posts.");
                return;
            }
 
            if (!authorIds.Any())
            {
                _logger.LogWarning("No authors found. Cannot seed posts.");
                return;
            }
 
            if (!collabSnapshotIds.Any())
            {
                _logger.LogWarning("No collaboration snapshots found. Cannot seed posts.");
                return;
            }
 
            var faker = new Faker();
            var posts = new List<Post>();
 
            for (int i = 0; i < POSTS_COUNT; i++)
            {
                var isPersonal = i % 2 == 0;
 
                var post = new Post
                {
                    PostAuthorId            = isPersonal ? faker.PickRandom(authorIds) : null,
                    CollaborationSnapshotId = isPersonal ? null : faker.PickRandom(collabSnapshotIds),
                    Title            = faker.Lorem.Sentence(wordCount: faker.Random.Int(3, 8)).TrimEnd('.'),
                    Description      = faker.Lorem.Paragraph(),
                    ExternalLink     = faker.Random.Bool(0.3f) ? faker.Internet.Url() : null,
                    Status           = faker.Random.WeightedRandom(
                                           new[] { PostStatus.Published, PostStatus.Archived, PostStatus.Draft },
                                           new[] { 0.75f, 0.15f, 0.10f }),
                    CreatedAt        = faker.Date.Past(1).ToUniversalTime(),
                    CreatedBy        = "seed",
                    UpdatedAt        = faker.Random.Bool(0.25f) ? faker.Date.Recent(60).ToUniversalTime() : null,
                    UpdatedBy        = faker.Random.Bool(0.25f) ? "seed" : null,
                    IsDeleted        = false
                };
 
                // призначаємо від 1 до TAGS_PER_POST тегів
                var assignedTagIds = faker.PickRandom(tagIds, faker.Random.Int(1, TAGS_PER_POST)).Distinct();
                foreach (var tagId in assignedTagIds)
                    post.PostTags.Add(new PostTag { TagId = tagId });
 
                posts.Add(post);
            }
 
            await _context.Posts.AddRangeAsync(posts);
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("<<< Seeded {Count} posts (SaveChanges: {Saved})", posts.Count, saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! ERROR seeding posts !!!");
            throw;
        }
    }
 
    private async Task SeedCommentsAsync()
    {
        try
        {
            _logger.LogInformation(">>> Seeding comments...");
 
            var postIds = await _context.Posts
                .Where(p => p.Status == PostStatus.Published && !p.IsDeleted)
                .Select(p => p.PostId)
                .ToListAsync();
 
            var authorIds = await _context.PostAuthors.Select(a => a.PostAuthorId).ToListAsync();
 
            if (!postIds.Any())
            {
                _logger.LogWarning("No published posts found. Skipping comments.");
                return;
            }
 
            if (!authorIds.Any())
            {
                _logger.LogWarning("No authors found. Skipping comments.");
                return;
            }
 
            var faker = new Faker();
            var comments = new List<Comment>();
 
            foreach (var postId in postIds)
            {
                var topLevel = new List<Comment>();
 
                // top-level коментарі
                for (int i = 0; i < COMMENTS_PER_POST; i++)
                {
                    var comment = new Comment
                    {
                        PostId          = postId,
                        PostAuthorId    = faker.PickRandom(authorIds),
                        ParentCommentId = null,
                        Body            = faker.Lorem.Sentences(faker.Random.Int(1, 3)),
                        CreatedAt       = faker.Date.Past(1).ToUniversalTime(),
                        CreatedBy       = "seed",
                        UpdatedAt       = faker.Random.Bool(0.15f) ? faker.Date.Recent(30).ToUniversalTime() : null,
                        UpdatedBy       = faker.Random.Bool(0.15f) ? "seed" : null,
                        IsDeleted       = false
                    };
 
                    topLevel.Add(comment);
                    comments.Add(comment);
                }
 
                // зберігаємо top-level, щоб отримати їх Id для відповідей
                await _context.Comments.AddRangeAsync(topLevel);
                await _context.SaveChangesAsync();
 
                // replies
                var replies = new List<Comment>();
                foreach (var parent in topLevel)
                {
                    var replyCount = faker.Random.Int(0, REPLIES_PER_COMMENT);
                    for (int r = 0; r < replyCount; r++)
                    {
                        replies.Add(new Comment
                        {
                            PostId          = postId,
                            PostAuthorId    = faker.PickRandom(authorIds),
                            ParentCommentId = parent.CommentId,
                            Body            = faker.Lorem.Sentence(),
                            CreatedAt       = parent.CreatedAt.ToUniversalTime().AddMinutes(faker.Random.Int(1, 1440)),
                            CreatedBy       = "seed",
                            UpdatedAt       = null,
                            UpdatedBy       = null,
                            IsDeleted       = false
                        });
                    }
                }
 
                if (replies.Any())
                {
                    await _context.Comments.AddRangeAsync(replies);
                    await _context.SaveChangesAsync();
                    comments.AddRange(replies);
                }
            }
 
            _logger.LogInformation("<<< Seeded {Count} comments (top-level + replies)", comments.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! ERROR seeding comments !!!");
            throw;
        }
    }
 
    private async Task SeedLikesAsync()
    {
        try
        {
            _logger.LogInformation(">>> Seeding likes...");
 
            var postIds = await _context.Posts
                .Where(p => p.Status == PostStatus.Published && !p.IsDeleted)
                .Select(p => p.PostId)
                .ToListAsync();
 
            if (!postIds.Any())
            {
                _logger.LogWarning("No published posts found. Skipping likes.");
                return;
            }
 
            var faker = new Faker();
            var likes = new List<Like>();
 
            foreach (var postId in postIds)
            {
                // унікальна комбінація PostId + UserId
                var usersWhoLiked = FakeUserIds
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(faker.Random.Int(1, LIKES_PER_POST))
                    .ToList();
 
                foreach (var userId in usersWhoLiked)
                {
                    likes.Add(new Like
                    {
                        PostId    = postId,
                        UserId    = userId,
                        CreatedAt = faker.Date.Past(1).ToUniversalTime()
                    });
                }
            }
 
            await _context.Likes.AddRangeAsync(likes);
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("<<< Seeded {Count} likes (SaveChanges: {Saved})", likes.Count, saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! ERROR seeding likes !!!");
            throw;
        }
    }
 
    private async Task SeedPostViewsAsync()
    {
        try
        {
            _logger.LogInformation(">>> Seeding post views...");
 
            var postIds = await _context.Posts
                .Where(p => !p.IsDeleted)
                .Select(p => p.PostId)
                .ToListAsync();
 
            if (!postIds.Any())
            {
                _logger.LogWarning("No posts found. Skipping views.");
                return;
            }
 
            var faker = new Faker();
            var views = new List<PostView>();
 
            foreach (var postId in postIds)
            {
                var viewCount = faker.Random.Int(1, VIEWS_PER_POST);
                for (int i = 0; i < viewCount; i++)
                {
                    views.Add(new PostView
                    {
                        PostId   = postId,
                        // 30% переглядів — анонімні
                        UserId   = faker.Random.Bool(0.7f) ? faker.PickRandom(FakeUserIds) : null,
                        ViewedAt = faker.Date.Past(1).ToUniversalTime()
                    });
                }
            }
 
            await _context.PostViews.AddRangeAsync(views);
            var saved = await _context.SaveChangesAsync();
            _logger.LogInformation("<<< Seeded {Count} post views (SaveChanges: {Saved})", views.Count, saved);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! ERROR seeding post views !!!");
            throw;
        }
    }
 
    private async Task SeedSavedPostsAsync()
    {
        try
        {
            _logger.LogInformation(">>> Seeding saved posts...");
 
            var postIds = await _context.Posts
                .Where(p => p.Status == PostStatus.Published && !p.IsDeleted)
                .Select(p => p.PostId)
                .ToListAsync();
 
            if (!postIds.Any())
            {
                _logger.LogWarning("No published posts found. Skipping saved posts.");
                return;
            }
 
            var faker = new Faker();
            var saved = new List<SavedPost>();
 
            foreach (var postId in postIds)
            {
                // унікальна комбінація PostId + UserId
                var usersWhoSaved = FakeUserIds
                    .OrderBy(_ => Guid.NewGuid())
                    .Take(faker.Random.Int(0, SAVED_PER_POST))
                    .ToList();
 
                foreach (var userId in usersWhoSaved)
                {
                    saved.Add(new SavedPost
                    {
                        PostId  = postId,
                        UserId  = userId,
                        SavedAt = faker.Date.Past(1).ToUniversalTime()
                    });
                }
            }
 
            await _context.SavedPosts.AddRangeAsync(saved);
            var savedCount = await _context.SaveChangesAsync();
            _logger.LogInformation("<<< Seeded {Count} saved posts (SaveChanges: {Saved})", saved.Count, savedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "!!! ERROR seeding saved posts !!!");
            throw;
        }
    }
    private async Task SeedPostMediaAsync()
    {
        _logger.LogInformation(">>> Seeding post media...");

        var postIds = await _context.Posts.Select(p => p.PostId).ToListAsync();
        var faker = new Faker();

        var media = new List<PostMedia>();

        foreach (var postId in postIds)
        {
            var mediaCount = faker.Random.Int(1, 3);

            for (int i = 0; i < mediaCount; i++)
            {
                var fileName = $"{Guid.NewGuid()}.jpg";

                media.Add(new PostMedia
                {
                    PostId = postId,
                    Bucket = "posts",
                    ObjectName = $"post-{postId}/{fileName}",
                    ContentType = "image/jpeg"
                });
            }
        }

        await _context.PostMedia.AddRangeAsync(media);
        var saved = await _context.SaveChangesAsync();

        _logger.LogInformation("<<< Seeded {Count} post media (SaveChanges: {Saved})", media.Count, saved);
    }
}