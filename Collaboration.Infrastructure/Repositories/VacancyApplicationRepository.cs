using Collaboration.Domain.Common;
using Collaboration.Domain.Entities;
using Collaboration.Domain.Interfaces;
using MongoDB.Driver;

namespace Collaboration.Infrastructure.Repositories;

public class VacancyApplicationRepository : GenericRepository<VacancyApplication>, IVacancyApplicationRepository
{
    private readonly IMongoCollection<VacancyApplication> _collection;

    public VacancyApplicationRepository(IMongoDatabase database) : base(database, "VacancyApplications")
    {
        _collection = database.GetCollection<VacancyApplication>("VacancyApplications");
    }

    public async Task<IEnumerable<VacancyApplication>> GetByVacancyIdAsync(
        string vacancyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vacancyId)) return [];

        var filter = Builders<VacancyApplication>.Filter.And(
            Builders<VacancyApplication>.Filter.Eq(a => a.VacancyId, vacancyId),
            Builders<VacancyApplication>.Filter.Eq(a => a.IsDeleted, false));

        return await _collection.Find(filter)
            .Sort(Builders<VacancyApplication>.Sort.Descending(a => a.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<VacancyApplication>> GetByApplicantUserIdAsync(
        string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return [];

        var filter = Builders<VacancyApplication>.Filter.And(
            Builders<VacancyApplication>.Filter.Eq("applicant.userId", userId),
            Builders<VacancyApplication>.Filter.Eq(a => a.IsDeleted, false));

        return await _collection.Find(filter)
            .Sort(Builders<VacancyApplication>.Sort.Descending(a => a.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsPendingAsync(
        string vacancyId, string userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(vacancyId) || string.IsNullOrWhiteSpace(userId)) return false;

        var filter = Builders<VacancyApplication>.Filter.And(
            Builders<VacancyApplication>.Filter.Eq(a => a.VacancyId, vacancyId),
            Builders<VacancyApplication>.Filter.Eq(a => a.Status, VacancyApplicationStatus.Pending),
            Builders<VacancyApplication>.Filter.Eq("applicant.userId", userId));

        return await _collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken) > 0;
    }

    // Викликається при зміні користувача — оновлює snapshot заявника
    public async Task UpdateApplicantSnapshotAsync(
        string userId, string username, string? avatarUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;

        var filter = Builders<VacancyApplication>.Filter.Eq("applicant.userId", userId);

        var update = Builders<VacancyApplication>.Update
            .Set("applicant.username", username)
            .Set("applicant.avatarUrl", avatarUrl)
            .Set("updatedAt", DateTime.UtcNow);

        await _collection.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
    }
}