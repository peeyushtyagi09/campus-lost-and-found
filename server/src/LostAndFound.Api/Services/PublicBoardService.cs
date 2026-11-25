using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Repositories;

namespace LostAndFound.Api.Services;

public sealed class PublicBoardService : IPublicBoardService
{
    private const int MaxPageSize = 50;
    private readonly ILostReportRepository _lostReports;
    private readonly IFoundReportRepository _foundReports;

    public PublicBoardService(ILostReportRepository lostReports, IFoundReportRepository foundReports)
    {
        _lostReports = lostReports;
        _foundReports = foundReports;
    }

    public async Task<ServiceResult<PagedResponse<PublicReportDto>>> GetBoardAsync(
        string type,
        ItemCategory? category,
        string? area,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedType = string.IsNullOrWhiteSpace(type) ? "lost" : type.Trim().ToLowerInvariant();
        if (normalizedType is not ("lost" or "found"))
        {
            return ServiceResult<PagedResponse<PublicReportDto>>.Failure(
                StatusCodes.Status400BadRequest,
                "Board type must be 'lost' or 'found'.");
        }

        var normalizedPage = Math.Max(page, 1);
        var normalizedSize = Math.Clamp(pageSize, 1, MaxPageSize);

        if (normalizedType == "found")
        {
            var (foundItems, foundTotal) = await _foundReports.SearchPublicAsync(
                category,
                area,
                query,
                normalizedPage,
                normalizedSize,
                cancellationToken);

            var foundDtos = foundItems.Select(ReportMappings.ToPublicDto).ToList();
            return ServiceResult<PagedResponse<PublicReportDto>>.Success(
                new PagedResponse<PublicReportDto>(foundDtos, normalizedPage, normalizedSize, foundTotal));
        }

        var (lostItems, lostTotal) = await _lostReports.SearchPublicAsync(
            category,
            area,
            query,
            normalizedPage,
            normalizedSize,
            cancellationToken);

        var lostDtos = lostItems.Select(ReportMappings.ToPublicDto).ToList();
        return ServiceResult<PagedResponse<PublicReportDto>>.Success(
            new PagedResponse<PublicReportDto>(lostDtos, normalizedPage, normalizedSize, lostTotal));
    }
}
