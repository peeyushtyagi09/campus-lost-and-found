using LostAndFound.Api.Entities;
using LostAndFound.Api.Options;
using LostAndFound.Api.Repositories;
using LostAndFound.Api.Services;
using NSubstitute;

namespace LostAndFound.Api.Tests.Matching;

public sealed class MatchingScoringTests
{
    private static MatchingService CreateService()
    {
        return new MatchingService(
            Substitute.For<ILostReportRepository>(),
            Substitute.For<IFoundReportRepository>(),
            Substitute.For<IMatchRepository>(),
            Microsoft.Extensions.Options.Options.Create(new MatchingOptions { ScoreThreshold = 50 }));
    }

    [Theory]
    [InlineData(ItemCategory.Phone, ItemCategory.Wallet, "Library", "Library", 0, "Black Phone", "Black Phone", 0)]
    [InlineData(ItemCategory.Phone, ItemCategory.Phone, "Library", "Library", 2, "Black Pixel Phone", "Black Pixel Phone", 100)]
    [InlineData(ItemCategory.Phone, ItemCategory.Phone, "Library", "library", 10, "Silver Case", "Blue Bag", 60)]
    [InlineData(ItemCategory.Phone, ItemCategory.Phone, "Library", "Cafeteria", 45, "Blue Umbrella", "Blue Umbrella", 50)]
    [InlineData(ItemCategory.Phone, ItemCategory.Phone, "Library", "Cafeteria", -1, "Silver Keys", "Red Wallet", 10)]
    public void ScorePair_EvaluatesWeightedRulesDeterministically(
        ItemCategory lostCategory,
        ItemCategory foundCategory,
        string lostArea,
        string foundArea,
        int dayOffset,
        string lostTitle,
        string foundTitle,
        int expectedScore)
    {
        var service = CreateService();
        var lostDate = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var lost = new LostReport
        {
            Category = lostCategory,
            CampusArea = lostArea,
            DateLost = lostDate,
            Title = lostTitle,
            PublicDescription = ""
        };
        var found = new FoundReport
        {
            Category = foundCategory,
            CampusArea = foundArea,
            DateFound = lostDate.AddDays(dayOffset),
            Title = foundTitle,
            PublicDescription = ""
        };

        var result = service.ScorePair(lost, found);
        result.Score.Should().Be(expectedScore);
    }
}
