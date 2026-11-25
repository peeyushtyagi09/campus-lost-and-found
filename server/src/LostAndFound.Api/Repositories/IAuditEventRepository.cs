using LostAndFound.Api.Entities;

namespace LostAndFound.Api.Repositories;

public interface IAuditEventRepository
{
    Task AppendAsync(AuditEvent auditEvent, CancellationToken cancellationToken);

    Task<(IReadOnlyList<AuditEvent> Items, long TotalCount)> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
