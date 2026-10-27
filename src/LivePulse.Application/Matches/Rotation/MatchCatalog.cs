using LivePulse.Domain.Matches;

namespace LivePulse.Application.Matches.Rotation;

public static class MatchCatalog
{
    private sealed record Fixture(string Home, string Away, decimal HomeOdds, decimal DrawOdds, decimal AwayOdds);

    private static readonly Fixture[] Fixtures =
    [
        new("Kartal SK", "Aslan FK", 2.10m, 3.20m, 3.50m),
        new("Boğaz Spor", "Anadolu United", 1.85m, 3.40m, 4.20m),
        new("Kuzey Yıldızı", "Güney Atletik", 2.60m, 3.10m, 2.75m),
        new("Marmara FK", "Ege Gücü", 2.30m, 3.00m, 3.10m),
        new("Karadeniz SK", "Akdeniz Spor", 1.95m, 3.30m, 3.90m),
        new("Toros United", "Kızılırmak FK", 2.80m, 3.20m, 2.45m),
        new("Uludağ Spor", "Erciyes SK", 2.15m, 3.10m, 3.40m),
        new("Sakarya Gücü", "Fırat Atletik", 1.75m, 3.60m, 4.60m),
        new("Salda FK", "Tuz Gölü Spor", 3.10m, 3.25m, 2.20m),
        new("Kapadokya SK", "Pamukkale United", 2.40m, 3.05m, 2.95m),
        new("Nemrut Spor", "Ağrı FK", 2.00m, 3.30m, 3.60m),
        new("Efes Gücü", "Truva Atletik", 2.55m, 3.15m, 2.70m)
    ];

    public static IReadOnlyList<Match> CreateSeason() =>
        Fixtures
            .Select((fixture, index) => Match.Create(
                Guid.NewGuid(),
                fixture.Home,
                fixture.Away,
                MatchOdds.Create(fixture.HomeOdds, fixture.DrawOdds, fixture.AwayOdds),
                seasonOrder: index + 1))
            .ToList();
}
