using System.Text.Json;
using LivePulse.Application.Abstractions;
using LivePulse.Application.Matches;
using LivePulse.Domain.Matches;
using StackExchange.Redis;

namespace LivePulse.Infrastructure.Redis;

public sealed class RedisOddsStore(IConnectionMultiplexer redis) : IOddsStore
{
    private const string KeyPrefix = "odds:";

    public async Task<MatchOdds?> GetAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var json = await redis.GetDatabase().StringGetAsync(KeyFor(matchId));
        return json.IsNullOrEmpty ? null : ToOdds(json.ToString());
    }

    public async Task SetAsync(Guid matchId, MatchOdds odds, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(OddsDto.From(odds));
        await redis.GetDatabase().StringSetAsync(KeyFor(matchId), json);
    }

    public async Task<IReadOnlyDictionary<Guid, MatchOdds>> ListAsync(CancellationToken cancellationToken)
    {
        var database = redis.GetDatabase();
        var result = new Dictionary<Guid, MatchOdds>();

        foreach (var endpoint in redis.GetEndPoints())
        {
            await foreach (var key in redis.GetServer(endpoint).KeysAsync(pattern: KeyPrefix + "*"))
            {
                var json = await database.StringGetAsync(key);
                if (!json.IsNullOrEmpty)
                {
                    result[Guid.Parse(key.ToString()[KeyPrefix.Length..])] = ToOdds(json.ToString());
                }
            }
        }

        return result;
    }

    private static string KeyFor(Guid matchId) => KeyPrefix + matchId;

    private static MatchOdds ToOdds(string json)
    {
        var dto = JsonSerializer.Deserialize<OddsDto>(json)!;
        return MatchOdds.Create(dto.Home, dto.Draw, dto.Away);
    }
}
