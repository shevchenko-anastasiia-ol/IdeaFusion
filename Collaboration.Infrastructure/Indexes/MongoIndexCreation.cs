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
        await CreateChatMessageIndexesAsync(cancellationToken);
        await CreateDirectConversationIndexesAsync(cancellationToken);
        await CreateTeamVacancyIndexesAsync(cancellationToken);
        await CreateVacancyApplicationIndexesAsync(cancellationToken);
    }

    /// <summary>
    /// Removes an index that is being replaced. Mongo treats a name as an identity, so
    /// an old index declared under the same name but a different key would make the
    /// creation below fail outright rather than migrate.
    /// </summary>
    private static async Task DropIndexIfExistsAsync<T>(
        IMongoCollection<T> collection, string name, CancellationToken cancellationToken)
    {
        var exists = false;
        try
        {
            using var cursor = await collection.Indexes.ListAsync(cancellationToken);
            var indexes = await cursor.ToListAsync(cancellationToken);
            exists = indexes.Any(i => string.Equals(i["name"].AsString, name, StringComparison.Ordinal));
        }
        catch (MongoCommandException)
        {
            // Listing failed (permissions, or the collection does not exist yet). Creating
            // the replacement below will surface anything that actually matters.
            return;
        }

        if (!exists) return;

        try
        {
            await collection.Indexes.DropOneAsync(name, cancellationToken);
        }
        catch (MongoCommandException)
        {
            // Someone else dropped it first, which is the outcome we wanted anyway.
        }
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
 
        // Індекс за учасниками.
        //
        // The member's subject is stored at members.user.userId: TeamMember is persisted
        // under "user" and UserSnapshot's identifier under "userId". The previous index
        // was declared on "members.userId", which matches no document at all, so every
        // membership lookup was a collection scan. It also has to be dropped by name:
        // Mongo refuses to redefine an index under a name that already exists.
        await DropIndexIfExistsAsync(collection, "idx_members_user_id", cancellationToken);

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending("members.user.userId"),
                new CreateIndexOptions { Name = "idx_members_user_user_id" }
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

        // Індекс за кількістю відкритих вакансій (для фільтру "є вакансії")
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<Team>(
                Builders<Team>.IndexKeys.Ascending(t => t.OpenVacancyCount),
                new CreateIndexOptions { Name = "idx_open_vacancy_count" }
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

    private async Task CreateChatMessageIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _context.ChatMessages;

        // Складний індекс: чат + час (для історії повідомлень)
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys
                    .Ascending(m => m.ChatId)
                    .Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "idx_chat_id_sent_at" }
            ),
            cancellationToken: cancellationToken
        );

        // Індекс за відправником (для синхронізації snapshot)
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys.Ascending("sender.userId"),
                new CreateIndexOptions { Name = "idx_sender_user_id" }
            ),
            cancellationToken: cancellationToken
        );

        // Індекс за датою створення (для пагінації курсом)
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<ChatMessage>(
                Builders<ChatMessage>.IndexKeys.Descending(m => m.CreatedAt),
                new CreateIndexOptions { Name = "idx_created_at" }
            ),
            cancellationToken: cancellationToken
        );
    }

    private async Task CreateDirectConversationIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _context.DirectConversations;

        // One conversation per pair of people: a declined request cannot be sent again as a new one.
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<DirectConversation>(
                Builders<DirectConversation>.IndexKeys.Ascending(c => c.PairKey),
                new CreateIndexOptions { Name = "idx_direct_pair", Unique = true }
            ),
            cancellationToken: cancellationToken
        );

        // The caller's conversations
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<DirectConversation>(
                Builders<DirectConversation>.IndexKeys.Ascending("participantIds"),
                new CreateIndexOptions { Name = "idx_direct_participants" }
            ),
            cancellationToken: cancellationToken
        );

        // One settings document per person
        await _context.NotificationSettings.Indexes.CreateOneAsync(
            new CreateIndexModel<NotificationSettings>(
                Builders<NotificationSettings>.IndexKeys.Ascending(s => s.UserId),
                new CreateIndexOptions { Name = "idx_notification_settings_user", Unique = true }
            ),
            cancellationToken: cancellationToken
        );
    }

    private async Task CreateTeamVacancyIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _context.TeamVacancies;

        // Складний індекс: команда + статус
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamVacancy>(
                Builders<TeamVacancy>.IndexKeys
                    .Ascending(v => v.TeamId)
                    .Ascending(v => v.Status)
                    .Descending(v => v.CreatedAt),
                new CreateIndexOptions { Name = "idx_vacancy_team_open" }
            ),
            cancellationToken: cancellationToken
        );

        // Категорія (сфера інтересів) + статус + час — для перегляду вакансій
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamVacancy>(
                Builders<TeamVacancy>.IndexKeys
                    .Ascending(v => v.Category)
                    .Ascending(v => v.Status)
                    .Descending(v => v.CreatedAt),
                new CreateIndexOptions { Name = "idx_vacancy_category_status" }
            ),
            cancellationToken: cancellationToken
        );

        // Пошук за роллю
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamVacancy>(
                Builders<TeamVacancy>.IndexKeys.Ascending(v => v.Role),
                new CreateIndexOptions { Name = "idx_vacancy_role" }
            ),
            cancellationToken: cancellationToken
        );

        // Текстовий індекс для ролі, опису та вимог
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamVacancy>(
                Builders<TeamVacancy>.IndexKeys
                    .Text(v => v.Role)
                    .Text(v => v.Description)
                    .Text(v => v.Requirements),
                new CreateIndexOptions { Name = "idx_vacancy_text" }
            ),
            cancellationToken: cancellationToken
        );

        // Індекс за датою створення (для сортування за замовчуванням)
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<TeamVacancy>(
                Builders<TeamVacancy>.IndexKeys.Descending(v => v.CreatedAt),
                new CreateIndexOptions { Name = "idx_vacancy_created_at" }
            ),
            cancellationToken: cancellationToken
        );
    }

    private async Task CreateVacancyApplicationIndexesAsync(CancellationToken cancellationToken)
    {
        var collection = _context.VacancyApplications;

        // Складний індекс: вакансія + статус (для списку заявок та dedupe/pending)
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<VacancyApplication>(
                Builders<VacancyApplication>.IndexKeys
                    .Ascending(a => a.VacancyId)
                    .Ascending(a => a.Status),
                new CreateIndexOptions { Name = "idx_app_vacancy_status" }
            ),
            cancellationToken: cancellationToken
        );

        // Заявник + вакансія (для перевірки повторних заявок)
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<VacancyApplication>(
                Builders<VacancyApplication>.IndexKeys
                    .Ascending("applicant.userId")
                    .Ascending(a => a.VacancyId),
                new CreateIndexOptions { Name = "idx_app_user_vacancy" }
            ),
            cancellationToken: cancellationToken
        );

        // Заявки за командою
        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<VacancyApplication>(
                Builders<VacancyApplication>.IndexKeys
                    .Ascending(a => a.TeamId)
                    .Descending(a => a.CreatedAt),
                new CreateIndexOptions { Name = "idx_app_team_created" }
            ),
            cancellationToken: cancellationToken
        );
    }
}