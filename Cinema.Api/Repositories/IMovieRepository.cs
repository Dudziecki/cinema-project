using Cinema.Api.Entities;

namespace Cinema.Api.Repositories;

public interface IMovieRepository
{
    Task<List<Movie>> GetAllAsync(string? search, int page, int pageSize, string? sortBy, bool descending, CancellationToken cancellationToken);
    Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken);
    Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken);
    Task<Movie> AddAsync(Movie movie, IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(int id, Movie movie, IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken);
    Task<int> DeleteOldWithoutScreeningsAsync(int beforeYear, CancellationToken cancellationToken);
    Task<bool> SetReleaseYearAsync(int id, int releaseYear, CancellationToken cancellationToken);
}
