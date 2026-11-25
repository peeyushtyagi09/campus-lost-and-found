using System.Text.RegularExpressions;
using System.Threading.Channels;
using LostAndFound.Api.Dtos.Reports;
using LostAndFound.Api.Entities;
using LostAndFound.Api.Options;
using LostAndFound.Api.Repositories;
using Microsoft.Extensions.Options;
using MongoDB.Bson;

namespace LostAndFound.Api.Services;

public sealed record MatchScoreResult(int Score, string Explanation);

public sealed record MatchingJob(ObjectId ReportId, bool IsFoundReport);

public interface IMatchingQueue
{
    ValueTask EnqueueLostAsync(ObjectId lostReportId, CancellationToken cancellationToken);

    ValueTask EnqueueFoundAsync(ObjectId foundReportId, CancellationToken cancellationToken);

    IAsyncEnumerable<MatchingJob> ReadAllAsync(CancellationToken cancellationToken);
}

public sealed class ChannelMatchingQueue : IMatchingQueue
{
    private readonly Channel<MatchingJob> _channel = Channel.CreateUnbounded<MatchingJob>();

    public ValueTask EnqueueLostAsync(ObjectId lostReportId, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(new MatchingJob(lostReportId, IsFoundReport: false), cancellationToken);

    public ValueTask EnqueueFoundAsync(ObjectId foundReportId, CancellationToken cancellationToken)
        => _channel.Writer.WriteAsync(new MatchingJob(foundReportId, IsFoundReport: true), cancellationToken);

    public IAsyncEnumerable<MatchingJob> ReadAllAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAllAsync(cancellationToken);
}

public interface IMatchingService
{
    MatchScoreResult ScorePair(LostReport lost, FoundReport found);

    Task<int> MatchForLostReportAsync(ObjectId lostReportId, CancellationToken cancellationToken);

    Task<int> MatchForFoundReportAsync(ObjectId foundReportId, CancellationToken cancellationToken);

    Task<int> RecomputeForUserAsync(string userId, bool isStaff, CancellationToken cancellationToken);

