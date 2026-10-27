using LivePulse.Domain.Matches;

namespace LivePulse.Application.Abstractions;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Match>> GetByIdsAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);

    Task<IReadOnlyList<Match>> GetLiveAsync(CancellationToken cancellationToken);

    Task<Match?> GetNextNotStartedAsync(CancellationToken cancellationToken);

    Task<bool> AnyUnfinishedAsync(CancellationToken cancellationToken);

    void AddRange(IEnumerable<Match> matches);
}
