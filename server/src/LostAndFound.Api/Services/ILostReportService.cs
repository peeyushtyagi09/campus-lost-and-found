using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Dtos.Reports;

namespace LostAndFound.Api.Services;

public interface ILostReportService
{
    Task<ServiceResult<LostReportOwnerDto>> CreateAsync(
        string reporterUserId,
        CreateLostReportRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<PagedResponse<LostReportOwnerDto>>> GetMineAsync(
        string reporterUserId,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ServiceResult<LostReportOwnerDto>> GetByIdAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken);

    Task<ServiceResult<LostReportOwnerDto>> UpdateAsync(
        string reportId,
        string callerUserId,
        UpdateLostReportRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<bool>> CloseAsync(
        string reportId,
        string callerUserId,
        bool isStaff,
        CancellationToken cancellationToken);
}
