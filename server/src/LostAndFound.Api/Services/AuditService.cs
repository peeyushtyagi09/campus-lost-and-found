using LostAndFound.Api.Dtos.Admin;
using LostAndFound.Api.Dtos.Common;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Repositories;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public interface IAuditService
{
    Task RecordAsync(
        string action,
        ObjectId actorId,
        UserRole actorRole,
        string? targetId,
        string summary,
        CancellationToken cancellationToken);

    Task<PagedResponse<AuditEventDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken);
}

public sealed class AuditService : IAuditService
{
    private readonly IAuditEventRepository _repository;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IAuditEventRepository repository, ILogger<AuditService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RecordAsync(
        string action,
        ObjectId actorId,
        UserRole actorRole,
        string? targetId,
        string summary,
        CancellationToken cancellationToken)
    {
        var audit = new AuditEvent
        {
            Id = ObjectId.GenerateNewId(),
            Action = action,
            ActorId = actorId,
            ActorRole = actorRole,
            TargetId = targetId,
            Summary = summary,
            Timestamp = DateTime.UtcNow
        };

        await _repository.AppendAsync(audit, cancellationToken);
        _logger.LogInformation(
            "AUDIT {Action} by {ActorId} ({ActorRole}) target={TargetId}: {Summary}",
            action,
            actorId,
            actorRole,
            targetId,
            summary);
    }

    public async Task<PagedResponse<AuditEventDto>> ListAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var normalizedPage = Math.Max(page, 1);
        var normalizedSize = Math.Clamp(pageSize, 1, 50);
        var (items, total) = await _repository.ListAsync(normalizedPage, normalizedSize, cancellationToken);

        var dtos = items.Select(e => new AuditEventDto(
            e.Id.ToString(),
            e.Action,
            e.ActorRole,
            e.TargetId,
            e.Summary,
            e.Timestamp)).ToList();

        return new PagedResponse<AuditEventDto>(dtos, normalizedPage, normalizedSize, total);
    }
}
