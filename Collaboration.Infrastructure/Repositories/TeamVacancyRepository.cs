using Collaboration.Domain.Common;
using Collaboration.Domain.Common.Helpers;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class TeamVacancyRepository : GenericRepository<TeamVacancy>, ITeamVacancyRepository
{
    private readonly IMongoCollection<TeamVacancy> _collection;

    public TeamVacancyRepository(IMongoDatabase database) : base(database, "TeamVacancies")
    {
        _collection = database.GetCollection<TeamVacancy>("TeamVacancies");
    }

    public async Task<PagedList<TeamVacancy>> GetByTeamIdAsync(
        string teamId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId))
            return new PagedList<TeamVacancy>([], 0, 1, pageSize);

        var filter = Builders<TeamVacancy>.Filter.And(
            Builders<TeamVacancy>.Filter.Eq(v => v.TeamId, teamId),
            Builders<TeamVacancy>.Filter.Eq(v => v.IsDeleted, false));

        return await PagedList<TeamVacancy>.ToPagedListAsync(
            _collection.Find(filter).Sort(OpenFirstThenNewest()),
            pageNumber, pageSize, cancellationToken);
    }

    public async Task<PagedList<TeamVacancy>> SearchVacanciesAsync(
        string? query, string? category, string? role, string? teamId, bool openOnly,
        int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        var filters = new List<FilterDefinition<TeamVacancy>>
        {
            Builders<TeamVacancy>.Filter.Eq(v => v.IsDeleted, false)
        };

        if (!string.IsNullOrWhiteSpace(query))
            filters.Add(Builders<TeamVacancy>.Filter.Text(query));

        if (!string.IsNullOrWhiteSpace(category))
            filters.Add(Builders<TeamVacancy>.Filter.Regex(
                v => v.Category, new BsonRegularExpression(RegexEscape(category), "i")));

        if (!string.IsNullOrWhiteSpace(role))
            filters.Add(Builders<TeamVacancy>.Filter.Regex(
                v => v.Role, new BsonRegularExpression(RegexEscape(role), "i")));

        if (!string.IsNullOrWhiteSpace(teamId))
            filters.Add(Builders<TeamVacancy>.Filter.Eq(v => v.TeamId, teamId));

        if (openOnly)
            filters.Add(Builders<TeamVacancy>.Filter.Eq(v => v.Status, TeamVacancyStatus.Open));

        var filter = Builders<TeamVacancy>.Filter.And(filters);

        return await PagedList<TeamVacancy>.ToPagedListAsync(
            _collection.Find(filter).Sort(OpenFirstThenNewest()),
            pageNumber, pageSize, cancellationToken);
    }

    public async Task<List<TeamVacancy>> GetOpenByTeamIdsAsync(
        IEnumerable<string> teamIds, CancellationToken cancellationToken = default)
    {
        var ids = teamIds.ToList();
        if (ids.Count == 0) return [];

        var filter = Builders<TeamVacancy>.Filter.And(
            Builders<TeamVacancy>.Filter.In(v => v.TeamId, ids),
            Builders<TeamVacancy>.Filter.Eq(v => v.Status, TeamVacancyStatus.Open),
            Builders<TeamVacancy>.Filter.Eq(v => v.IsDeleted, false));

        return await _collection.Find(filter).ToListAsync(cancellationToken);
    }

    // Викликається при зміні даних команди — оновлює snapshot у вакансіях
    public async Task UpdateTeamSnapshotAsync(
        string teamId, string teamName, string? teamAvatarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return;

        var filter = Builders<TeamVacancy>.Filter.Eq(v => v.TeamId, teamId);

        var update = Builders<TeamVacancy>.Update
            .Set(v => v.TeamName, teamName)
            .Set(v => v.TeamAvatarUrl, teamAvatarUrl)
            .Set(v => v.UpdatedAt, DateTime.UtcNow);

        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static SortDefinition<TeamVacancy> OpenFirstThenNewest()
    {
        return Builders<TeamVacancy>.Sort
            .Ascending(v => v.Status)
            .Descending(v => v.CreatedAt);
    }

    private static string RegexEscape(string value) =>
        System.Text.RegularExpressions.Regex.Escape(value.Trim());
}