using Cinema.Api.Data;
using Cinema.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cinema.Api.Repositories;

public sealed class EfMovieRepository(CinemaDbContext db) : IMovieRepository
{
    public Task<List<Movie>> GetAllAsync(string? search, int page, int pageSize, string? sortBy, bool descending, CancellationToken cancellationToken)
    {
        IQueryable<Movie> query = db.Movies
            .AsNoTracking()
            .Include(x => x.MovieGenres)
            .ThenInclude(x => x.Genre);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.Title.ToLower().Contains(search.ToLower()));

        query = (sortBy?.ToLowerInvariant(), descending) switch
        {
            ("year", true) => query.OrderByDescending(x => x.ReleaseYear),
            ("year", false) => query.OrderBy(x => x.ReleaseYear),
            ("duration", true) => query.OrderByDescending(x => x.DurationMinutes),
            ("duration", false) => query.OrderBy(x => x.DurationMinutes),
            (_, true) => query.OrderByDescending(x => x.Title),
            _ => query.OrderBy(x => x.Title)
        };

        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public Task<Movie?> GetByIdAsync(int id, CancellationToken cancellationToken) =>
        db.Movies.AsNoTracking()
            .Include(x => x.MovieGenres).ThenInclude(x => x.Genre)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<Genre>> GetGenresAsync(CancellationToken cancellationToken) =>
        db.Genres.AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);

    public async Task<Movie> AddAsync(Movie movie, IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken)
    {
        var uniqueIds = genreIds.Distinct().ToArray();
        var genres = await db.Genres.Where(x => uniqueIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (genres.Count != uniqueIds.Length)
            throw new InvalidOperationException("Один или несколько указанных жанров не существуют.");

        movie.MovieGenres = uniqueIds.Select(id => new MovieGenre { GenreId = id }).ToList();
        db.Movies.Add(movie);
        await db.SaveChangesAsync(cancellationToken);
        return (await GetByIdAsync(movie.Id, cancellationToken))!;
    }

    public async Task<bool> UpdateAsync(int id, Movie movie, IReadOnlyCollection<int> genreIds, CancellationToken cancellationToken)
    {
        var existing = await db.Movies
            .Include(x => x.MovieGenres)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (existing is null) return false;

        var uniqueIds = genreIds.Distinct().ToArray();
        var genres = await db.Genres.Where(x => uniqueIds.Contains(x.Id)).ToListAsync(cancellationToken);
        if (genres.Count != uniqueIds.Length)
            throw new InvalidOperationException("Один или несколько указанных жанров не существуют.");

        existing.Title = movie.Title;
        existing.DurationMinutes = movie.DurationMinutes;
        existing.ReleaseYear = movie.ReleaseYear;
        existing.Description = movie.Description;
        var currentIds = existing.MovieGenres.Select(x => x.GenreId).ToHashSet();
        var requestedIds = uniqueIds.ToHashSet();

        foreach (var relation in existing.MovieGenres.Where(x => !requestedIds.Contains(x.GenreId)).ToList())
            db.MovieGenres.Remove(relation);
        foreach (var genreId in uniqueIds.Where(x => !currentIds.Contains(x)))
            existing.MovieGenres.Add(new MovieGenre { MovieId = id, GenreId = genreId });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // FindAsync + Remove demonstrates ChangeTracker state = Deleted, then SaveChangesAsync.
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var movie = await db.Movies.FindAsync(id);
        if (movie is null) return false;
        if (await db.Screenings.AnyAsync(x => x.MovieId == id, cancellationToken)) return false;
        db.Movies.Remove(movie);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    // ExecuteDeleteAsync demonstrates a bulk delete without materializing entities.
    public Task<int> DeleteOldWithoutScreeningsAsync(int beforeYear, CancellationToken cancellationToken) =>
        db.Movies.Where(x => x.ReleaseYear < beforeYear && !x.Screenings.Any())
            .ExecuteDeleteAsync(cancellationToken);

    // ExecuteUpdateAsync demonstrates a direct set-based update without loading the entity.
    public async Task<bool> SetReleaseYearAsync(int id, int releaseYear, CancellationToken cancellationToken)
    {
        var updated = await db.Movies
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.ReleaseYear, releaseYear), cancellationToken);
        return updated > 0;
    }
}
