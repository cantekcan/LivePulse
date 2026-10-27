using System.Net.Http.Json;
using LivePulse.Application.Matches;

namespace LivePulse.Simulator;

public sealed class MatchApiClient(HttpClient httpClient)
{
    public async Task<IReadOnlyList<MatchDto>> GetMatchesAsync(CancellationToken cancellationToken) =>
        await httpClient.GetFromJsonAsync<List<MatchDto>>("api/matches", cancellationToken) ?? [];
}
