using Collaboration.Domain.Entities;
using Collaboration.Infrastructure.Data;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Indexes;

public class MongoIndexCreation : IIndexCreation
{
    private readonly MongoDbContext _context;
 
    public MongoIndexCreation(MongoDbContext context)
    {
        _context = context;
    }
 
    public async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        await CreateTeamIndexesAsync(cancellationToken);
        await CreateCollaborationRequestIndexesAsync(cancellationToken);
        await CreateGroupInvitationIndexesAsync(cancellationToken);
        await CreateTeamPostIndexesAsync(cancellationToken);
    }
 
    private async Task CreateTeamIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _context.Teams;
 
        // Текстовий індекс для пошуку за назвою
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Text(t => t.Name),
                new CreateIndexOptions { Name = "idx_name_text" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за категорією
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending(t => t.Category),
                new CreateIndexOptions { Name = "idx_category" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за статусом
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending(t => t.Status),
                new CreateIndexOptions { Name = "idx_status" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за тегами
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending(t => t.Tags),
                new CreateIndexOptions { Name = "idx_tags" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за учасниками (userId у вкладеному масиві)
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending("members.userId"),
                new CreateIndexOptions { Name = "idx_members_user_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Складний індекс: категорія + статус
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys
                    .Ascending(t => t.Category)
                    .Ascending(t => t.Status),
                new CreateIndexOptions { Name = "idx_category_status" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за датою створення
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Descending(t => t.CreatedAt),
                new CreateIndexOptions { Name = "idx_created_at" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс для м'якого видалення
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending(t => t.IsDeleted),
                new CreateIndexOptions { Name = "idx_is_deleted" }
            ),
            cancellationToken: cancellationToken
        );
    }
 
    private async Task CreateCollaborationRequestIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _context.CollaborationRequests;
 
        // Індекс за командою
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<CollaborationRequest>(
                Builders<CollaborationRequest>.IndexKeys.Ascending(r => r.TeamId),
                new CreateIndexOptions { Name = "idx_team_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за відправником
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<CollaborationRequest>(
                Builders<CollaborationRequest>.IndexKeys.Ascending(r => r.FromUserId),
                new CreateIndexOptions { Name = "idx_from_user_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за отримувачем
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<CollaborationRequest>(
                Builders<CollaborationRequest>.IndexKeys.Ascending(r => r.ToUserId),
                new CreateIndexOptions { Name = "idx_to_user_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за статусом
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<CollaborationRequest>(
                Builders<CollaborationRequest>.IndexKeys.Ascending(r => r.Status),
                new CreateIndexOptions { Name = "idx_status" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Складний індекс: команда + статус
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<CollaborationRequest>(
                Builders<CollaborationRequest>.IndexKeys
                    .Ascending(r => r.TeamId)
                    .Ascending(r => r.Status),
                new CreateIndexOptions { Name = "idx_team_status" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за датою створення
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<CollaborationRequest>(
                Builders<CollaborationRequest>.IndexKeys.Descending(r => r.CreatedAt),
                new CreateIndexOptions { Name = "idx_created_at" }
            ),
            cancellationToken: cancellationToken
        );
    }
 
    private async Task CreateGroupInvitationIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _context.GroupInvitations;
 
        // Індекс за командою
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GroupInvitation>(
                Builders<GroupInvitation>.IndexKeys.Ascending(i => i.TeamId),
                new CreateIndexOptions { Name = "idx_team_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за запрошеним користувачем
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GroupInvitation>(
                Builders<GroupInvitation>.IndexKeys.Ascending(i => i.InvitedUserId),
                new CreateIndexOptions { Name = "idx_invited_user_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за тим, хто запросив
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GroupInvitation>(
                Builders<GroupInvitation>.IndexKeys.Ascending(i => i.InvitedByUserId),
                new CreateIndexOptions { Name = "idx_invited_by_user_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за статусом
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GroupInvitation>(
                Builders<GroupInvitation>.IndexKeys.Ascending(i => i.Status),
                new CreateIndexOptions { Name = "idx_status" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за датою закінчення (для пошуку протермінованих)
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GroupInvitation>(
                Builders<GroupInvitation>.IndexKeys.Ascending(i => i.ExpiresAt),
                new CreateIndexOptions { Name = "idx_expires_at" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Складний унікальний індекс: одне активне запрошення до команди для юзера
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GroupInvitation>(
                Builders<GroupInvitation>.IndexKeys
                    .Ascending(i => i.TeamId)
                    .Ascending(i => i.InvitedUserId)
                    .Ascending(i => i.Status),
                new CreateIndexOptions { Name = "idx_team_invited_user_status" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за датою створення
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<GroupInvitation>(
                Builders<GroupInvitation>.IndexKeys.Descending(i => i.CreatedAt),
                new CreateIndexOptions { Name = "idx_created_at" }
            ),
            cancellationToken: cancellationToken
        );
    }
 
    private async Task CreateTeamPostIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _context.TeamPosts;
 
        // Унікальний індекс: один запис для одного поста в команді
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamPost>(
                Builders<TeamPost>.IndexKeys
                    .Ascending(p => p.TeamId)
                    .Ascending(p => p.PostId),
                new CreateIndexOptions { Name = "idx_team_post_unique", Unique = true }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за командою
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamPost>(
                Builders<TeamPost>.IndexKeys.Ascending(p => p.TeamId),
                new CreateIndexOptions { Name = "idx_team_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за автором
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamPost>(
                Builders<TeamPost>.IndexKeys.Ascending("author.userId"),
                new CreateIndexOptions { Name = "idx_author_user_id" }
            ),
            cancellationToken: cancellationToken
        );
 
        // Індекс за датою публікації
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamPost>(
                Builders<TeamPost>.IndexKeys.Descending(p => p.PublishedAt),
                new CreateIndexOptions { Name = "idx_published_at" }
            ),
            cancellationToken: cancellationToken
        );
    }
}