    Task<ServiceResult<IReadOnlyList<ItemMatchDto>>> GetMineAsync(string userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<ItemMatchDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<ServiceResult<ItemMatchDto>> UpdateStatusAsync(
        string matchId,
        string callerUserId,
        bool isStaff,
        MatchStatus targetStatus,
        CancellationToken cancellationToken);
}

public sealed class MatchingService : IMatchingService
{
    private static readonly Regex TokenRegex = new("[a-z0-9]{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly HashSet<string> StopWords = new(StringComparer.Ordinal)
    {
        "the", "and", "for", "with", "near", "from", "left", "lost", "found", "item", "this", "that"
    };

    private readonly ILostReportRepository _lostReports;
    private readonly IFoundReportRepository _foundReports;
    private readonly IMatchRepository _matches;
    private readonly INotificationService _notifications;
    private readonly int _scoreThreshold;

    public MatchingService(
        ILostReportRepository lostReports,
        IFoundReportRepository foundReports,
        IMatchRepository matches,
        INotificationService notifications,
        IOptions<MatchingOptions> matchingOptions)
    {
        _lostReports = lostReports;
        _foundReports = foundReports;
        _matches = matches;
        _notifications = notifications;
        _scoreThreshold = matchingOptions.Value.ScoreThreshold;
    }

    public MatchScoreResult ScorePair(LostReport lost, FoundReport found)
    {
        ArgumentNullException.ThrowIfNull(lost);
        ArgumentNullException.ThrowIfNull(found);

        if (lost.Category != found.Category)
        {
            return new MatchScoreResult(0, "Category mismatch");
        }

        var score = 10;
        var reasons = new List<string> { "Same category (+10)" };

        if (string.Equals(lost.CampusArea.Trim(), found.CampusArea.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            score += 30;
            reasons.Add("Same campus area (+30)");
        }

        var dayDelta = (found.DateFound.Date - lost.DateLost.Date).TotalDays;
        if (dayDelta is >= 0 and <= 30)
        {
            score += 20;
            reasons.Add("Found within 0-30 days of loss (+20)");
        }

        var lostTokens = Tokenize($"{lost.Title} {lost.PublicDescription}");
        var foundTokens = Tokenize($"{found.Title} {found.PublicDescription}");
        if (lostTokens.Count > 0 && foundTokens.Count > 0)
        {
            var intersection = lostTokens.Count(token => foundTokens.Contains(token));
            var union = lostTokens.Count + foundTokens.Count - intersection;
            if (union > 0 && intersection > 0)
            {
                var textPoints = (int)Math.Round((double)intersection / union * 40.0, MidpointRounding.AwayFromZero);
                textPoints = Math.Clamp(textPoints, 0, 40);
                if (textPoints > 0)
                {
                    score += textPoints;
                    reasons.Add($"Text similarity (+{textPoints})");
                }
            }
        }

        return new MatchScoreResult(score, string.Join("; ", reasons));
    }

    public async Task<int> MatchForLostReportAsync(ObjectId lostReportId, CancellationToken cancellationToken)
    {
        var lost = await _lostReports.GetByIdAsync(lostReportId, cancellationToken);
        if (lost is null || lost.Status is not (ReportStatus.Open or ReportStatus.Matched))
        {
            return 0;
        }

        var candidates = await _foundReports.GetOpenByCategoryAsync(lost.Category, cancellationToken);
        var created = 0;

        foreach (var found in candidates)
        {
            var result = ScorePair(lost, found);
            if (result.Score < _scoreThreshold)
            {
                continue;
            }

            await UpsertAsync(lost, found, result, cancellationToken);
            created++;
        }

        return created;
    }

    public async Task<int> MatchForFoundReportAsync(ObjectId foundReportId, CancellationToken cancellationToken)
    {
        var found = await _foundReports.GetByIdAsync(foundReportId, cancellationToken);
        if (found is null || found.Status is not (ReportStatus.Open or ReportStatus.Matched))
        {
            return 0;
        }

        var candidates = await _lostReports.GetOpenByCategoryAsync(found.Category, cancellationToken);
        var created = 0;

        foreach (var lost in candidates)
        {
            var result = ScorePair(lost, found);
            if (result.Score < _scoreThreshold)
            {
                continue;
            }

            await UpsertAsync(lost, found, result, cancellationToken);
            created++;
        }

        return created;
    }

    public async Task<int> RecomputeForUserAsync(string userId, bool isStaff, CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out var callerId))
        {
            return 0;
        }

        var count = 0;
        if (isStaff)
        {
            foreach (var category in Enum.GetValues<ItemCategory>())
            {
                var foundList = await _foundReports.GetOpenByCategoryAsync(category, cancellationToken);
                foreach (var found in foundList)
                {
                    count += await MatchForFoundReportAsync(found.Id, cancellationToken);
                }
            }
            return count;
        }

        var (myLost, _) = await _lostReports.GetByReporterAsync(callerId, 1, 50, cancellationToken);
        foreach (var lost in myLost)
        {
            count += await MatchForLostReportAsync(lost.Id, cancellationToken);
        }

        return count;
    }

    public async Task<ServiceResult<IReadOnlyList<ItemMatchDto>>> GetMineAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(userId, out var reporterId))
        {
            return ServiceResult<IReadOnlyList<ItemMatchDto>>.Failure(
                StatusCodes.Status401Unauthorized,
                "Authentication required.");
        }

        var matches = await _matches.GetByReporterAsync(reporterId, cancellationToken);
        var dtos = await MapWithPublicFoundReportsAsync(matches, cancellationToken);
        return ServiceResult<IReadOnlyList<ItemMatchDto>>.Success(dtos);
    }

