using System.Text.RegularExpressions;
using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoFoundReportRepository : IFoundReportRepository
{
    private readonly IMongoCollection<FoundReport> _reports;

    public MongoFoundReportRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _reports = context.FoundReports;
    }

    public async Task CreateAsync(FoundReport report, CancellationToken cancellationToken)
    {
        await _reports.InsertOneAsync(report, cancellationToken: cancellationToken);
    }

    public async Task<FoundReport?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken)
    {
        return await _reports
            .Find(report => report.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FoundReport>> GetByIdsAsync(
        IEnumerable<ObjectId> ids,
        CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        var filter = Builders<FoundReport>.Filter.In(report => report.Id, idList);
        return await _reports.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<FoundReport> Items, long TotalCount)> GetByFinderAsync(
        ObjectId finderId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var filter = Builders<FoundReport>.Filter.Eq(report => report.FinderId, finderId);
        var total = await _reports.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var items = await _reports
            .Find(filter)
            .SortByDescending(report => report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<FoundReport> Items, long TotalCount)> SearchPublicAsync(
        ItemCategory? category,
        string? campusArea,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var builder = Builders<FoundReport>.Filter;
        var filters = new List<FilterDefinition<FoundReport>>
        {
            builder.In(report => report.Status, [ReportStatus.Open, ReportStatus.Matched])
        };

        if (category.HasValue)
        {
            filters.Add(builder.Eq(report => report.Category, category.Value));
        }

        if (!string.IsNullOrWhiteSpace(campusArea))
        {
            var escapedArea = Regex.Escape(campusArea.Trim());
            filters.Add(builder.Regex(
                report => report.CampusArea,
                new BsonRegularExpression($"^{escapedArea}$", "i")));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            filters.Add(builder.Text(query.Trim()));
        }

        var combinedFilter = builder.And(filters);
        var total = await _reports.CountDocumentsAsync(combinedFilter, cancellationToken: cancellationToken);
        var items = await _reports
            .Find(combinedFilter)
            .SortByDescending(report => report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<FoundReport>> GetOpenByCategoryAsync(
        ItemCategory category,
        CancellationToken cancellationToken)
    {
        var filter = Builders<FoundReport>.Filter.And(
            Builders<FoundReport>.Filter.In(report => report.Status, [ReportStatus.Open, ReportStatus.Matched]),
            Builders<FoundReport>.Filter.Eq(report => report.Category, category));

        return await _reports.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task<long> FlagEligibleForDisposalAsync(
        DateTime createdBeforeUtc,
        CancellationToken cancellationToken)
    {
        var filter = Builders<FoundReport>.Filter.And(
            Builders<FoundReport>.Filter.In(r => r.Status, [ReportStatus.Open, ReportStatus.Matched]),
            Builders<FoundReport>.Filter.Eq(r => r.EligibleForDisposal, false),
            Builders<FoundReport>.Filter.Lte(r => r.CreatedAt, createdBeforeUtc));

        var now = DateTime.UtcNow;
        var update = Builders<FoundReport>.Update
            .Set(r => r.EligibleForDisposal, true)
            .Set(r => r.DisposalFlaggedAt, now);

        var result = await _reports.UpdateManyAsync(filter, update, cancellationToken: cancellationToken);
        return result.ModifiedCount;
    }

    public async Task<IReadOnlyList<FoundReport>> GetEligibleForDisposalAsync(CancellationToken cancellationToken)
    {
        var filter = Builders<FoundReport>.Filter.And(
            Builders<FoundReport>.Filter.In(r => r.Status, [ReportStatus.Open, ReportStatus.Matched]),
            Builders<FoundReport>.Filter.Eq(r => r.EligibleForDisposal, true));

        return await _reports.Find(filter).SortBy(r => r.CreatedAt).ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(FoundReport report, CancellationToken cancellationToken)
    {
        await _reports.ReplaceOneAsync(
            existing => existing.Id == report.Id,
            report,
            cancellationToken: cancellationToken);
    }
}
