using LostAndFound.Api.Entities;
using MongoDB.Bson;
using MongoDB.Driver;

namespace LostAndFound.Api.Repositories;

public sealed class MongoLostReportRepository : ILostReportRepository
{
    private readonly IMongoCollection<LostReport> _reports;

    public MongoLostReportRepository(MongoContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _reports = context.LostReports;
    }

    public async Task CreateAsync(LostReport report, CancellationToken cancellationToken)
    {
        await _reports.InsertOneAsync(report, cancellationToken: cancellationToken);
    }

    public async Task<LostReport?> GetByIdAsync(ObjectId id, CancellationToken cancellationToken)
    {
        return await _reports
            .Find(report => report.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<LostReport> Items, long TotalCount)> GetByReporterAsync(
        ObjectId reporterId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var filter = Builders<LostReport>.Filter.Eq(report => report.ReporterId, reporterId);
        var total = await _reports.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        var items = await _reports
            .Find(filter)
            .SortByDescending(report => report.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<(IReadOnlyList<LostReport> Items, long TotalCount)> SearchPublicAsync(
        ItemCategory? category,
        string? campusArea,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var builder = Builders<LostReport>.Filter;
        var filters = new List<FilterDefinition<LostReport>>
        {
            builder.In(report => report.Status, [ReportStatus.Open, ReportStatus.Matched])
        };

        if (category.HasValue)
        {
            filters.Add(builder.Eq(report => report.Category, category.Value));
        }

        if (!string.IsNullOrWhiteSpace(campusArea))
        {
            var escapedArea = System.Text.RegularExpressions.Regex.Escape(campusArea.Trim());
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

    public async Task<IReadOnlyList<LostReport>> GetOpenByCategoryAsync(
        ItemCategory category,
        CancellationToken cancellationToken)
    {
        var filter = Builders<LostReport>.Filter.And(
            Builders<LostReport>.Filter.In(report => report.Status, [ReportStatus.Open, ReportStatus.Matched]),
            Builders<LostReport>.Filter.Eq(report => report.Category, category));

        return await _reports.Find(filter).ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(LostReport report, CancellationToken cancellationToken)
    {
        await _reports.ReplaceOneAsync(
            existing => existing.Id == report.Id,
            report,
            cancellationToken: cancellationToken);
    }
}