    public async Task<IReadOnlyList<ItemMatchDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var matches = await _matches.GetAllAsync(cancellationToken);
        return await MapWithPublicFoundReportsAsync(matches, cancellationToken);
    }

    public async Task<ServiceResult<ItemMatchDto>> UpdateStatusAsync(
        string matchId,
        string callerUserId,
        bool isStaff,
        MatchStatus targetStatus,
        CancellationToken cancellationToken)
    {
        if (!ObjectId.TryParse(matchId, out var id) || !ObjectId.TryParse(callerUserId, out var callerId))
        {
            return ServiceResult<ItemMatchDto>.Failure(StatusCodes.Status404NotFound, "Match not found.");
        }

        var match = await _matches.GetByIdAsync(id, cancellationToken);
        if (match is null || (match.LostReporterId != callerId && !isStaff))
        {
            return ServiceResult<ItemMatchDto>.Failure(StatusCodes.Status404NotFound, "Match not found.");
        }

        match.Status = targetStatus;
        match.UpdatedAt = DateTime.UtcNow;
        await _matches.UpdateAsync(match, cancellationToken);

        if (targetStatus == MatchStatus.Confirmed)
        {
            var lost = await _lostReports.GetByIdAsync(match.LostReportId, cancellationToken);
            if (lost is not null && lost.Status == ReportStatus.Open)
            {
                lost.Status = ReportStatus.Matched;
                lost.UpdatedAt = DateTime.UtcNow;
                await _lostReports.UpdateAsync(lost, cancellationToken);
            }
        }

        var found = await _foundReports.GetByIdAsync(match.FoundReportId, cancellationToken);
        if (found is null)
        {
            return ServiceResult<ItemMatchDto>.Failure(StatusCodes.Status404NotFound, "Found report not found.");
        }

        var dto = new ItemMatchDto(
            match.Id.ToString(),
            match.LostReportId.ToString(),
            match.FoundReportId.ToString(),
            match.Score,
            match.Explanation,
            match.Status,
            match.CreatedAt,
            ReportMappings.ToPublicDto(found));

        return ServiceResult<ItemMatchDto>.Success(dto);
    }

    private async Task UpsertAsync(
        LostReport lost,
        FoundReport found,
        MatchScoreResult result,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var match = new ItemMatch
        {
            Id = ObjectId.GenerateNewId(),
            LostReportId = lost.Id,
            FoundReportId = found.Id,
            LostReporterId = lost.ReporterId,
            Score = result.Score,
            Explanation = result.Explanation,
            Status = MatchStatus.Suggested,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _matches.UpsertSuggestedAsync(match, cancellationToken);
        await _notifications.NotifyUserAsync(
            lost.ReporterId,
            NotificationType.MatchFound,
            $"A possible match ({result.Score}/100) was found for '{lost.Title}'.",
            match.Id.ToString(),
            cancellationToken);
    }

    private async Task<IReadOnlyList<ItemMatchDto>> MapWithPublicFoundReportsAsync(
        IReadOnlyList<ItemMatch> matches,
        CancellationToken cancellationToken)
    {
        if (matches.Count == 0)
        {
            return [];
        }

        var foundIds = matches.Select(m => m.FoundReportId).ToList();
        var foundReports = await _foundReports.GetByIdsAsync(foundIds, cancellationToken);
        var byId = foundReports.ToDictionary(f => f.Id);

        var result = new List<ItemMatchDto>(matches.Count);
        foreach (var match in matches)
        {
            if (!byId.TryGetValue(match.FoundReportId, out var found))
            {
                continue;
            }

            result.Add(new ItemMatchDto(
                match.Id.ToString(),
                match.LostReportId.ToString(),
                match.FoundReportId.ToString(),
                match.Score,
                match.Explanation,
                match.Status,
                match.CreatedAt,
                ReportMappings.ToPublicDto(found)));
        }

        return result;
    }

    private static HashSet<string> Tokenize(string input)
    {
        var tokens = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match match in TokenRegex.Matches(input.ToLowerInvariant()))
        {
            if (!StopWords.Contains(match.Value))
            {
                tokens.Add(match.Value);
            }
        }
        return tokens;
    }
}
