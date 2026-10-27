using LivePulse.Application.Abstractions;
using LivePulse.Domain.Matches;
using Microsoft.EntityFrameworkCore;

namespace LivePulse.Infrastructure.Persistence;

public sealed class MatchRepository(LivePulseDbContext dbContext) : IMatchRepository
{
    public Task<Match?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Matches.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Match>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken) =>
        await dbContext.Matches.Where(m => ids.Contains(m.Id)).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Match>> GetLiveAsync(CancellationToken cancellationToken) =>
        await dbContext.Matches
            .Where(m => m.Status == MatchStatus.Live)
            .OrderBy(m => m.SeasonOrder)
            .ToListAsync(cancellationToken);

    public Task<Match?> GetNextNotStartedAsync(CancellationToken cancellationToken) =>
        dbContext.Matches
            .Where(m => m.Status == MatchStatus.NotStarted)
            .OrderBy(m => m.SeasonOrder)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<bool> AnyUnfinishedAsync(CancellationToken cancellationToken) =>
        dbContext.Matches.AnyAsync(m => m.Status != MatchStatus.Finished, cancellationToken);

    public void AddRange(IEnumerable<Match> matches) => dbContext.Matches.AddRange(matches);
}
