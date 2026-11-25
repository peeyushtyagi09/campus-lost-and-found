using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Repositories;

namespace LostAndFound.Api.Services;

public sealed class PublicBoardService : IPublicBoardService
{
    private const int MaxPageSize = 50;
    private readonly ILostReportRepository _lostReports;

    public PublicBoardService(ILostReportRepository lostReports)
    {
        _lostReports = lostReports;
    }

    public async Task<PagedResponse<PublicReportDto>> GetBoardAsync(
        ItemCategory? category,
        string? area,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedSize = Math.Clamp(pageSize, 1, MaxPageSize);

        var (items, totalCount) = await _lostReports.SearchPublicAsync(
            category,
            area,
            query,
            normalizedPage,
            normalizedSize,
            cancellationToken);

        var dtos = items.Select(ReportMappings.ToPublicDto).ToList();
        return new PagedResponse<PublicReportDto>(dtos, normalizedPage, normalizedSize, totalCount);
    }
}
