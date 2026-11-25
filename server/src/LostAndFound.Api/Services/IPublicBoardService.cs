using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;

namespace LostAndFound.Api.Services;

public interface IPublicBoardService
{
    Task<ServiceResult<PagedResponse<PublicReportDto>>> GetBoardAsync(
        string type,
        ItemCategory? category,
        string? area,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
