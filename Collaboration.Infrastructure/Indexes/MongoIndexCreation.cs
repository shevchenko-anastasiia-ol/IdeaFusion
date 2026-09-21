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
        await CreateTeamVacancyIndexesAsync(cancellationToken);
        await CreateVacancyApplicationIndexesAsync(cancellationToken);
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