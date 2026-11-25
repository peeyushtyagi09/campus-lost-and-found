using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;

namespace LostAndFound.Api.Services;

public interface IFoundReportService
{
    Task<ServiceResult<FoundReportOwnerDto>> CreateAsync(
        string finderUserId,
        CreateFoundReportRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResponse<FoundReportOwnerDto>>> GetMineAsync(
        string finderUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<FoundReportOwnerDto>> GetByIdAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken);

    Task<ServiceResult<FoundReportOwnerDto>> UpdateAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        UpdateFoundReportRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> CloseAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken);
}
